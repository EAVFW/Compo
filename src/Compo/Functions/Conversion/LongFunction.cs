namespace Compo.Functions.Conversion;

/// <summary>
/// Converts a value to a long (64-bit integer).
/// Supports conversion from string, double, decimal, int, boolean.
/// </summary>
[FunctionRegistration("long")]
public class LongFunction :
    IFunction<string, long>,
    IFunction<double, long>,
    IFunction<decimal, long>,
    IFunction<int, long>,
    IFunction<bool, long>,
    IFunction<long, long>
{
    public long Execute(string value)
    {
        if (long.TryParse(value, out var result))
            return result;

        throw new InvalidOperationException($"Cannot convert '{value}' to long");
    }

    public long Execute(double value)
    {
        if (value > long.MaxValue || value < long.MinValue)
            throw new InvalidOperationException($"Value {value} is out of range for long");

        return (long)System.Math.Round(value);
    }

    public long Execute(decimal value)
    {
        if (value > long.MaxValue || value < long.MinValue)
            throw new InvalidOperationException($"Value {value} is out of range for long");

        return (long)System.Math.Round(value);
    }

    public long Execute(int value)
    {
        return value;
    }

    public long Execute(bool value)
    {
        return value ? 1L : 0L;
    }

    public long Execute(long value)
    {
        return value;
    }
}
