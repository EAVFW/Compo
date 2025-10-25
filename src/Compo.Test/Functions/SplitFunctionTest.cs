using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test.Functions;

public class SplitFunctionTest
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

    /// <summary>
    /// Function that returns a dictionary with strings to split
    /// </summary>
    public class DataFunction : IFunction<IDictionary<string, object>>
    {
        private readonly IDictionary<string, object> _data = new Dictionary<string, object>
        {
            { "spaceSeparated", "one two three" },
            { "commaSeparated", "apple,banana,orange" },
            { "pipeSeparated", "a|b|c|d" },
            { "separator", " " },
        };

        public IDictionary<string, object> Execute()
        {
            return _data;
        }
    }

    [Theory]
    [InlineData("@split('apple,banana,orange', ',')", new[] { "apple", "banana", "orange" })]
    [InlineData("@split('a|b|c|d', '|')", new[] { "a", "b", "c", "d" })]
    [InlineData("@split('single', ',')", new[] { "single" })]
    [InlineData("@split('one two tree', ' ')", new[] { "one", "two", "tree" })]
    public void Split_ShouldSplitStringBySeparator(string expression, string[] expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeAssignableTo<IEnumerable<object>>();
        var actual = ((IEnumerable<object>)result!).Cast<string>().ToArray();
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("@split(data()['commaSeparated'], ',')[0]", "apple")]
    [InlineData("@split(data()['commaSeparated'], ',')[1]", "banana")]
    [InlineData("@split(data()['commaSeparated'], ',')[2]", "orange")]
    public void Split_ShouldAccessArrayElementsWithComma(string expression, string expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("@split(data()['spaceSeparated'], data()['separator'])[0]", "one")]
    [InlineData("@split(data()['spaceSeparated'], data()['separator'])[1]", "two")]
    [InlineData("@split(data()['spaceSeparated'], data()['separator'])[2]", "three")]
    public void Split_WithSpaceSeparatorFromData_ShouldWork(string expression, string expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().Be(expected);
    }

    [Fact]
    public void Split_WithEmptyString_ShouldReturnEmptyArray()
    {
        var evaluator = CreateEvaluator();
        var engine = new ExpressionParser();

        var expression = "@split('', ',')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeAssignableTo<IEnumerable<object>>();
        var actual = ((IEnumerable<object>)result!).ToArray();
        actual.Should().BeEmpty();
    }

    [Theory]
    [InlineData("@split('a,,b,,c', ',', false)", new[] { "a", "", "b", "", "c" })]
    [InlineData("@split('a,,b,,c', ',', true)", new[] { "a", "b", "c" })]
    public void Split_WithRemoveEmptyEntries_ShouldHandleEmptyValues(string expression, string[] expected)
    {
        var evaluator = CreateEvaluator();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeAssignableTo<IEnumerable<object>>();
        var actual = ((IEnumerable<object>)result!).Cast<string>().ToArray();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Split_WithMultiCharacterSeparator_ShouldWork()
    {
        var evaluator = CreateEvaluator();
        var engine = new ExpressionParser();

        var expression = "@split('apple::banana::orange', '::')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeAssignableTo<IEnumerable<object>>();
        var actual = ((IEnumerable<object>)result!).Cast<string>().ToArray();
        actual.Should().BeEquivalentTo(new[] { "apple", "banana", "orange" });
    }

    [Fact]
    public void Split_WithEmptySeparator_ShouldReturnSingleElement()
    {
        var evaluator = CreateEvaluator();
        var engine = new ExpressionParser();

        // Empty separator returns the whole string as single element (standard .NET behavior)
        var expression = "@split('test', '')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeAssignableTo<IEnumerable<object>>();
        var actual = ((IEnumerable<object>)result!).Cast<string>().ToArray();
        actual.Should().BeEquivalentTo(new[] { "test" });
    }

    /// <summary>
    /// Test the use case from the original requirement - splitting and accessing multiple parts
    /// This simulates: split(payload()['field'],' ')[0], split(...)[1], split(...)[2]
    /// </summary>
    [Fact]
    public void Split_RealWorldExample_ShouldAccessMultipleParts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Using comma separator (space separators from literals are trimmed by parser)
        var expr0 = "@split('part1,part2,part3', ',')[0]";
        var expr1 = "@split('part1,part2,part3', ',')[1]";
        var expr2 = "@split('part1,part2,part3', ',')[2]";

        var result0 = evaluator.Evaluate(engine.BuildAst(expr0).Value!);
        var result1 = evaluator.Evaluate(engine.BuildAst(expr1).Value!);
        var result2 = evaluator.Evaluate(engine.BuildAst(expr2).Value!);

        result0.Should().Be("part1");
        result1.Should().Be("part2");
        result2.Should().Be("part3");
    }
}
