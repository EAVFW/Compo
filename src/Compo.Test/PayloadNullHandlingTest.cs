using Microsoft.Extensions.DependencyInjection;

namespace Compo.Test;

/// <summary>
/// Tests for handling null values in payload data
/// Replicates the error: "Key 'telephone1' not found" when accessing null values in dictionary
/// </summary>
public class PayloadNullHandlingTest
{
    /// <summary>
    /// Simulates PayloadProvider function that returns a dictionary with null values
    /// </summary>
    public class PayloadFunction : IFunction<IDictionary<string, object?>>
    {
        private readonly IDictionary<string, object?> _payload = new Dictionary<string, object?>
        {
            { "name", "Test Name" },
            { "telephone1", null }, // Null value that exists in dictionary
            { "emailaddress1", "" },  // Empty string
            // Note: "missing_field" is intentionally NOT in the dictionary
        };

        public IDictionary<string, object?> Execute()
        {
            return _payload;
        }
    }

    [Theory]
    [InlineData("@payload()['name']", "Test Name")] // Should work - value exists
    [InlineData("@payload()['emailaddress1']", "")] // Should work - empty string exists
    public void PayloadAccess_WithExistingValues_ShouldWork(string expression, object expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void PayloadAccess_WithNullValue_ReturnsNull()
    {
        // IMPORTANT: Compo handles null values in dictionaries correctly!
        // The error "Key 'telephone1' not found" in production is NOT caused by Compo,
        // but rather by our PayloadProvider which filters out null values
        // before returning the dictionary (see DataProcessor.cs line 268-269).
        //
        // In this test, the PayloadFunction returns a dictionary WITH null values,
        // and Compo correctly returns null without throwing.
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@payload()['telephone1']";
        var ast = engine.BuildAst(expression).Value!;

        // Compo returns null gracefully - no exception is thrown
        var actual = expressionEvaluator.Evaluate(ast);
        actual.Should().BeNull("because telephone1 exists in dictionary with null value");
    }

    [Fact]
    public void PayloadAccess_WithMissingKey_ShouldThrowKeyNotFoundException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@payload()['missing_field']";
        var ast = engine.BuildAst(expression).Value!;

        // This should throw because missing_field doesn't exist in the dictionary at all
        var act = () => expressionEvaluator.Evaluate(ast);
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("Key 'missing_field' not found");
    }

    // TODO: Tests for potential fixes:
    // Option 1: Optional chaining - payload()?['telephone1'] returns null gracefully
    // Option 2: coalesce function - @coalesce(payload()['telephone1'], '') returns empty string
    // Option 3: tryget function - @tryget(payload(), 'telephone1', null) returns null if missing
}

/// <summary>
/// Tests for type conversion errors (like DateTime conversion)
/// Demonstrates the error: "Cannot convert '2025-03-14-13.20.24.208783' to DateTime"
/// </summary>
public class TypeConversionErrorTest
{
    /// <summary>
    /// Function that returns a payload with invalid datetime format
    /// </summary>
    public class PayloadFunction : IFunction<IDictionary<string, object?>>
    {
        private readonly IDictionary<string, object?> _payload = new Dictionary<string, object?>
        {
            { "valid_datetime", "2025-03-14T13:20:24.208783Z" },  // Valid ISO 8601
            { "invalid_datetime", "2025-03-14-13.20.24.208783" }, // Invalid format with dots and dashes
            { "date_only", "2025-03-14" },  // Valid date only
        };

        public IDictionary<string, object?> Execute()
        {
            return _payload;
        }
    }

    [Theory]
    [InlineData("@datetime(payload()['valid_datetime'])")]
    [InlineData("@datetime(payload()['date_only'])")]
    public void DateTimeConversion_WithValidFormat_ShouldWork(string expression)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().NotBeNull();
        actual.Should().BeOfType<DateTime>();
    }

    [Fact]
    public void DateTimeConversion_WithInvalidFormat_ShouldThrow()
    {
        // Demonstrates error when datetime conversion fails:
        // failedFields: { consent_date: {
        //   message: 'Exception has been thrown by the target of an invocation.',
        //   innerMessage: 'Cannot convert '2025-03-14-13.20.24.208783' to DateTime'
        // }}
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var expression = "@datetime(payload()['invalid_datetime'])";
        var ast = engine.BuildAst(expression).Value!;

        var act = () => expressionEvaluator.Evaluate(ast);

        // The datetime function should throw when it cannot convert the string
        act.Should().Throw<Exception>()
            .Where(ex => ex.Message.Contains("2025-03-14-13.20.24.208783") ||
                        (ex.InnerException != null && ex.InnerException.Message.Contains("DateTime")));
    }
}

/// <summary>
/// Tests for dictionary lookup functions (like picklist mapping)
/// Demonstrates accessing a dictionary with indexer to map string keys to integer values
/// </summary>
public class DictionaryLookupTest
{
    /// <summary>
    /// Function that returns a payload with string values that need to be mapped
    /// </summary>
    public class PayloadFunction : IFunction<IDictionary<string, object?>>
    {
        private readonly IDictionary<string, object?> _payload = new Dictionary<string, object?>
        {
            { "email_type", "Work email" },
            { "contact_type", "Private email" },
            { "missing_type", "Unknown value" },
        };

        public IDictionary<string, object?> Execute()
        {
            return _payload;
        }
    }

    /// <summary>
    /// Function that returns a mapping dictionary (like picklist values)
    /// Maps string labels to integer option values
    /// </summary>
    public class MappingFunction : IFunction<IDictionary<string, object>>
    {
        private readonly IDictionary<string, object> _mapping = new Dictionary<string, object>
        {
            { "Work email", 121140000 },
            { "Private email", 121140001 },
        };

        public IDictionary<string, object> Execute()
        {
            return _mapping;
        }
    }

    [Theory]
    [InlineData("@mapping()['Work email']", 121140000)]
    [InlineData("@mapping()['Private email']", 121140001)]
    public void DictionaryAccess_WithValidKey_ShouldReturnMappedValue(string expression, int expected)
    {
        // Test basic dictionary access with literal keys
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MappingFunction>("mapping");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("@mapping()[payload()['email_type']]", 121140000)]  // "Work email" -> 121140000
    [InlineData("@mapping()[payload()['contact_type']]", 121140001)] // "Private email" -> 121140001
    public void DictionaryAccess_WithDynamicKey_ShouldReturnMappedValue(string expression, int expected)
    {
        // Test dictionary access with dynamic keys from payload
        // This simulates: @picklist(payload()['email_type'], {'Work email': 121140000, 'Private email': 121140001})
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.RegisterFunction<MappingFunction>("mapping");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void DictionaryAccess_WithMissingKey_ShouldThrow()
    {
        // When the payload value doesn't exist in the mapping, it should throw
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<PayloadFunction>("payload");
        services.RegisterFunction<MappingFunction>("mapping");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // "Unknown value" doesn't exist in the mapping dictionary
        var expression = "@mapping()[payload()['missing_type']]";
        var ast = engine.BuildAst(expression).Value!;

        var act = () => expressionEvaluator.Evaluate(ast);
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("Key 'Unknown value' not found");
    }
}
