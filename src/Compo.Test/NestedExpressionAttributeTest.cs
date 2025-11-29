using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

/// <summary>
/// Tests for the [NestedExpression] attribute feature.
/// Functions marked with [NestedExpression] should receive pre-parsed Node parameters
/// instead of string parameters, avoiding runtime parsing overhead.
/// </summary>
public class NestedExpressionAttributeTest
{
    /// <summary>
    /// Test function that accepts a nested expression as a Node parameter.
    /// This simulates functions like filter() or mapreduce() that need to evaluate
    /// dynamic predicates or transformations.
    /// </summary>
    [FunctionRegistration("testNested")]
    public class TestNestedExpressionFunction : IFunction<Node, object?>
    {
        private readonly IExpressionEvaluator _evaluator;

        public TestNestedExpressionFunction(IExpressionEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        public object? Execute([NestedExpression] Node predicateNode)
        {
            // Verify we received a Node (not a string!) - this is the key test
            // The Node should be pre-parsed by ExpressionEvaluator.PrepareArguments()
            predicateNode.Should().NotBeNull();

            // Evaluate the pre-parsed Node
            return _evaluator.Evaluate(predicateNode);
        }
    }

    [Fact]
    public void NestedExpression_ShouldReceivePreParsedNode()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestNestedExpressionFunction>("testNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - The nested expression contains a function call
        // ExpressionEvaluator should parse '@not(false)' to a Node before passing to Execute()
        var expression = "@testNested('@not(false)')";
        var ast = parser.BuildAst(expression);

        ast.Success.Should().BeTrue("expression should parse successfully");

        var result = evaluator.Evaluate(ast.Value!);

        // Assert
        result.Should().Be(true, "nested expression should be evaluated correctly");
    }

    [Fact]
    public void NestedExpression_ShouldSupportComplexExpressions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestNestedExpressionFunction>("testNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Use a complex nested expression with nested function calls
        var expression = "@testNested('@if(true, @not(false), false)')";
        var ast = parser.BuildAst(expression);

        var result = evaluator.Evaluate(ast.Value!);

        // Assert
        result.Should().Be(true, "nested expression with multiple functions should evaluate correctly");
    }

    /// <summary>
    /// Test function with multiple parameters, some nested and some regular.
    /// This simulates more complex functions like mapreduce(items, map, reduce)
    /// where all three parameters are nested expressions.
    /// </summary>
    [FunctionRegistration("testMultipleNested")]
    public class TestMultipleNestedExpressionsFunction : IFunction<Node, Node, object?>
    {
        private readonly IExpressionEvaluator _evaluator;

        public TestMultipleNestedExpressionsFunction(IExpressionEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        public object? Execute(
            [NestedExpression] Node firstNode,
            [NestedExpression] Node secondNode)
        {
            var first = _evaluator.Evaluate(firstNode);
            var second = _evaluator.Evaluate(secondNode);

            // Concatenate the results
            return $"{first}{second}";
        }
    }

    [Fact]
    public void NestedExpression_ShouldSupportMultipleNestedParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestMultipleNestedExpressionsFunction>("testMultipleNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Call with two nested expressions
        var expression = "@testMultipleNested('@not(false)', '@and(true, true)')";
        var ast = parser.BuildAst(expression);

        var result = evaluator.Evaluate(ast.Value!);

        // Assert
        result.Should().Be("TrueTrue", "both nested expressions should be evaluated and concatenated");
    }

    [Fact]
    public void NestedExpression_InvalidArgumentType_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestNestedExpressionFunction>("testNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Pass a non-string value node (should fail)
        // This tests the error handling when [NestedExpression] receives wrong type
        var expression = "@testNested(123)";
        var ast = parser.BuildAst(expression);

        // Assert
        var act = () => evaluator.Evaluate(ast.Value!);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*[NestedExpression] parameter must receive a string value node*");
    }

    [Fact]
    public void NestedExpression_InvalidExpressionString_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestNestedExpressionFunction>("testNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Pass invalid expression syntax (missing @ prefix for function)
        var expression = "@testNested('invalid()')";
        var ast = parser.BuildAst(expression);

        // Assert
        var act = () => evaluator.Evaluate(ast.Value!);
        act.Should().Throw<Exception>()
            .WithMessage("Parse error*");
    }
}
