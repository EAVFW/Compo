namespace Compo.Functions.Logical;

/// <summary>
/// Conditional function - returns one value if condition is true, another if false.
/// if(condition, valueIfTrue, valueIfFalse)
/// Uses lazy evaluation to avoid evaluating the branch that won't be returned.
/// </summary>
[FunctionRegistration("if")]
public class IfFunction :
    IFunction<bool, Lazy<string?>, Lazy<string?>, string?>,
    IFunction<bool, Lazy<int>, Lazy<int>, int>,
    IFunction<bool, Lazy<double>, Lazy<double>, double>,
    IFunction<bool, Lazy<decimal>, Lazy<decimal>, decimal>,
    IFunction<bool, Lazy<bool>, Lazy<bool>, bool>,
    IFunction<bool, Lazy<object?>, Lazy<object?>, object?>
{
    public string? Execute(bool condition, Lazy<string?> valueIfTrue, Lazy<string?> valueIfFalse)
    {
        return condition ? valueIfTrue.Value : valueIfFalse.Value;
    }

    public int Execute(bool condition, Lazy<int> valueIfTrue, Lazy<int> valueIfFalse)
    {
        return condition ? valueIfTrue.Value : valueIfFalse.Value;
    }

    public double Execute(bool condition, Lazy<double> valueIfTrue, Lazy<double> valueIfFalse)
    {
        return condition ? valueIfTrue.Value : valueIfFalse.Value;
    }

    public decimal Execute(bool condition, Lazy<decimal> valueIfTrue, Lazy<decimal> valueIfFalse)
    {
        return condition ? valueIfTrue.Value : valueIfFalse.Value;
    }

    public bool Execute(bool condition, Lazy<bool> valueIfTrue, Lazy<bool> valueIfFalse)
    {
        return condition ? valueIfTrue.Value : valueIfFalse.Value;
    }

    public object? Execute(bool condition, Lazy<object?> valueIfTrue, Lazy<object?> valueIfFalse)
    {
        return condition ? valueIfTrue.Value : valueIfFalse.Value;
    }
}
