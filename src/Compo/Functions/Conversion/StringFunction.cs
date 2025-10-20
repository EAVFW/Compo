namespace Compo.Functions.Conversion;

/// <summary>
/// Converts a value to a string.
/// Supports conversion from any IConvertible type.
/// </summary>
[FunctionRegistration("string")]
public class StringFunction :
    IFunction<int, string>,
    IFunction<long, string>,
    IFunction<double, string>,
    IFunction<decimal, string>,
    IFunction<bool, string>,
    IFunction<string, string>
{
    public string Execute(int value)
    {
        return value.ToString();
    }

    public string Execute(long value)
    {
        return value.ToString();
    }

    public string Execute(double value)
    {
        return value.ToString();
    }

    public string Execute(decimal value)
    {
        return value.ToString();
    }

    public string Execute(bool value)
    {
        return value.ToString().ToLowerInvariant();
    }

    public string Execute(string value)
    {
        return value;
    }
}
