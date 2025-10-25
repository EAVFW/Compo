using System.Text.Json;
using Compo.Serialization;

namespace Compo.Test;

public class ASTSerializationTest
{
    [Fact]
    public void AST_ShouldPreserveSpaceSeparator()
    {
        var engine = new ExpressionParser();
        var expression = "@split(payload()['category'], ' ')";
        var ast = engine.BuildAst(expression);

        ast.Success.Should().BeTrue();

        // Serialize the AST
        var serializer = new AstSerializer();
        var json = serializer.Serialize(ast.Value!);

        Console.WriteLine($"Serialized AST: {json}");

        // Check that the space is preserved in the JSON
        json.Should().Contain("\"value\":\" \"", "the space separator should be preserved in the AST");

        // Deserialize and verify
        var deserialized = serializer.Deserialize(json);
        var functionNode = deserialized as FunctionNode;
        functionNode.Should().NotBeNull();
        functionNode!.Function.Should().Be("split");
        functionNode.Arguments.Should().HaveCount(2);

        var secondArg = functionNode.Arguments[1] as ValueNode<string>;
        secondArg.Should().NotBeNull();
        secondArg!.Value.Should().Be(" ", "the space separator should be preserved after deserialization");
    }

    [Fact]
    public void AST_ComplexExpression_ShouldPreserveSpaceSeparator()
    {
        var engine = new ExpressionParser();
        var expression = "@lookup('product','product_key', split(payload()['product_code'],' ')[0],split(payload()['product_code'],' ')[1],split(payload()['product_code'],' ')[2])";
        var ast = engine.BuildAst(expression);

        ast.Success.Should().BeTrue();

        // Serialize the AST
        var serializer = new AstSerializer();
        var json = serializer.Serialize(ast.Value!);

        Console.WriteLine($"Expression: {expression}");
        Console.WriteLine($"Serialized AST: {json}");

        // Check that ALL space separators are preserved
        var spaceCount = json.Split(new[] { "\"value\":\" \"" }, StringSplitOptions.None).Length - 1;
        spaceCount.Should().Be(3, "there should be 3 space separators in the expression");
    }
}
