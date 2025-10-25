using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

public class ParserNestedFunctionsTest
{
    [Fact]
    public void Parser_ShouldHandleNestedFunctionsWithMultipleArguments()
    {
        var engine = new ExpressionParser();

        // Test the problematic expression structure
        var expression = "@if(isEmpty(trim('  ')), 'default', 'value')";
        var ast = engine.BuildAst(expression);

        ast.Success.Should().BeTrue();

        var functionNode = ast.Value as FunctionNode;
        functionNode.Should().NotBeNull();
        functionNode!.Function.Should().Be("if", "the outer function should be 'if'");
        functionNode.Arguments.Should().HaveCount(3, "if should have 3 arguments: condition, true_value, false_value");

        // First argument should be isEmpty(trim('  '))
        var firstArg = functionNode.Arguments[0] as FunctionNode;
        firstArg.Should().NotBeNull();
        firstArg!.Function.Should().Be("isEmpty");

        // Second argument should be 'default'
        var secondArg = functionNode.Arguments[1] as ValueNode<string>;
        secondArg.Should().NotBeNull();
        secondArg!.Value.Should().Be("default");

        // Third argument should be 'value'
        var thirdArg = functionNode.Arguments[2] as ValueNode<string>;
        thirdArg.Should().NotBeNull();
        thirdArg!.Value.Should().Be("value");
    }

    [Fact]
    public void Parser_ShouldHandleComplexNestedExpression()
    {
        var engine = new ExpressionParser();

        // Simplified version of the real use case
        var expression = "@if(isEmpty('test'), null, concat('a', 'b'))";
        var ast = engine.BuildAst(expression);

        ast.Success.Should().BeTrue();

        var functionNode = ast.Value as FunctionNode;
        functionNode.Should().NotBeNull();
        functionNode!.Function.Should().Be("if");
        functionNode.Arguments.Should().HaveCount(3);

        // Third argument should be concat function
        var thirdArg = functionNode.Arguments[2] as FunctionNode;
        thirdArg.Should().NotBeNull();
        thirdArg!.Function.Should().Be("concat", "the third argument should be the concat function, not include 'null,' in the name");
    }

    [Fact]
    public void Parser_ShouldEvaluateComplexNestedExpression()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Test that the expression actually evaluates correctly
        var expression = "@if(isEmpty(trim('  ')), 'empty', 'not-empty')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("empty", "trimmed whitespace should be considered empty");
    }

    [Fact]
    public void Parser_ShouldHandleMultipleNestedFunctionsInDifferentArguments()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@if(isEmpty(''), concat('a', 'b'), concat('c', 'd'))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("ab", "empty string condition should return first concat result");
    }
}
