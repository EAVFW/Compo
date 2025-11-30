using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test.Functions;

/// <summary>
/// Tests for functions that implement multiple IFunction interfaces with different arities.
/// This simulates use cases like LookupFunction which supports variable number of lookup values.
/// </summary>
public class MultiInterfaceFunctionTest
{
    /// <summary>
    /// A mock function similar to LookupFunction that supports 1-5 parameters.
    /// This simulates creating a reference with different numbers of key parts.
    /// </summary>
    public class MockReferenceFunction
        : IFunction<string, string, object, MockReference>
        , IFunction<string, string, object, object, MockReference>
        , IFunction<string, string, object, object, object, MockReference>
        , IFunction<string, string, object, object, object, object, MockReference>
        , IFunction<string, string, object, object, object, object, object, MockReference>
    {
        private MockReference CreateReference(string table, string keyName, params object[] values)
        {
            return new MockReference
            {
                Table = table,
                KeyName = keyName,
                Values = values,
                Hash = string.Join("|", values)
            };
        }

        public MockReference Execute(string t1, string t2, object t3)
            => CreateReference(t1, t2, t3);

        public MockReference Execute(string t1, string t2, object t3, object t4)
            => CreateReference(t1, t2, t3, t4);

        public MockReference Execute(string t1, string t2, object t3, object t4, object t5)
            => CreateReference(t1, t2, t3, t4, t5);

        public MockReference Execute(string t1, string t2, object t3, object t4, object t5, object t6)
            => CreateReference(t1, t2, t3, t4, t5, t6);

        public MockReference Execute(string t1, string t2, object t3, object t4, object t5, object t6, object t7)
            => CreateReference(t1, t2, t3, t4, t5, t6, t7);
    }

    public class MockReference
    {
        public required string Table { get; init; }
        public required string KeyName { get; init; }
        public required object[] Values { get; init; }
        public required string Hash { get; init; }
    }

    /// <summary>
    /// Function to provide test data
    /// </summary>
    public class DataFunction : IFunction<IDictionary<string, object>>
    {
        private readonly IDictionary<string, object> _data = new Dictionary<string, object>
        {
            { "id", "123" },
            { "firstName", "John" },
            { "lastName", "Doe" },
            { "email", "john@example.com" }
        };

        public IDictionary<string, object> Execute() => _data;
    }

    [Theory]
    [InlineData("@ref('contact', 'ContactKey', '123')", "contact", "ContactKey", "123")]
    [InlineData("@ref('account', 'AccountKey', 'ACC001')", "account", "AccountKey", "ACC001")]
    public void MultiInterface_With3Args_ShouldWork(string expression, string expectedTable, string expectedKey, string expectedValue)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.Table.Should().Be(expectedTable);
        mockRef.KeyName.Should().Be(expectedKey);
        mockRef.Values.Should().HaveCount(1);
        mockRef.Values[0].Should().Be(expectedValue);
        mockRef.Hash.Should().Be(expectedValue);
    }

    [Fact]
    public void MultiInterface_With4Args_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@ref('contact', 'NameKey', 'John', 'Doe')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.Table.Should().Be("contact");
        mockRef.KeyName.Should().Be("NameKey");
        mockRef.Values.Should().HaveCount(2);
        mockRef.Values.Should().Equal("John", "Doe");
        mockRef.Hash.Should().Be("John|Doe");
    }

    [Fact]
    public void MultiInterface_With5Args_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@ref('entity', 'CompositeKey', 'A', 'B', 'C')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.Values.Should().HaveCount(3);
        mockRef.Values.Should().Equal("A", "B", "C");
        mockRef.Hash.Should().Be("A|B|C");
    }

    [Fact]
    public void MultiInterface_With6Args_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@ref('entity', 'Key4', 'W', 'X', 'Y', 'Z')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.Values.Should().HaveCount(4);
        mockRef.Values.Should().Equal("W", "X", "Y", "Z");
        mockRef.Hash.Should().Be("W|X|Y|Z");
    }

    [Fact]
    public void MultiInterface_With7Args_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@ref('entity', 'Key5', 'A', 'B', 'C', 'D', 'E')";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.Values.Should().HaveCount(5);
        mockRef.Values.Should().Equal("A", "B", "C", "D", "E");
        mockRef.Hash.Should().Be("A|B|C|D|E");
    }

    [Fact]
    public void MultiInterface_WithDataFunction_ShouldResolveCorrectOverload()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Simulate lookup with composite key from payload data
        var expression = "@ref('contact', 'FullNameKey', data()['firstName'], data()['lastName'])";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.Table.Should().Be("contact");
        mockRef.KeyName.Should().Be("FullNameKey");
        mockRef.Values.Should().HaveCount(2);
        mockRef.Values.Should().Equal("John", "Doe");
        mockRef.Hash.Should().Be("John|Doe");
    }

    [Fact]
    public void MultiInterface_NestedInConditional_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.RegisterFunction<DataFunction>("data");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Use different overloads in conditional branches
        var expression = "@if(equals(data()['id'], '123'), ref('contact', 'IdKey', data()['id']), ref('contact', 'NameKey', data()['firstName'], data()['lastName']))";
        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.KeyName.Should().Be("IdKey");
        mockRef.Values.Should().HaveCount(1);
        mockRef.Hash.Should().Be("123");
    }

    /// <summary>
    /// Verify that the registration correctly handles all 5 interfaces.
    /// This ensures ExtractParameters doesn't throw AmbiguousMatchException.
    /// </summary>
    [Fact]
    public void MultiInterface_Registration_ShouldCreateMultipleFunctionRegistrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.DiscoverFunctions();

        // Get all FunctionRegistration instances for "ref"
        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "ref")
            .ToList();

        // Should have 5 registrations - one for each IFunction interface
        registrations.Should().HaveCount(5);

        // Verify each has different argument counts
        var argCounts = registrations.Select(r => r.ArgumentTypes?.Length ?? 0).OrderBy(x => x).ToList();
        argCounts.Should().Equal(3, 4, 5, 6, 7);
    }

    /// <summary>
    /// Verify that different arities are resolved correctly at runtime.
    /// This tests that the expression evaluator picks the right overload based on argument count.
    /// </summary>
    [Theory]
    [InlineData("@ref('t', 'k', 'a')", 1)]
    [InlineData("@ref('t', 'k', 'a', 'b')", 2)]
    [InlineData("@ref('t', 'k', 'a', 'b', 'c')", 3)]
    [InlineData("@ref('t', 'k', 'a', 'b', 'c', 'd')", 4)]
    [InlineData("@ref('t', 'k', 'a', 'b', 'c', 'd', 'e')", 5)]
    public void MultiInterface_DifferentArities_ShouldResolveCorrectly(string expression, int expectedValueCount)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MockReferenceFunction>("ref");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst(expression).Value!);

        result.Should().BeOfType<MockReference>();
        var mockRef = (MockReference)result!;
        mockRef.Values.Should().HaveCount(expectedValueCount);
    }
}
