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
                        throw new KeyNotFoundException($"Key '{key}' not found");
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

    private object? EvaluateFunction(FunctionNode functionNode)
    {
        var f = functionNode.Function;

        // Evaluate arguments first to know the count
        var args = functionNode.Arguments.Select(Evaluate).ToArray();

        // Find all registrations with this function name
        var candidateRegistrations = registrations.Where(x => x.FunctionName == f).ToArray();

        if (candidateRegistrations.Length == 0)
        {
            throw new Exception($"function {f} is not registered");
        }

        // If there's only one registration, use it
        if (candidateRegistrations.Length == 1)
        {
            var registration = candidateRegistrations[0];
            if (serviceProvider.GetService(registration.FunctionType) is not IFunction ifn)
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

        if (serviceProvider.GetService(finalRegistration.FunctionType) is not IFunction ifn2)
        {
            throw new Exception("No can do 2");
        }

        return FunctionAuxiliary.FunctionInvoker(ifn2, args);
    }
}
