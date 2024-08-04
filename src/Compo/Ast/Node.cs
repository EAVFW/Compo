using System.Runtime.InteropServices.JavaScript;

namespace Compo;

public record Node;

/// <summary>
/// Value Node is encapsulating a value
/// </summary>
/// <param name="Value">The value wrapped as a Node</param>
/// <typeparam name="T">The Node value type</typeparam>
public record ValueNode<T>(T Value) : Node
{
    public virtual bool Equals(ValueNode<T>? other)
    {
        return other != null && other.Value.Equals(Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Value);
    }
}

/// <summary>
/// Function Node is encapsulating a function call and its arguments
/// </summary>
/// <param name="Function">Function name</param>
/// <param name="Arguments">Function arguments, can be any node</param>
public record FunctionNode(string Function, List<Node> Arguments) : Node
{
    public virtual bool Equals(FunctionNode? other)
    {
        return
            other != null
            && other.Function == Function
            && other.Arguments.SequenceEqual(Arguments);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(),
            Arguments.Aggregate(Function.GetHashCode(), (acc, node) => HashCode.Combine(acc, node.GetHashCode())));
    }
}

/// <summary>
/// Access Node is encapsulating a node and an index
/// </summary>
/// <param name="Node"></param>
/// <param name="Index"></param>
public record AccessNode(Node Node, Node Index) : Node
{
    public virtual bool Equals(AccessNode? other)
    {
        return other != null && other.Node.Equals(Node) && other.Index.Equals(Index);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), HashCode.Combine(Node.GetHashCode(), Index.GetHashCode()));
    }
}
