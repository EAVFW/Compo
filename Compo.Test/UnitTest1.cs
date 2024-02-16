using Microsoft.Extensions.DependencyInjection;
using Compo.Functions.Math;

namespace Compo.Test;

public class UnitTest1
{
    [Theory]
    [InlineData("@add(1,2)", 3)]
    [InlineData("@abs(1.2)", 1)]
    [InlineData("@mult(2,2)", 4d)]
    [InlineData("@mult(2.1, 2.3)", 4.83)]
    public void Tester(string expression, object result)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunctions(typeof(MultiplicationFunction), "mult");

        services.DiscoverFunctions();

        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();

        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();

        var engine = new ExpressionParser();
        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression));

        actual.Should().BeEquivalentTo(result);
    }
}
