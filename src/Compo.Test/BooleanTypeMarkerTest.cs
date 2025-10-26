using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

public class BooleanTypeMarkerTest
{
    /// <summary>
    /// Test that demonstrates True/False marker types work with open generics
    /// </summary>
    [Fact]
    public void If_WithTrueMarker_ReturnsLeftType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Expression: if(true, 'left', 42)
        // Should return 'left' as string
        var expression = "@if(true, 'left', 42)";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("left");
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void If_WithFalseMarker_ReturnsRightType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Expression: if(false, 'left', 42)
        // Should return 42 as int
        var expression = "@if(false, 'left', 42)";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(42);
    }

    /// <summary>
    /// Mock OptionValue to simulate the real xrm-sink type
    /// </summary>
    public readonly record struct MockOptionValue(int Value)
    {
        public static implicit operator MockOptionValue(int value) => new(value);
        public static implicit operator int(MockOptionValue option) => option.Value;
    }

    /// <summary>
    /// Mock picklist function that returns OptionValue
    /// </summary>
    [FunctionRegistration("mockPicklist")]
    public class MockPicklistFunction : IFunction<MockOptionValue>
    {
        public MockOptionValue Execute()
        {
            return new MockOptionValue(100);
        }
    }

    [Fact]
    public void If_WithCustomStructType_PreservesType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockPicklistFunction>("mockPicklist");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Expression: if(false, null, mockPicklist())
        // Should return MockOptionValue
        var expression = "@if(false, null, mockPicklist())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().NotBeNull();
        result.Should().BeOfType<MockOptionValue>();
        ((MockOptionValue)result!).Value.Should().Be(100);
    }

    [Fact]
    public void If_WithEmptyCheck_AndCustomStruct_ReturnsNull()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockPicklistFunction>("mockPicklist");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Expression: if(isEmpty(''), null, mockPicklist())
        // Should return null WITHOUT evaluating mockPicklist
        var expression = "@if(isEmpty(''), null, mockPicklist())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeNull();
    }

    [Fact]
    public void If_WithNonEmptyCheck_AndCustomStruct_ReturnsStruct()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockPicklistFunction>("mockPicklist");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Expression: if(isEmpty('value'), null, mockPicklist())
        // Should return MockOptionValue
        var expression = "@if(isEmpty('value'), null, mockPicklist())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().NotBeNull();
        result.Should().BeOfType<MockOptionValue>();
        ((MockOptionValue)result!).Value.Should().Be(100);
    }
}
