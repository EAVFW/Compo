using Microsoft.Extensions.DependencyInjection;
using Compo.Functions.Math;

namespace Compo.Test;

public class MathTest
{
    [Theory]
    [InlineData("@add(1,2)", 3)]
    [InlineData("@abs(1.2)", 1)]

    [InlineData("@mult(2,2)", 4d)]
    [InlineData("@mult(2.1, 2.3)", 4.83)]

    [InlineData("@max(1,2)", 2)]
    [InlineData("@max(1,3,2)", 3)]
    [InlineData("@max(1.1,3.2,2.3)", 3.2)]
    [InlineData("@max(1)", 1)]
    [InlineData("@max(1.1,2,3.3)", 3.3)]
    [InlineData("@max(1.1,6,3.3)", 6)]

    [InlineData("@div(4,2)", 2)]
    [InlineData("@div(4.2, 2.1)", 2)]
    public void Tester(string expression, object result)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<MultiplicationFunction>("mult");

        services.DiscoverFunctions();

        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();

        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();

        var engine = new ExpressionParser();
        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().BeEquivalentTo(result);
    }
}
