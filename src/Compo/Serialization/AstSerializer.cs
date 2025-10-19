using System.Text.Json;

namespace Compo.Serialization;

/// <summary>
/// Serializer for Compo AST nodes. Handles polymorphic serialization
/// of Node types to/from JSON using type discriminators.
/// </summary>
public class AstSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AstSerializer"/> class
    /// with default options (indented JSON).
    /// </summary>
    public AstSerializer() : this(true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AstSerializer"/> class.
    /// </summary>
    /// <param name="writeIndented">Whether to format JSON with indentation.</param>
    public AstSerializer(bool writeIndented)
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            Converters = { new NodeJsonConverter() }
        };
    }

    /// <summary>
    /// Serializes a Node to JSON string.
    /// </summary>
    /// <param name="node">The node to serialize.</param>
    /// <returns>JSON representation of the node.</returns>
    public string Serialize(Node node)
    {
        return JsonSerializer.Serialize(node, _options);
    }

    /// <summary>
    /// Deserializes a JSON string to a Node.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized Node.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization returns null.</exception>
    public Node Deserialize(string json)
    {
        return JsonSerializer.Deserialize<Node>(json, _options)
            ?? throw new InvalidOperationException("Deserialization returned null");
    }
}
