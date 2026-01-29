namespace Compo.Functions.Logical;

/// <summary>
/// Inequality comparison.
/// Returns true if values are not equal.
/// Convenience function equivalent to not(equals(a, b)).
/// </summary>
[FunctionRegistration("neq")]
public class NotEqualsFunction :
    IFunction<string, string, bool>,
    IFunction<int, int, bool>,
    IFunction<double, double, bool>,
    IFunction<decimal, decimal, bool>,
    IFunction<bool, bool, bool>
{
    public bool Execute(string a, string b)
    {
        return !string.Equals(a, b, StringComparison.Ordinal);
    }

    public bool Execute(int a, int b)
    {
        return a != b;
    }

    public bool Execute(double a, double b)
    {
        return System.Math.Abs(a - b) >= double.Epsilon;
    }

    public bool Execute(decimal a, decimal b)
    {
        return a != b;
    }

    public bool Execute(bool a, bool b)
    {
        return a != b;
    }
}
