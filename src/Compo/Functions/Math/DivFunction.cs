namespace Compo.Functions.Math;

[FunctionRegistration("div")]
public class DivFunction :
    IFunction<int, int, int>,
    IFunction<decimal, decimal, decimal>
{
    public int Execute(int t1, int t2)
    {
        return t1 / t2;
    }

    public decimal Execute(decimal t1, decimal t2)
    {
        return t1 / t2;
    }
}
