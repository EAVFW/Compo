namespace Compo.Functions.Logical;

/// <summary>
/// Conditional function - returns one value if condition is true, another if false.
/// if(condition, valueIfTrue, valueIfFalse)
/// </summary>
[FunctionRegistration("if")]
public class IfFunction :
    IFunction<bool, string, string, string>,
    IFunction<bool, int, int, int>,
    IFunction<bool, double, double, double>,
    IFunction<bool, decimal, decimal, decimal>,
    IFunction<bool, bool, bool, bool>
{
    public string Execute(bool condition, string valueIfTrue, string valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }

    public int Execute(bool condition, int valueIfTrue, int valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }

    public double Execute(bool condition, double valueIfTrue, double valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }

    public decimal Execute(bool condition, decimal valueIfTrue, decimal valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }

    public bool Execute(bool condition, bool valueIfTrue, bool valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }
}
