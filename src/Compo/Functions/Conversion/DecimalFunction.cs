namespace Compo.Functions.Conversion;

/// <summary>
/// Converts a value to a decimal.
/// Supports conversion from string, double, int, long, boolean.
/// </summary>
[FunctionRegistration("decimal")]
public class DecimalFunction :
    IFunction<string, decimal>,
    IFunction<double, decimal>,
    IFunction<int, decimal>,
    IFunction<long, decimal>,
    IFunction<bool, decimal>,
    IFunction<decimal, decimal>
{
    public decimal Execute(string value)
    {
        if (decimal.TryParse(value, out var result))
            return result;

        throw new InvalidOperationException($"Cannot convert '{value}' to decimal");
    }

    public decimal Execute(double value)
    {
        return (decimal)value;
    }

    public decimal Execute(int value)
    {
        return value;
    }

    public decimal Execute(long value)
    {
        return value;
    }

    public decimal Execute(bool value)
    {
        return value ? 1m : 0m;
    }

    public decimal Execute(decimal value)
    {
        return value;
    }
}
