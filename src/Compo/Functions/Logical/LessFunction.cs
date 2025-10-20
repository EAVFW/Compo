namespace Compo.Functions.Logical;

/// <summary>
/// Less than comparison.
/// Returns true if first value is less than second.
/// </summary>
[FunctionRegistration("less")]
public class LessFunction :
    IFunction<int, int, bool>,
    IFunction<double, double, bool>,
    IFunction<decimal, decimal, bool>,
    IFunction<long, long, bool>
{
    public bool Execute(int a, int b)
    {
        return a < b;
    }

    public bool Execute(double a, double b)
    {
        return a < b;
    }

    public bool Execute(decimal a, decimal b)
    {
        return a < b;
    }

    public bool Execute(long a, long b)
    {
        return a < b;
    }
}
