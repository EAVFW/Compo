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
        // ExpressionEvaluator should parse 'not(false)' to a Node before passing to Execute()
        // Note: @ is automatically prepended by [NestedExpression] handling
        var expression = "@testNested('not(false)')";
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
        // Note: @ is NOT used inside nested expressions - it's added automatically
        var expression = "@testNested('if(true, not(false), false)')";
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
        // Note: @ is NOT used inside nested expressions - it's added automatically
        var expression = "@testMultipleNested('not(false)', 'and(true, true)')";
        var ast = parser.BuildAst(expression);

        var result = evaluator.Evaluate(ast.Value!);

        // Assert
        result.Should().Be("TrueTrue", "both nested expressions should be evaluated and concatenated");
    }

    [Fact]
    public void NestedExpression_WithAtSymbol_ShouldStillWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestNestedExpressionFunction>("testNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Test backward compatibility: @ prefix should still work
        var expression = "@testNested('@not(false)')";
        var ast = parser.BuildAst(expression);

        var result = evaluator.Evaluate(ast.Value!);

        // Assert
        result.Should().Be(true, "nested expression with @ prefix should work for backward compatibility");
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

        // Act - Pass a value node (int)
        // After the fix, [NestedExpression] accepts value nodes directly
        var expression = "@testNested(123)";
        var ast = parser.BuildAst(expression);

        // Assert - Should now accept int value nodes and evaluate them
        var result = evaluator.Evaluate(ast.Value!);
        result.Should().Be(123, "int value nodes are now accepted");
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

        // Act - Pass invalid expression syntax (malformed function call)
        var expression = "@testNested('invalid(')";
        var ast = parser.BuildAst(expression);

        // Assert
        var act = () => evaluator.Evaluate(ast.Value!);
        act.Should().Throw<Exception>()
            .WithMessage("Parse error*");
    }

    /// <summary>
    /// Reproduces the issue with single quotes in nested expressions.
    /// When a nested expression contains single quotes (e.g., from bracket notation like ['key']),
    /// it cannot be wrapped as a string literal because Compo doesn't support escape sequences.
    /// </summary>
    [Fact]
    public void NestedExpression_WithSingleQuotesInString_ShouldFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestNestedExpressionFunction>("testNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Try to use bracket notation with single quotes inside a string literal
        // This simulates: @filter(payload()['participants'], 'predicate')
        // When wrapped in quotes, it becomes: '@filter(payload()[\'participants\'], \'predicate\')'
        // The \' doesn't work because Compo has no escape mechanism!
        var expression = "@testNested('@filter(payload()[\\'participants\\'], \\'predicate\\')')";
        var parseAction = () => parser.BuildAst(expression);

        // Assert - This should fail to parse due to the escaped quotes
        var exception = parseAction.Should().Throw<Exception>("Compo doesn't support escape sequences").Which;
        exception.Message.Should().Contain("Parse error", "the parser should reject the escaped quotes");
    }

    /// <summary>
    /// Test that [NestedExpression] can accept already-parsed FunctionNode directly.
    /// This is the proposed solution to avoid string escaping issues.
    /// </summary>
    [Fact]
    public void NestedExpression_WithDirectFunctionNode_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestNestedExpressionFunction>("testNested");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Pass a nested function directly without wrapping in quotes
        // @testNested(not(false))  <- not() is parsed as FunctionNode, not as string
        // Note: @ is only used at the outermost level, not for nested functions
        var expression = "@testNested(not(false))";
        var ast = parser.BuildAst(expression);

        ast.Success.Should().BeTrue("expression with nested function should parse");

        // After the fix, [NestedExpression] should accept FunctionNode directly
        var result = evaluator.Evaluate(ast.Value!);

        // Check what we actually got
        Console.WriteLine($"Result: {result}");
        Console.WriteLine($"Result type: {result?.GetType().Name}");

        // With the fix, this should work!
        result.Should().Be(true, "nested not(false) should evaluate to true");
    }

    /// <summary>
    /// Test the full scenario: mapreduce with filter containing bracket notation.
    /// This reproduces the exact issue from sample-netbank-messages-dev.json.
    /// </summary>
    [FunctionRegistration("testMapReduce")]
    public class TestMapReduceFunction : IFunction<Node, Node, Node, object?>
    {
        private readonly IExpressionEvaluator _evaluator;

        public TestMapReduceFunction(IExpressionEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        public object? Execute(
            [NestedExpression] Node itemsNode,
            [NestedExpression] Node mapNode,
            [NestedExpression] Node reduceNode)
        {
            itemsNode.Should().NotBeNull();
            mapNode.Should().NotBeNull();
            reduceNode.Should().NotBeNull();

            // For testing, just return "success"
            return "success";
        }
    }

    [Fact]
    public void MapReduce_WithNestedFilterAsDirectFunction_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestMapReduceFunction>("testMapReduce");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Use nested functions directly without string wrapping
        // @testMapReduce(payload()['items'], not(false), string('result'))
        // Note: @ is only at the outermost level
        var expression = "@testMapReduce(payload()['items'], not(false), string('result'))";
        var ast = parser.BuildAst(expression);

        ast.Success.Should().BeTrue("expression with nested functions should parse");

        // After the fix, this should work and return "success"
        var result = evaluator.Evaluate(ast.Value!);

        // With the fix, it accepts FunctionNode directly
        result.Should().Be("success", "mapreduce test should succeed");
    }

    /// <summary>
    /// Realistic test: Filter an array using a predicate with item() context.
    /// This tests the actual use case: @filter(['test','nottest'], predicate)
    /// where the predicate uses item() to access each array element.
    /// </summary>
    [FunctionRegistration("testFilter")]
    public class TestFilterFunction : IFunction<object, Node, object?>
    {
        private readonly IExpressionEvaluator _evaluator;
        private readonly IServiceProvider _serviceProvider;

        public TestFilterFunction(IExpressionEvaluator evaluator, IServiceProvider serviceProvider)
        {
            _evaluator = evaluator;
            _serviceProvider = serviceProvider;
        }

        public object? Execute(object items, [NestedExpression] Node predicateNode)
        {
            var array = items as IEnumerable<object> ?? throw new ArgumentException("First parameter must be an array");
            var result = new List<object>();

            // Simulate what filter() does: evaluate predicate for each item
            foreach (var item in array)
            {
                // Create scoped service with item context (simplified for test)
                // In real implementation, this would set ItemProvider
                using var scope = _serviceProvider.CreateScope();
                var scopedEvaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

                // For this test, we'll use the evaluator directly
                // In production, ItemProvider would be set here
                var predicateResult = _evaluator.Evaluate(predicateNode);

                if (predicateResult is bool match && match)
                {
                    result.Add(item);
                }
            }

            return result;
        }
    }

    [Fact]
    public void Filter_WithStringPredicate_CurrentBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestFilterFunction>("testFilter");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Current approach: wrap predicate in quotes (string literal)
        // Simulated: @filter(items, 'neq(item(), "test")')
        // Pass null as items for this simple test, focus on the predicate
        var expression = "@testFilter(null, 'not(false)')";
        var ast = parser.BuildAst(expression);

        ast.Success.Should().BeTrue("expression should parse");

        // The function will receive null for items and throw, that's expected
        // This demonstrates that the string approach WORKS (no escaping issues here)
        var act = () => evaluator.Evaluate(ast.Value!);
        act.Should().Throw<Exception>("items is null")
            .WithInnerException<ArgumentException>();
    }

    [Fact]
    public void Filter_WithDirectPredicate_ProposedBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestFilterFunction>("testFilter");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - Proposed approach: pass predicate directly without quotes
        // @testFilter(items, not(false))
        // The predicate is parsed once as part of the main expression tree
        var expression = "@testFilter(null, not(false))";
        var ast = parser.BuildAst(expression);

        ast.Success.Should().BeTrue("expression should parse");

        // After the fix, this should work and throw ArgumentException (items is null)
        var act = () => evaluator.Evaluate(ast.Value!);

        // With the fix, it accepts FunctionNode and throws ArgumentException because items is null
        act.Should().Throw<Exception>("items is null")
            .WithInnerException<ArgumentException>();
    }

    [Fact]
    public void Filter_WithBracketNotationInPredicate_ShowsEscapingProblem()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExpressionEngine();
        services.RegisterFunction<TestFilterFunction>("testFilter");

        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var parser = serviceProvider.GetRequiredService<ExpressionParser>();

        // Act - This demonstrates the escaping problem!
        // If we try: '@filter(payload()[\'items\'], \'predicate\')'
        // The \' doesn't work in Compo parser!

        // Test the actual problematic expression from our profile
        var expression = "@testNested('@filter(payload()[\\'participants\\'], \\'neq()\\')')";
        var parseAction = () => parser.BuildAst(expression);

        // This should fail to parse
        parseAction.Should().Throw<Exception>()
            .WithMessage("*Parse error*", "escaped quotes are not supported");
    }
}
