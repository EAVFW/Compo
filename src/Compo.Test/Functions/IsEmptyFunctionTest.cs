using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test.Functions;

public class IsEmptyFunctionTest
{
    private static IExpressionEvaluator CreateEvaluator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IExpressionEvaluator>();
    }

    [Theory]
    [InlineData("@isEmpty('')", true)]
    [InlineData("@isEmpty(' ')", true)]
    [InlineData("@isEmpty('  ')", true)]
    [InlineData("@isEmpty('   ')", true)]
    [InlineData("@isEmpty('hello')", false)]
    [InlineData("@isEmpty('hello world')", false)]
    [InlineData("@isEmpty(' hello ')", false)]
    public void IsEmpty_ShouldCheckIfStringIsEmpty(string expression, bool expected)
    {
        var evaluator = CreateEvaluator();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(expected);
    }

    [Fact]
    public void IsEmpty_WithTrim_ShouldWorkTogether()
    {
        var evaluator = CreateEvaluator();
        var engine = new ExpressionParser();

        // Test isEmpty with trim - common pattern
        var expression = "@isEmpty(trim('  hello  '))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(false, "trimmed 'hello' is not empty");
    }

    [Fact]
    public void IsEmpty_WithTrimOnWhitespace_ShouldReturnTrue()
    {
        var evaluator = CreateEvaluator();
        var engine = new ExpressionParser();

        var expression = "@isEmpty(trim('   '))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(true, "trimmed whitespace results in empty string");
    }

    [Fact]
    public void IsEmpty_WithIf_ShouldEnableConditionalLogic()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Test the use case from the requirement
        var expression = "@if(isEmpty(trim(payload()['category'])), 'default', trim(payload()['category']))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("default", "empty category should return default value");
    }

    [Fact]
    public void IsEmpty_WithIf_NonEmptyValue_ShouldReturnValue()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunctionWithValue>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@if(isEmpty(trim(payload()['category'])), 'default', trim(payload()['category']))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be("electronics", "non-empty category should return the category value");
    }

    public class PayloadFunction : IFunction<IDictionary<string, object>>
    {
        public IDictionary<string, object> Execute()
        {
            return new Dictionary<string, object>
            {
                { "category", "   " } // Whitespace only
            };
        }
    }

    public class PayloadFunctionWithValue : IFunction<IDictionary<string, object>>
    {
        public IDictionary<string, object> Execute()
        {
            return new Dictionary<string, object>
            {
                { "category", "  electronics  " } // Non-empty value with whitespace
            };
        }
    }
}
