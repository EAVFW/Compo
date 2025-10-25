using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

public class NullConditionalAccessTest
{
    /// <summary>
    /// Function that returns a dictionary with some keys to simulate a data object
    /// </summary>
    public class DataFunction : IFunction<IDictionary<string, object>>
    {
        private readonly IDictionary<string, object> _data = new Dictionary<string, object>
        {
            { "name", "John" },
            { "age", 30 },
            { "city", "New York" },
            // Note: 'email' and 'phone' keys are intentionally missing
        };

        public IDictionary<string, object> Execute()
        {
            return _data;
        }
    }

    /// <summary>
    /// Test that accessing a missing key with ? returns null instead of throwing
    /// </summary>
    [Theory]
    [InlineData("@data()?['email']", null)]
    [InlineData("@data()['name']", "John")]
    [InlineData("@data()?['name']", "John")]
    [InlineData("@data()?['phone']", null)]
    public void NullConditionalAccess_ShouldReturnNull_WhenKeyDoesNotExist(string expression, object? expectedResult)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().Be(expectedResult);
    }

    /// <summary>
    /// Test that accessing a missing key without ? throws KeyNotFoundException
    /// </summary>
    [Fact]
    public void RegularAccess_ShouldThrow_WhenKeyDoesNotExist()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@data()['email']";
        var ast = engine.BuildAst(expression).Value!;

        Action act = () => expressionEvaluator.Evaluate(ast);

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("Key 'email' not found");
    }

    /// <summary>
    /// Test nested null-conditional access - handles both missing keys and non-dictionary values
    /// </summary>
    [Theory]
    [InlineData("@data()?['address']?['street']", null)]
    [InlineData("@data()?['name']?['length']", null)]
    public void NullConditionalAccess_ShouldHandleNestedMissingKeys(string expression, object? expectedResult)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().Be(expectedResult);
    }
}
