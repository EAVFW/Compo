using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Compo;

public interface IExpressionEvaluator
{
    /// <summary>
    /// Evaluate a node tree, that is each arguments is evaluated and used to evaluate the current node.
    ///
    /// The return object type can be inferred from the <see cref="FunctionNode"/> or the <see cref="ValueNode{T}"/>.
    /// </summary>
    /// <param name="node">Either a <see cref="ValueNode{T}"/> or <see cref="FunctionNode"/></param>
    /// <returns>The value of a <see cref="ValueNode{T}"/> or the result of a <see cref="FunctionNode"/></returns>
    object? Evaluate(Node node);

    T EvaluateValue<T>(ValueNode<T> valueNode);
}

public class ExpressionEvaluator(
    ILogger<ExpressionEvaluator> logger,
    IEnumerable<FunctionRegistration> registrations,
    IServiceProvider serviceProvider) : IExpressionEvaluator
{
    /// <inheritdoc/>
    public object? Evaluate(Node node)
    {
        switch (node)
        {
            case FunctionNode f:
                return EvaluateFunction(f);
            case ValueNode<object> v:
                return v.Value;
            case ValueNode<int> v:
                return v.Value;
            case ValueNode<double> v:
                return v.Value;
            case ValueNode<string> v:
                return v.Value;
            case ValueNode<bool> v:
                return v.Value;
            case ValueNode<decimal> v:
                return v.Value;
            case AccessNode n:
                var evaluatedNode = Evaluate(n.Node);

                if (evaluatedNode == null)
                {
                    if (n.Nulled)
                    {
                        return null;
                    }

                    throw new NullReferenceException($"Accessing a null node: {n.Node}");
                }

                // Short circuit evaluation for index access
                var evaluatedIndex = Evaluate(n.Index);
                if (evaluatedIndex is string key)
                {
                    if (evaluatedNode is IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue(key, out var value))
                        {
                            return value;
                        }

                        // If null-conditional operator is used and key is not found, return null
                        if (n.Nulled)
                        {
                            return null;
                        }

                        throw new KeyNotFoundException($"Key '{key}' not found");
                    }

                    // If null-conditional operator is used and node is not a dictionary, return null
                    if (n.Nulled)
                    {
                        return null;
                    }

                    throw new Exception("Expecting node to be an object");
                }

                if (evaluatedIndex is int index)
                {
                    if (evaluatedNode is IEnumerable<object> list)
                    {
                        return list.ElementAt(index);
                    }

                    throw new Exception("Expecting node to be an list");
                }

                throw new InvalidDataException("AccessNode index must be either a string or an int.");
            default:
                throw new NotSupportedException($"Node type {node.NodeType} is not supported.");
        }
    }

    public T EvaluateValue<T>(ValueNode<T> valueNode)
    {
        return valueNode.Value;
    }

    private object?[] PrepareArguments(List<Node> argumentNodes, FunctionRegistration[] candidateRegistrations)
    {
        // Check if any candidate function expects Lazy<T> or has [NestedExpression] parameters
        var needsSpecialHandling = candidateRegistrations.Any(reg =>
            (reg.ArgumentTypes?.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Lazy<>)) ?? false) ||
            (reg.Parameters?.Any(p => p.GetCustomAttribute<NestedExpressionAttribute>() != null) ?? false));

        if (!needsSpecialHandling)
        {
            // Standard evaluation - evaluate all arguments immediately
            return argumentNodes.Select(Evaluate).ToArray();
        }

        // Find the first matching registration with special parameter handling
        var specialRegistration = candidateRegistrations.FirstOrDefault(reg =>
            (reg.ArgumentTypes?.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Lazy<>)) ?? false) ||
            (reg.Parameters?.Any(p => p.GetCustomAttribute<NestedExpressionAttribute>() != null) ?? false));

        if (specialRegistration == null || specialRegistration.ArgumentTypes == null)
        {
            return argumentNodes.Select(Evaluate).ToArray();
        }

        var args = new object?[argumentNodes.Count];
        for (int i = 0; i < argumentNodes.Count && i < specialRegistration.ArgumentTypes.Length; i++)
        {
            var paramType = specialRegistration.ArgumentTypes[i];
            var argNode = argumentNodes[i];
            var paramInfo = specialRegistration.Parameters != null && i < specialRegistration.Parameters.Length
                ? specialRegistration.Parameters[i]
                : null;

            // Check for [NestedExpression] attribute
            if (paramInfo?.GetCustomAttribute<NestedExpressionAttribute>() != null)
            {
                // Accept either string (parse it) OR direct Node (use as-is)
                if (argNode is ValueNode<string> stringNode)
                {
                    // Parse string argument to Node
                    var parser = (ExpressionParser)serviceProvider.GetService(typeof(ExpressionParser))!;

                    // Prepend @ if not already present - nested expressions follow Compo design
                    // where @ is only used for the outermost expression trigger
                    var expressionText = stringNode.Value.TrimStart();
                    if (!expressionText.StartsWith('@'))
                    {
                        expressionText = "@" + expressionText;
                    }

                    var parseResult = parser.BuildAst(expressionText);
                    if (parseResult.Value == null)
                    {
                        throw new InvalidOperationException($"Failed to parse nested expression: {stringNode.Value}");
                    }
                    args[i] = parseResult.Value;  // Pass Node instead of string
                }
                else if (argNode is FunctionNode || argNode is AccessNode)
                {
                    // Accept already-parsed Node directly
                    // This avoids string escaping issues and improves performance
                    args[i] = argNode;
                }
                else if (argNode is ValueNode<int> || argNode is ValueNode<bool> ||
                         argNode is ValueNode<decimal> || argNode is ValueNode<object>)
                {
                    // Accept value nodes directly
                    args[i] = argNode;
                }
                else
                {
                    throw new InvalidOperationException($"[NestedExpression] parameter must receive a string value node or a Node, got {argNode.GetType().Name}");
                }
            }
            else if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                // Create Lazy<T> wrapper that defers evaluation
                var valueType = paramType.GetGenericArguments()[0];

                // Capture the node in a closure for lazy evaluation
                var node = argNode;
                args[i] = CreateLazy(node, valueType);
            }
            else
            {
                // Evaluate immediately for non-lazy, non-nested parameters
                args[i] = Evaluate(argNode);
            }
        }

        return args;
    }

    private object CreateLazy(Node node, Type valueType)
    {
        // Create Lazy<object?> first, then we'll handle conversion in FunctionInvoker
        // This is simpler than trying to create Lazy<T> with proper typing via reflection
        return new Lazy<object?>(() => Evaluate(node));
    }

    private object? EvaluateFunction(FunctionNode functionNode)
    {
        var f = functionNode.Function;

        // Find all registrations with this function name first
        var candidateRegistrations = registrations.Where(x => x.FunctionName == f).ToArray();

        if (candidateRegistrations.Length == 0)
        {
            throw new Exception($"function {f} is not registered");
        }

        // Prepare arguments - evaluate based on whether function expects Lazy<T>
        var args = PrepareArguments(functionNode.Arguments, candidateRegistrations);

        // If there's only one registration, use it
        if (candidateRegistrations.Length == 1)
        {
            var registration = candidateRegistrations[0];
            var ifn = GetFunctionInstance(registration, args);
            if (ifn == null)
            {
                throw new Exception("No can do 2");
            }

            return FunctionAuxiliary.FunctionInvoker(ifn, args);
        }

        // Multiple registrations - find the one that matches the argument count
        // Use pre-parsed ArgumentTypes for fast matching
        FunctionRegistration? matchedRegistration = null;

        foreach (var candidate in candidateRegistrations)
        {
            // If ArgumentTypes is cached, use it for fast matching
            if (candidate.ArgumentTypes != null)
            {
                if (candidate.ArgumentTypes.Length == args.Length)
                {
                    matchedRegistration = candidate;
                    break;
                }
            }
            else
            {
                // Fallback to reflection if ArgumentTypes not available
                var functionType = candidate.FunctionType;
                var interfaces = functionType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IFunction"))
                    .ToArray();

                foreach (var iface in interfaces)
                {
                    var genericArgs = iface.GetGenericArguments();
                    // IFunction<T1, T2, ..., TN, TResult> has N+1 generic arguments
                    // The last one is the return type, so argument count is Length - 1
                    var expectedArgCount = genericArgs.Length - 1;

                    if (expectedArgCount == args.Length)
                    {
                        matchedRegistration = candidate;
                        break;
                    }
                }

                if (matchedRegistration != null)
                    break;
            }
        }

        // If no match found, fall back to first registration (existing behavior)
        var finalRegistration = matchedRegistration ?? candidateRegistrations[0];

        var ifn2 = GetFunctionInstance(finalRegistration, args);
        if (ifn2 == null)
        {
            throw new Exception("No can do 2");
        }

        return FunctionAuxiliary.FunctionInvoker(ifn2, args);
    }

    private IFunction? GetFunctionInstance(FunctionRegistration registration, object?[] args)
    {
        if (!registration.IsOpenGeneric)
        {
            // Closed type - get from DI
            return serviceProvider.GetService(registration.FunctionType) as IFunction;
        }

        // Open generic - use object? for both type parameters
        // The True/False dispatch will handle type safety at runtime
        var closedType = registration.FunctionType.MakeGenericType(typeof(object), typeof(object));

        // Create instance
        return Activator.CreateInstance(closedType) as IFunction;
    }
}