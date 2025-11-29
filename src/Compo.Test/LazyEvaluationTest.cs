using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

public class LazyEvaluationTest
{
    /// <summary>
    /// Function that throws an exception when called
    /// </summary>
    public class ThrowFunction : IFunction<string>
    {
        public string Execute()
        {
            throw new InvalidOperationException("This function should not be called due to lazy evaluation!");
        }
    }

    [Fact]
    public void If_WithTrueCondition_ShouldNotEvaluateFalseBranch()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<ThrowFunction>("throw");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // throw() is in the false branch, so it should NOT be evaluated
        var expression = "@if(true, 'success', throw())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("success", "true branch should be returned without evaluating false branch");
    }

    [Fact]
    public void If_WithFalseCondition_ShouldNotEvaluateTrueBranch()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<ThrowFunction>("throw");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // throw() is in the true branch, so it should NOT be evaluated
        var expression = "@if(false, throw(), 'success')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("success", "false branch should be returned without evaluating true branch");
    }

    [Fact]
    public void If_WithEmptyCheck_AndThrowingFunction_ShouldNotEvaluateWhenEmpty()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<ThrowFunction>("throw");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Real-world scenario: if field is empty, return null, otherwise call function
        var expression = "@if(isEmpty(''), null, throw())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeNull("empty string should return null without calling throw()");
    }

    [Fact]
    public void If_WithNonEmptyCheck_AndThrowingInNullBranch_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<ThrowFunction>("throw");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // If field is NOT empty, don't call the throw in the null branch
        var expression = "@if(isEmpty('value'), throw(), 'success')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("success", "non-empty string should return success without calling throw()");
    }

    [Fact]
    public void If_WithNestedIf_ShouldLazilyEvaluate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<ThrowFunction>("throw");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Nested if with throw() in unreachable branch
        var expression = "@if(true, 'outer-true', if(true, throw(), 'inner-false'))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("outer-true", "outer true branch should prevent inner if evaluation");
    }

    [Fact]
    public void If_WithNumericTypes_ShouldLazilyEvaluate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Test with integers  - result is string because of object? overload matching
        var expression = "@if(true, 42, div(1, 0))"; // div by zero in false branch
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().NotBeNull("should return a result without evaluating division by zero");
        result.ToString().Should().Be("42");
    }

    [Fact]
    public void If_WithComplexExpression_ShouldLazilyEvaluate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.RegisterFunction<ThrowFunction>("picklist");  // Simulate picklist that throws
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Simulates: @if(isEmpty(trim(payload()['field'])), null, picklist(trim(payload()['field'])))
        var expression = "@if(isEmpty(trim(payload()['category'])), null, picklist())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeNull("empty category should return null without calling picklist");
    }

    public class PayloadFunction : IFunction<IDictionary<string, object>>
    {
        public IDictionary<string, object> Execute()
        {
            return new Dictionary<string, object>
            {
                { "category", "   " } // Empty after trim
            };
        }
    }
}