namespace Compo.Functions.Logical;

public record struct True { }
public record struct False { }

/// <summary>
/// Conditional function - returns one value if condition is true, another if false.
/// if(condition, valueIfTrue, valueIfFalse)
/// Uses lazy evaluation to avoid evaluating the branch that won't be returned.
/// </summary>
[FunctionRegistration("if")]
public class IfFunction<TLeft, TRight> :
    IFunction<True, Lazy<TLeft>, Lazy<TRight>, TLeft>,
    IFunction<False, Lazy<TLeft>, Lazy<TRight>, TRight>
{
    public TLeft Execute(True _, Lazy<TLeft> valueIfTrue, Lazy<TRight> valueIfFalse)
    {
        return valueIfTrue.Value;
    }

    public TRight Execute(False _, Lazy<TLeft> valueIfTrue, Lazy<TRight> valueIfFalse)
    {
        return valueIfFalse.Value;
    }
}
