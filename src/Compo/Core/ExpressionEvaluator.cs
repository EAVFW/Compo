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
}

public class ExpressionEvaluator(
    ILogger<ExpressionEvaluator> logger,
    IEnumerable<FunctionRegistration> registrations,
    IServiceProvider serviceProvider) : IExpressionEvaluator
{
    /// <inheritdoc/>
    public object? Evaluate(Node node)
    {
        if (node is FunctionNode f)
        {
            return EvaluateFunction(f);
        }

        var nodeTypes = node.GetType().GetGenericArguments();
        var nodeType = nodeTypes.FirstOrDefault();

        if (nodeType != null && nodeTypes.Length > 1)
            throw new Exception("Cannot happen?");

        // TODO: Hvad er cost? Er det hurtigere at bruge reflection eller at lave en switch på alle typer?
        var method = typeof(ExpressionEvaluator)
            .GetMethod(nameof(EvaluateValue), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(nodeType!);

        var value = method.Invoke(this, [node]);

        var typedValue = Convert.ChangeType(value, nodeType!);

        return typedValue;
    }

    private T EvaluateValue<T>(ValueNode<T> valueNode)
    {
        return valueNode.Value;
    }

    private object? EvaluateFunction(FunctionNode functionNode)
    {
        var f = functionNode.Function;
        var registration = registrations.FirstOrDefault(x => x.FunctionName == f);
        if (registration == default)
        {
            throw new Exception("No can do 1");
        }

        Evaluate(functionNode.Arguments.First());
        var args = functionNode.Arguments.Select(Evaluate).ToArray();

        if (serviceProvider.GetService(registration.FunctionType) is not IFunction ifn)
        {
            throw new Exception("No can do 2");
        }

        return FunctionAuxiliary.FunctionInvoker(ifn, args);
    }
}
