using Microsoft.Extensions.DependencyInjection;
using Compo.Serialization;

namespace Compo.Test;

public class RealWorldExpressionTest
{
    /// <summary>
    /// Test a complex real-world expression with nested functions, null literals, and multiple arguments
    /// </summary>
    [Fact]
    public void ComplexExpression_WithNullAndNestedFunctions_ShouldParse()
    {
        var engine = new ExpressionParser();

        var expression = "@if(isEmpty(trim('  ')), null, concat('a', 'b'))";
        var ast = engine.BuildAst(expression);

        ast.Success.Should().BeTrue();

        var functionNode = ast.Value as FunctionNode;
        functionNode.Should().NotBeNull();
        functionNode!.Function.Should().Be("if");
        functionNode.Arguments.Should().HaveCount(3);

        // First argument: isEmpty(trim('  '))
        var condition = functionNode.Arguments[0] as FunctionNode;
        condition.Should().NotBeNull();
        condition!.Function.Should().Be("isEmpty");

        // Second argument: null
        var trueValue = functionNode.Arguments[1] as ValueNode<object?>;
        trueValue.Should().NotBeNull();
        trueValue!.Value.Should().BeNull();

        // Third argument: concat('a', 'b')
        var falseValue = functionNode.Arguments[2] as FunctionNode;
        falseValue.Should().NotBeNull();
        falseValue!.Function.Should().Be("concat");
    }

    [Fact]
    public void ComplexExpression_WithNullAndNestedFunctions_ShouldEvaluate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Test with empty value - should return null
        var expression1 = "@if(isEmpty(trim('  ')), null, concat('a', 'b'))";
        var result1 = evaluator.Evaluate(engine.BuildAst(expression1).Value!);
        result1.Should().BeNull("empty trimmed value should return null");

        // Test with non-empty value - should return concat result
        var expression2 = "@if(isEmpty(trim('test')), null, concat('a', 'b'))";
        var result2 = evaluator.Evaluate(engine.BuildAst(expression2).Value!);
        result2.Should().Be("ab", "non-empty value should return concat result");
    }

    [Fact]
    public void NullLiteral_ShouldSerializeAndDeserialize()
    {
        var engine = new ExpressionParser();
        var expression = "@if(true, null, 'value')";
        var ast = engine.BuildAst(expression);

        // Serialize
        var serializer = new AstSerializer();
        var json = serializer.Serialize(ast.Value!);

        json.Should().Contain("\"value\": null", "null should be serialized as JSON null");

        // Deserialize
        var deserialized = serializer.Deserialize(json);
        var functionNode = deserialized as FunctionNode;
        functionNode.Should().NotBeNull();

        var nullArg = functionNode!.Arguments[1] as ValueNode<object?>;
        nullArg.Should().NotBeNull();
        nullArg!.Value.Should().BeNull();
    }
}
