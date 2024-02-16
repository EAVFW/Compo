namespace Compo;

public record Node;

/// <summary>
/// Value Node is encapsulating a value
/// </summary>
/// <param name="Value">The value wrapped as a Node</param>
/// <typeparam name="T">The Node value type</typeparam>
public record ValueNode<T>(T Value) : Node;

/// <summary>
/// Function Node is encapsulating a function call and its arguments
/// </summary>
/// <param name="Function">Function name</param>
/// <param name="Arguments">Function arguments, can be any node</param>
public record FunctionNode(string Function, List<Node> Arguments) : Node;

/// <summary>
/// Access Node is encapsulating a node and an index
/// </summary>
/// <param name="Node"></param>
/// <param name="Index"></param>
public record AccessNode(Node Node, Node Index) : Node;
