namespace Compo.Functions.Conversion;

/// <summary>
/// Converts a value to a boolean.
/// Supports conversion from string, int, long, double, decimal.
/// </summary>
[FunctionRegistration("boolean")]
public class BooleanFunction :
    IFunction<string, bool>,
    IFunction<int, bool>,
    IFunction<long, bool>,
    IFunction<double, bool>,
    IFunction<decimal, bool>,
    IFunction<bool, bool>
{
    public bool Execute(string value)
    {
        if (bool.TryParse(value, out var result))
            return result;

        // Try numeric conversion
        if (int.TryParse(value, out var numValue))
            return numValue != 0;

        // Case-insensitive string comparisons
        var lower = value.ToLowerInvariant();
        if (lower == "true" || lower == "yes" || lower == "1")
            return true;
        if (lower == "false" || lower == "no" || lower == "0")
            return false;

        throw new InvalidOperationException($"Cannot convert '{value}' to boolean");
    }

    public bool Execute(int value)
    {
        return value != 0;
    }

    public bool Execute(long value)
    {
        return value != 0L;
    }

    public bool Execute(double value)
    {
        return value != 0.0;
    }

    public bool Execute(decimal value)
    {
        return value != 0m;
    }

    public bool Execute(bool value)
    {
        return value;
    }
}
