using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

/// <summary>
/// Tests to verify that Dictionary&lt;string, object?&gt; (with nullable values) works with Compo's bracket notation.
/// This is the actual type we use in production code.
/// </summary>
public class DictionaryNullableValueTest
{
    /// <summary>
    /// Function that returns a Dictionary&lt;string, object?&gt; matching our production code.
    /// </summary>
    public class TestFunction : IFunction<IDictionary<string, object?>>
    {
        public IDictionary<string, object?> Execute()
        {
            // Simulate our JsonElementToDictionary output with nested dictionaries
            var senderDict = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { "userId", "EMP001" },
                { "name", "Employee Name" },
                { "userType", "EMPLOYEE" }
            };

            var result = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { "sender", senderDict },
                { "id", "msg-001" },
                { "content", "Test message" }
            };

            return result;
        }
    }

    /// <summary>
    /// Tests that bracket notation works on Dictionary&lt;string, object?&gt;.
    /// </summary>
    [Fact]
    public void DictionaryWithNullableValues_BracketNotation_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestFunction>();
        services.RegisterFunction<TestFunction>("test");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        var ast = parser.BuildAst("@test()['sender']['userId']");

        Assert.NotNull(ast.Value);

        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Act
        var result = evaluator.Evaluate(ast.Value);

        // Assert
        Assert.Equal("EMP001", result);
    }

    /// <summary>
    /// Tests that dot notation works on Dictionary&lt;string, object?&gt;.
    /// </summary>
    [Fact]
    public void DictionaryWithNullableValues_DotNotation_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestFunction>();
        services.RegisterFunction<TestFunction>("test");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        var ast = parser.BuildAst("@test().sender.userId");

        Assert.NotNull(ast.Value);

        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Act
        var result = evaluator.Evaluate(ast.Value);

        // Assert
        Assert.Equal("EMP001", result);
    }

    /// <summary>
    /// Tests that both syntaxes return the same result.
    /// </summary>
    [Fact]
    public void DictionaryWithNullableValues_BothSyntaxes_ShouldReturnSameValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestFunction>();
        services.RegisterFunction<TestFunction>("test");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Test bracket notation
        var bracketAst = parser.BuildAst("@test()['sender']['userId']");
        Assert.NotNull(bracketAst.Value);
        var bracketResult = evaluator.Evaluate(bracketAst.Value);

        // Test dot notation
        var dotAst = parser.BuildAst("@test().sender.userId");
        Assert.NotNull(dotAst.Value);
        var dotResult = evaluator.Evaluate(dotAst.Value);

        // Assert
        Assert.Equal(bracketResult, dotResult);
        Assert.Equal("EMP001", bracketResult);
    }
}
