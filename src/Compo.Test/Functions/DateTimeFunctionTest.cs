using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test.Functions;

public class DateTimeFunctionTest
{
    [Fact]
    public void UtcNow_ReturnsCurrentUtcDateTime()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@utcNow()";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().NotBeNull();
        result.Should().BeOfType<DateTime>();
        var dateTime = (DateTime) result!;
        dateTime.Kind.Should().Be(DateTimeKind.Utc);

        // Should be close to current time (within 1 second)
        var now = DateTime.UtcNow;
        (now - dateTime).TotalSeconds.Should().BeLessThan(1);
    }

    [Fact]
    public void Ge_WithDateTime_ComparesCorrectly_FutureDate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Future date >= now should be true
        var futureDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var expression = $"@ge(datetime('{futureDate}'), utcNow())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(true);
    }

    [Fact]
    public void Ge_WithDateTime_ComparesCorrectly_PastDate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Past date >= now should be false
        var pastDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var expression = $"@ge(datetime('{pastDate}'), utcNow())";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(false);
    }

    [Fact]
    public void Ge_WithDateTime_ComparesCorrectly_SameDate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Same date >= same date should be true
        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var expression = $"@ge(datetime('{now}'), datetime('{now}'))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(true);
    }

    [Fact]
    public void RealWorldExample_FilterByValidTo()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Simulate payload with valid_to in the future
        var futureValidTo = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        // Create a mock payload function
        services.RegisterFunction<PayloadWithValidToFunction>("payload");
        var serviceProvider2 = services.BuildServiceProvider();
        var evaluator2 = serviceProvider2.GetRequiredService<IExpressionEvaluator>();

        // Expression from the real requirement
        var expression = "@ge(datetime(payload()['lh_valid_to']), utcNow())";
        var result = evaluator2.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(true, "valid_to is in the future");
    }

    [Fact]
    public void RealWorldExample_FilterByExpiredValidTo()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Simulate payload with expired valid_to
        var pastValidTo = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        // Create a mock payload function
        services.RegisterFunction<PayloadWithExpiredValidToFunction>("payload");
        var serviceProvider2 = services.BuildServiceProvider();
        var evaluator2 = serviceProvider2.GetRequiredService<IExpressionEvaluator>();

        // Expression from the real requirement
        var expression = "@ge(datetime(payload()['lh_valid_to']), utcNow())";
        var result = evaluator2.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(false, "valid_to is in the past");
    }

    [Fact]
    public void ParseDateTime_WithIso8601String_ParsesCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@datetime('2024-10-26T12:30:45.123Z')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().NotBeNull();
        result.Should().BeOfType<DateTime>();
        var dateTime = (DateTime) result!;
        dateTime.Year.Should().Be(2024);
        dateTime.Month.Should().Be(10);
        dateTime.Day.Should().Be(26);
        dateTime.Hour.Should().Be(12);
        dateTime.Minute.Should().Be(30);
        dateTime.Second.Should().Be(45);
    }

    // Helper function for testing
    public class PayloadWithValidToFunction : IFunction<IDictionary<string, object>>
    {
        public IDictionary<string, object> Execute()
        {
            var futureDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            return new Dictionary<string, object>
            {
                { "lh_valid_to", futureDate }
            };
        }
    }

    public class PayloadWithExpiredValidToFunction : IFunction<IDictionary<string, object>>
    {
        public IDictionary<string, object> Execute()
        {
            var pastDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            return new Dictionary<string, object>
            {
                { "lh_valid_to", pastDate }
            };
        }
    }
}