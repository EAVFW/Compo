namespace Compo.Functions.Math;

[FunctionRegistration("add")]
public class AddFunction :
    IFunction<double, double, double>,
    IFunction<int, int, int>
{
    public double Execute(double t1, double t2)
    {
        return t1 + t2;
    }

    public int Execute(int t1, int t2)
    {
        return t1 + t2;
    }
}
