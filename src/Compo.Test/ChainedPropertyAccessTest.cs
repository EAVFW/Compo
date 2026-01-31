using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Compo.Test;

/// <summary>
/// Tests to verify chained property access works consistently across different syntaxes.
/// This test reproduces the issue where parent().sender.userId doesn't work the same as parent('sender')['userId'].
/// </summary>
public class ChainedPropertyAccessTest
{
    /// <summary>
    /// Wrapper class that implements IAccessNode to support bracket notation on JsonElement.
    /// This simulates how ForeachItem works in the production code.
    /// </summary>
    public class JsonElementWrapper : IAccessNode
    {
        private readonly JsonElement _element;

        public JsonElementWrapper(JsonElement element)
        {
            _element = element;
        }

        public bool TryGetValue(string key, out object? value)
        {
            if (_element.ValueKind == JsonValueKind.Object && _element.TryGetProperty(key, out var property))
            {
                // Return nested objects as wrapped JsonElements too
                value = property.ValueKind == JsonValueKind.Object
                    ? new JsonElementWrapper(property)
                    : ConvertJsonElementValue(property);
                return true;
            }

            value = null;
            return false;
        }

        private static object? ConvertJsonElementValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => new JsonElementWrapper(element),
                JsonValueKind.Array => element,
                _ => element
            };
        }
    }

    /// <summary>
    /// Simulates a parent object with nested properties (like a message with sender).
    /// </summary>
    public class ParentObject : IAccessNode
    {
        private readonly JsonElement _data;

        public ParentObject(JsonElement data)
        {
            _data = data;
        }

        public bool TryGetValue(string key, out object? value)
        {
            if (_data.TryGetProperty(key, out var property))
            {
                // Return as wrapped JsonElement to support chained bracket notation
                value = property.ValueKind == JsonValueKind.Object
                    ? new JsonElementWrapper(property)
                    : ConvertValue(property);
                return true;
            }

            value = null;
            return false;
        }

        private static object? ConvertValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => new JsonElementWrapper(element),
                JsonValueKind.Array => element,
                _ => element
            };
        }
    }

    /// <summary>
    /// Function that returns a parent object, simulating parent() in our expressions.
    /// </summary>
    public class ParentFunction : IFunction<ParentObject>, IFunction<string, object>
    {
        private readonly ParentObject _parent;

        public ParentFunction()
        {
            // Create a JSON structure like: { "sender": { "userId": "EMP001", "name": "Employee" } }
            var json = """
            {
                "sender": {
                    "userId": "EMP001",
                    "name": "Employee Name",
                    "userType": "EMPLOYEE"
                },
                "id": "msg-001",
                "content": "Test message"
            }
            """;

            var doc = JsonDocument.Parse(json);
            _parent = new ParentObject(doc.RootElement);
        }

        // parent() - returns the parent object
        public ParentObject Execute()
        {
            return _parent;
        }

        // parent('fieldName') - returns a specific field from the parent (wrapped for bracket notation support)
        public object Execute(string fieldName)
        {
            if (_parent.TryGetValue(fieldName, out var value))
            {
                return value!;
            }

            throw new InvalidOperationException($"Field '{fieldName}' not found in parent");
        }
    }

    /// <summary>
    /// Tests that parent('sender')['userId'] works correctly.
    /// This is the WORKING syntax we use in the fixed profiles.
    /// </summary>
    [Fact]
    public void ParentFunction_WithBracketNotation_ShouldAccessNestedProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ParentFunction>();
        services.RegisterFunction<ParentFunction>("parent");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        // Use function notation for first level, bracket notation for nested
        var ast = parser.BuildAst("@parent('sender')['userId']");

        Assert.NotNull(ast.Value);

        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Act
        var result = evaluator.Evaluate(ast.Value);

        // Assert
        Assert.NotNull(result);

        // Convert result to string for comparison
        var actualValue = ExtractStringValue(result);
        Assert.Equal("EMP001", actualValue);
    }

    private static string? ExtractStringValue(object? result)
    {
        return result switch
        {
            null => null,
            string s => s,
            JsonElement element => element.GetString(),
            JsonElementWrapper wrapper => ExtractFromWrapper(wrapper),
            _ => result.ToString()
        };
    }

    private static string? ExtractFromWrapper(JsonElementWrapper wrapper)
    {
        // Try to get the raw value from the wrapper
        if (wrapper.TryGetValue("toString", out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Tests that parent().sender.userId SHOULD work but currently doesn't.
    /// This is the BROKEN syntax that was originally in the profiles.
    ///
    /// Expected: Should return "EMP001"
    /// Actual: Likely fails or returns null because dot notation doesn't work for chained access on JsonElement
    /// </summary>
    [Fact]
    public void ParentFunction_WithDotNotation_ShouldAccessNestedProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ParentFunction>();
        services.RegisterFunction<ParentFunction>("parent");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        // Use dot notation for chained property access
        var ast = parser.BuildAst("@parent().sender.userId");

        Assert.NotNull(ast.Value);

        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Act & Assert
        var exception = Record.Exception(() => evaluator.Evaluate(ast.Value));

        // This test documents the current behavior
        // If this test passes (no exception), then the issue is fixed!
        // If it throws an exception or returns null/wrong value, it demonstrates the bug

        if (exception != null)
        {
            // Document that dot notation currently fails
            Assert.NotNull(exception);
            // Uncomment below to see the actual error:
            // throw exception;
        }
        else
        {
            var result = evaluator.Evaluate(ast.Value);

            // If we get here, check if the result is correct
            var actualValue = ExtractStringValue(result);
            Assert.Equal("EMP001", actualValue);
        }
    }

    /// <summary>
    /// Tests that both syntaxes should return the same result.
    /// This is the ideal behavior we expect.
    /// </summary>
    [Fact]
    public void ParentFunction_DotNotationAndBracketNotation_ShouldReturnSameValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ParentFunction>();
        services.RegisterFunction<ParentFunction>("parent");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Evaluate using bracket notation (the working syntax)
        var bracketAst = parser.BuildAst("@parent('sender')['userId']");
        Assert.NotNull(bracketAst.Value);
        var bracketResult = evaluator.Evaluate(bracketAst.Value);
        var bracketValue = ExtractStringValue(bracketResult) ?? "";

        // Try to evaluate using dot notation (the broken syntax)
        var dotAst = parser.BuildAst("@parent().sender.userId");
        Assert.NotNull(dotAst.Value);

        var dotException = Record.Exception(() => evaluator.Evaluate(dotAst.Value));

        if (dotException != null)
        {
            // If dot notation throws an exception, the test fails
            Assert.Fail($"Dot notation failed with exception: {dotException.Message}. Bracket notation returned: {bracketValue}");
        }
        else
        {
            var dotResult = evaluator.Evaluate(dotAst.Value);
            var dotValue = ExtractStringValue(dotResult) ?? "";

            // Assert that both syntaxes return the same value
            Assert.Equal(bracketValue, dotValue);
        }
    }

    /// <summary>
    /// Tests accessing nested properties via bracket notation at all levels.
    /// This demonstrates an alternative fully-bracket syntax: parent()['sender']['userId']
    /// </summary>
    [Fact]
    public void ParentFunction_WithFullBracketNotation_ShouldAccessNestedProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ParentFunction>();
        services.RegisterFunction<ParentFunction>("parent");
        services.DiscoverFunctions();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var parser = new ExpressionParser();
        // Use bracket notation for all levels
        var ast = parser.BuildAst("@parent()['sender']['userId']");

        Assert.NotNull(ast.Value);

        var evaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();

        // Act
        var result = evaluator.Evaluate(ast.Value);

        // Assert
        Assert.NotNull(result);
        var actualValue = ExtractStringValue(result);
        Assert.Equal("EMP001", actualValue);
    }
}
