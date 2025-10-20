namespace Compo.Functions.Logical;

/// <summary>
/// Greater than or equal comparison.
/// Returns true if first value is greater than or equal to second.
/// </summary>
[FunctionRegistration("greaterorequals")]
public class GreaterOrEqualsFunction :
    IFunction<int, int, bool>,
    IFunction<double, double, bool>,
    IFunction<decimal, decimal, bool>,
    IFunction<long, long, bool>
{
    public bool Execute(int a, int b)
    {
        return a >= b;
    }

    public bool Execute(double a, double b)
    {
        return a >= b;
    }

    public bool Execute(decimal a, decimal b)
    {
        return a >= b;
    }

    public bool Execute(long a, long b)
    {
        return a >= b;
    }
}
