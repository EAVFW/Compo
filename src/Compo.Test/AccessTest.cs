using Microsoft.Extensions.DependencyInjection;
using Compo.Functions.Math;

namespace Compo.Test;

public class AccessTest
{
    public class OutputsFunction : IFunction<IDictionary<string, object>>
    {
        private readonly IDictionary<string, object> _outputs = new Dictionary<string, object>
        {
            { "value1", 3 },
        };

        public IDictionary<string, object> Execute()
        {
            return _outputs;
        }
    }

    [Theory]
    [InlineData("@outputs()['value1']", 3)]
    public void Tester(string expression, object result)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<MultiplicationFunction>("mult");
        services.RegisterFunction<OutputsFunction>("outputs");

        services.DiscoverFunctions();

        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();

        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();

        var engine = new ExpressionParser();
        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().BeEquivalentTo(result);
    }
}
