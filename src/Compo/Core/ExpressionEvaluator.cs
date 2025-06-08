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
        var registration = registrations.FirstOrDefault(x => x.FunctionName == f);

        if (registration == null)
        {
            throw new Exception($"function {f} is not registered");
        }

        var args = functionNode.Arguments.Select(Evaluate).ToArray();

        if (serviceProvider.GetService(registration.FunctionType) is not IFunction ifn)
        {
            throw new Exception("No can do 2");
        }

        return FunctionAuxiliary.FunctionInvoker(ifn, args);
    }
}
