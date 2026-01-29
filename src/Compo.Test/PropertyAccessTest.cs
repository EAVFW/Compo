using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

/// <summary>
/// Tests to verify if Compo can access properties on C# objects returned by functions.
/// </summary>
public class PropertyAccessTest
{
    /// <summary>
    /// Simple C# object that implements IAccessNode to support bracket notation.
    /// </summary>
    public class TestObject : IAccessNode
    {
        public string Data { get; set; } = "hello world";

        public bool TryGetValue(string key, out object? value)
        {
            if (key == "Data")
            {
                value = Data;
                return true;
            }

            value = null;
            return false;
        }
    }

    /// <summary>
    /// Test function that returns a C# object with a Data property.
    /// </summary>
    public class TestFunction : IFunction<TestObject>
    {
        public TestObject Execute()
        {
            return new TestObject { Data = "hello world" };
        }
    }

    [Fact]
    public void CanAccessFieldViaIAccessNode()
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
        // Use bracket notation to access field via IAccessNode
        var ast = parser.BuildAst("@test()['Data']");

        Assert.NotNull(ast.Value);

        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Act
        var result = evaluator.Evaluate(ast.Value);

        // Assert
        Assert.Equal("hello world", result);
    }
}
