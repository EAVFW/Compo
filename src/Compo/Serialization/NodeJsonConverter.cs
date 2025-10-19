using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compo.Serialization;

/// <summary>
/// JSON converter for polymorphic Node serialization/deserialization.
/// Uses $type discriminator to handle different Node implementations.
/// </summary>
public class NodeJsonConverter : JsonConverter<Node>
{
    public override Node? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("$type", out var typeProperty))
        {
            throw new JsonException("Missing $type discriminator in Node JSON");
        }

        var nodeType = typeProperty.GetString();

        return nodeType switch
        {
            "function" => DeserializeFunctionNode(root),
            "access" => DeserializeAccessNode(root, options),
            "value_string" => new ValueNode<string>(root.GetProperty("value").GetString()!),
            "value_int" => new ValueNode<int>(root.GetProperty("value").GetInt32()),
            "value_decimal" => new ValueNode<decimal>(root.GetProperty("value").GetDecimal()),
            "value_bool" => new ValueNode<bool>(root.GetProperty("value").GetBoolean()),
            _ => throw new JsonException($"Unknown node type: {nodeType}")
        };
    }

    private FunctionNode DeserializeFunctionNode(JsonElement element)
    {
        var function = element.GetProperty("function").GetString()!;
        var argsElement = element.GetProperty("arguments");

        var arguments = new List<Node>();
        foreach (var argElement in argsElement.EnumerateArray())
        {
            var argJson = argElement.GetRawText();
            var argNode = JsonSerializer.Deserialize<Node>(argJson, new JsonSerializerOptions
            {
                Converters = { new NodeJsonConverter() }
            });
            if (argNode != null)
            {
                arguments.Add(argNode);
            }
        }

        return new FunctionNode(function, arguments);
    }

    private AccessNode DeserializeAccessNode(JsonElement element, JsonSerializerOptions options)
    {
        var nodeJson = element.GetProperty("node").GetRawText();
        var indexJson = element.GetProperty("index").GetRawText();
        var nulled = element.GetProperty("nulled").GetBoolean();

        var node = JsonSerializer.Deserialize<Node>(nodeJson, new JsonSerializerOptions
        {
            Converters = { new NodeJsonConverter() }
        });
        var index = JsonSerializer.Deserialize<Node>(indexJson, new JsonSerializerOptions
        {
            Converters = { new NodeJsonConverter() }
        });

        return new AccessNode(node!, index!, nulled);
    }

    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case FunctionNode fn:
                writer.WriteString("$type", "function");
                writer.WriteString("function", fn.Function);
                writer.WritePropertyName("arguments");
                writer.WriteStartArray();
                foreach (var arg in fn.Arguments)
                {
                    JsonSerializer.Serialize(writer, arg, options);
                }
                writer.WriteEndArray();
                break;

            case AccessNode an:
                writer.WriteString("$type", "access");
                writer.WritePropertyName("node");
                JsonSerializer.Serialize(writer, an.Node, options);
                writer.WritePropertyName("index");
                JsonSerializer.Serialize(writer, an.Index, options);
                writer.WriteBoolean("nulled", an.Nulled);
                break;

            case ValueNode<string> vs:
                writer.WriteString("$type", "value_string");
                writer.WriteString("value", vs.Value);
                break;

            case ValueNode<int> vi:
                writer.WriteString("$type", "value_int");
                writer.WriteNumber("value", vi.Value);
                break;

            case ValueNode<decimal> vd:
                writer.WriteString("$type", "value_decimal");
                writer.WriteNumber("value", vd.Value);
                break;

            case ValueNode<bool> vb:
                writer.WriteString("$type", "value_bool");
                writer.WriteBoolean("value", vb.Value);
                break;

            default:
                throw new JsonException($"Unknown node type: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}
