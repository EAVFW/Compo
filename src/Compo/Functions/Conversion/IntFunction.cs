namespace Compo.Functions.Conversion;

/// <summary>
/// Converts a value to an integer.
/// Supports conversion from string, double, decimal, boolean.
/// </summary>
[FunctionRegistration("int")]
public class IntFunction :
    IFunction<string, int>,
    IFunction<double, int>,
    IFunction<decimal, int>,
    IFunction<bool, int>,
    IFunction<int, int>
{
    public int Execute(string value)
    {
        if (int.TryParse(value, out var result))
            return result;

        throw new InvalidOperationException($"Cannot convert '{value}' to int");
    }

    public int Execute(double value)
    {
        if (value > int.MaxValue || value < int.MinValue)
            throw new InvalidOperationException($"Value {value} is out of range for int");

        return (int)System.Math.Round(value);
    }

    public int Execute(decimal value)
    {
        if (value > int.MaxValue || value < int.MinValue)
            throw new InvalidOperationException($"Value {value} is out of range for int");

        return (int)System.Math.Round(value);
    }

    public int Execute(bool value)
    {
        return value ? 1 : 0;
    }

    public int Execute(int value)
    {
        return value;
    }
}
