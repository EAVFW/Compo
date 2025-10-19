using Xunit;
using Compo;
using Compo.Serialization;
using FluentAssertions;

namespace Compo.Test.Serialization;

public class AstSerializerTests
{
    [Fact]
    public void Serialize_AccessNode_ShouldIncludeTypeDiscriminator()
    {
        // Arrange
        var serializer = new AstSerializer();
        var payloadFunc = new FunctionNode("payload", new List<Node>());
        var fieldName = new ValueNode<string>("firstName");
        var accessNode = new AccessNode(payloadFunc, fieldName, false);

        // Act
        var json = serializer.Serialize(accessNode);

        // Assert
        json.Should().Contain("\"$type\": \"access\"");
        json.Should().Contain("\"function\": \"payload\"");
        json.Should().Contain("\"value\": \"firstName\"");
    }

    [Fact]
    public void Deserialize_AccessNode_ShouldRecreateNode()
    {
        // Arrange
        var serializer = new AstSerializer();
        var json = """
        {
          "$type": "access",
          "node": {
            "$type": "function",
            "function": "payload",
            "arguments": []
          },
          "index": {
            "$type": "value_string",
            "value": "firstName"
          },
          "nulled": false
        }
        """;

        // Act
        var node = serializer.Deserialize(json);

        // Assert
        node.Should().BeOfType<AccessNode>();
        var accessNode = (AccessNode)node;
        accessNode.Node.Should().BeOfType<FunctionNode>();
        accessNode.Index.Should().BeOfType<ValueNode<string>>();
        ((ValueNode<string>)accessNode.Index).Value.Should().Be("firstName");
    }

    [Fact]
    public void SerializeDeserialize_FunctionNode_ShouldRoundTrip()
    {
        // Arrange
        var serializer = new AstSerializer();
        var args = new List<Node>
        {
            new ValueNode<string>("arg1"),
            new ValueNode<int>(42)
        };
        var functionNode = new FunctionNode("testFunc", args);

        // Act
        var json = serializer.Serialize(functionNode);
        var deserialized = serializer.Deserialize(json);

        // Assert
        deserialized.Should().BeOfType<FunctionNode>();
        var fn = (FunctionNode)deserialized;
        fn.Function.Should().Be("testFunc");
        fn.Arguments.Should().HaveCount(2);
    }

    [Fact]
    public void Serialize_ValueNodes_ShouldHandleDifferentTypes()
    {
        // Arrange
        var serializer = new AstSerializer();

        // Act & Assert - String
        var stringNode = new ValueNode<string>("test");
        var stringJson = serializer.Serialize(stringNode);
        stringJson.Should().Contain("\"$type\": \"value_string\"");
        var deserializedString = serializer.Deserialize(stringJson);
        deserializedString.Should().BeOfType<ValueNode<string>>();
        ((ValueNode<string>)deserializedString).Value.Should().Be("test");

        // Act & Assert - Int
        var intNode = new ValueNode<int>(123);
        var intJson = serializer.Serialize(intNode);
        intJson.Should().Contain("\"$type\": \"value_int\"");
        var deserializedInt = serializer.Deserialize(intJson);
        deserializedInt.Should().BeOfType<ValueNode<int>>();
        ((ValueNode<int>)deserializedInt).Value.Should().Be(123);

        // Act & Assert - Decimal
        var decimalNode = new ValueNode<decimal>(123.45m);
        var decimalJson = serializer.Serialize(decimalNode);
        decimalJson.Should().Contain("\"$type\": \"value_decimal\"");
        var deserializedDecimal = serializer.Deserialize(decimalJson);
        deserializedDecimal.Should().BeOfType<ValueNode<decimal>>();
        ((ValueNode<decimal>)deserializedDecimal).Value.Should().Be(123.45m);

        // Act & Assert - Bool
        var boolNode = new ValueNode<bool>(true);
        var boolJson = serializer.Serialize(boolNode);
        boolJson.Should().Contain("\"$type\": \"value_bool\"");
        var deserializedBool = serializer.Deserialize(boolJson);
        deserializedBool.Should().BeOfType<ValueNode<bool>>();
        ((ValueNode<bool>)deserializedBool).Value.Should().BeTrue();
    }

    [Fact]
    public void SerializeDeserialize_ComplexExpression_ShouldRoundTrip()
    {
        // Arrange - Simulate @payload()['firstName']
        var serializer = new AstSerializer();
        var parser = new ExpressionParser();
        var parseResult = parser.BuildAst("@payload()['firstName']");

        // Act
        var json = serializer.Serialize(parseResult.Value!);
        var deserialized = serializer.Deserialize(json);

        // Assert
        deserialized.Should().BeOfType<AccessNode>();
        var accessNode = (AccessNode)deserialized;
        accessNode.Node.Should().BeOfType<FunctionNode>();
        var funcNode = (FunctionNode)accessNode.Node;
        funcNode.Function.Should().Be("payload");
    }

    [Fact]
    public void Constructor_WithCompactOption_ShouldNotIndent()
    {
        // Arrange
        var serializer = new AstSerializer(writeIndented: false);
        var node = new ValueNode<string>("test");

        // Act
        var json = serializer.Serialize(node);

        // Assert
        json.Should().NotContain("\n");
        json.Should().NotContain("  ");
    }
}
