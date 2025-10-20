namespace Compo.Functions.Logical;

/// <summary>
/// Equality comparison.
/// Returns true if both values are equal.
/// </summary>
[FunctionRegistration("equals")]
public class EqualsFunction :
    IFunction<string, string, bool>,
    IFunction<int, int, bool>,
    IFunction<double, double, bool>,
    IFunction<decimal, decimal, bool>,
    IFunction<bool, bool, bool>
{
    public bool Execute(string a, string b)
    {
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    public bool Execute(int a, int b)
    {
        return a == b;
    }

    public bool Execute(double a, double b)
    {
        return System.Math.Abs(a - b) < double.Epsilon;
    }

    public bool Execute(decimal a, decimal b)
    {
        return a == b;
    }

    public bool Execute(bool a, bool b)
    {
        return a == b;
    }
}
