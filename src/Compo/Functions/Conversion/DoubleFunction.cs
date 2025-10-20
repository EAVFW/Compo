namespace Compo.Functions.Conversion;

/// <summary>
/// Converts a value to a double.
/// Supports conversion from string, decimal, int, long, boolean.
/// </summary>
[FunctionRegistration("double")]
public class DoubleFunction :
    IFunction<string, double>,
    IFunction<decimal, double>,
    IFunction<int, double>,
    IFunction<long, double>,
    IFunction<bool, double>,
    IFunction<double, double>
{
    public double Execute(string value)
    {
        if (double.TryParse(value, out var result))
            return result;

        throw new InvalidOperationException($"Cannot convert '{value}' to double");
    }

    public double Execute(decimal value)
    {
        return (double)value;
    }

    public double Execute(int value)
    {
        return value;
    }

    public double Execute(long value)
    {
        return value;
    }

    public double Execute(bool value)
    {
        return value ? 1.0 : 0.0;
    }

    public double Execute(double value)
    {
        return value;
    }
}
