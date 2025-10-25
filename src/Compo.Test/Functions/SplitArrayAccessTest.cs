using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test.Functions;

public class SplitArrayAccessTest
{
    [Fact]
    public void Split_ShouldAllowIndexAccess_ToAllElements()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Test accessing all three elements like in the real use case
        var expr0 = "@split('one two tree', ' ')[0]";
        var expr1 = "@split('one two tree', ' ')[1]";
        var expr2 = "@split('one two tree', ' ')[2]";

        var result0 = evaluator.Evaluate(engine.BuildAst(expr0).Value!);
        var result1 = evaluator.Evaluate(engine.BuildAst(expr1).Value!);
        var result2 = evaluator.Evaluate(engine.BuildAst(expr2).Value!);

        result0.Should().Be("one");
        result1.Should().Be("two");
        result2.Should().Be("tree");
    }

    [Fact]
    public void Split_WithPayload_ShouldAllowIndexAccess()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register a payload function
        services.RegisterFunction<PayloadFunction>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Test use case with payload field access
        var expr0 = "@split(payload()['product_code'], ' ')[0]";
        var expr1 = "@split(payload()['product_code'], ' ')[1]";
        var expr2 = "@split(payload()['product_code'], ' ')[2]";

        var result0 = evaluator.Evaluate(engine.BuildAst(expr0).Value!);
        var result1 = evaluator.Evaluate(engine.BuildAst(expr1).Value!);
        var result2 = evaluator.Evaluate(engine.BuildAst(expr2).Value!);

        result0.Should().Be("one");
        result1.Should().Be("two");
        result2.Should().Be("tree");
    }

    public class PayloadFunction : IFunction<IDictionary<string, object>>
    {
        public IDictionary<string, object> Execute()
        {
            return new Dictionary<string, object>
            {
                { "product_code", "one two tree" }
            };
        }
    }
}
