namespace Compo;

public record Node;

public record ValueNode<T>(T Value) : Node
{
    public T Value { get; init; } = Value;
}

public record FunctionNode(string Function, List<Node> Arguments) : Node
{
    public string Function { get; init; } = Function;

    public List<Node> Arguments { get; init; } = Arguments;
}
