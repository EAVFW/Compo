using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

/// <summary>
/// Regression tests for dot notation parsing in function arguments.
/// Ensures that dot notation properly stops at commas when used in function calls.
/// </summary>
public class DotNotationWithCommasTest
{
    /// <summary>
    /// Test object with a field to access via dot notation.
    /// </summary>
    public class TestObject : IAccessNode
    {
        public string Name { get; set; } = "Alice";

        public bool TryGetValue(string key, out object? value)
        {
            if (key == "Name")
            {
                value = Name;
                return true;
            }

            value = null;
            return false;
        }
    }

    /// <summary>
    /// Function that returns a test object.
    /// </summary>
    public class GetObjectFunction : IFunction<TestObject>
    {
        public TestObject Execute()
        {
            return new TestObject { Name = "Alice" };
        }
    }

    /// <summary>
    /// Equals function for testing.
    /// </summary>
    public class EqualsFunction : IFunction<object, object, bool>
    {
        public bool Execute(object a, object b)
        {
            return Equals(a, b);
        }
    }

    [Fact]
    public void DotNotation_InFunctionCall_ShouldParseArgumentsCorrectly()
    {
        // This test verifies the parser fix for: AnyCharExcept('[', ')', '.', '?', ',')
        // Before fix: @equals(obj().Name, 'test') parsed as 1 argument: "Name, 'test'"
        // After fix: @equals(obj().Name, 'test') parses as 2 arguments: "Name" and "'test'"

        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<GetObjectFunction>();
        services.AddScoped<EqualsFunction>();
        services.RegisterFunction<GetObjectFunction>("obj");
        services.RegisterFunction<EqualsFunction>("equals");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();

        // Test both bracket and dot notation
        var bracketAst = parser.BuildAst("@equals(obj()['Name'], 'Alice')");
        var dotAst = parser.BuildAst("@equals(obj().Name, 'Alice')");

        Assert.NotNull(bracketAst.Value);
        Assert.NotNull(dotAst.Value);

        // Both should parse as FunctionNode with 2 arguments
        var bracketFunc = Assert.IsType<FunctionNode>(bracketAst.Value);
        var dotFunc = Assert.IsType<FunctionNode>(dotAst.Value);

        Assert.Equal(2, bracketFunc.Arguments.Count);
        Assert.Equal(2, dotFunc.Arguments.Count); // This was 1 before the fix!

        // Act - evaluate both expressions
        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        var bracketResult = evaluator.Evaluate(bracketAst.Value);
        var dotResult = evaluator.Evaluate(dotAst.Value);

        // Assert - both should return true
        Assert.Equal(true, bracketResult);
        Assert.Equal(true, dotResult);
    }

    [Theory]
    [InlineData("@equals(obj().Name, 'Alice')", true)]
    [InlineData("@equals(obj().Name, 'Bob')", false)]
    [InlineData("@equals(obj()['Name'], 'Alice')", true)]
    [InlineData("@equals(obj()['Name'], 'Bob')", false)]
    public void DotNotation_WithDifferentValues_ShouldWorkCorrectly(string expression, bool expected)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<GetObjectFunction>();
        services.AddScoped<EqualsFunction>();
        services.RegisterFunction<GetObjectFunction>("obj");
        services.RegisterFunction<EqualsFunction>("equals");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        var ast = parser.BuildAst(expression);

        Assert.NotNull(ast.Value);

        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Act
        var result = evaluator.Evaluate(ast.Value);

        // Assert
        Assert.Equal(expected, result);
    }
}
