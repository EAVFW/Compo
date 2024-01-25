namespace Compo.Functions.Math;

[FunctionRegistration("abs")]
public class AbsFunction :
    IFunction<double, int>,
    IFunction<float, int>
{
    public int Execute(double t)
    {
        return (int)System.Math.Abs(t);
    }

    public int Execute(float t)
    {
        return (int)System.Math.Abs(t);
    }
}
