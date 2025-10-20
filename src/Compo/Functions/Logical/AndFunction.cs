namespace Compo.Functions.Logical;

/// <summary>
/// Logical AND operation.
/// Returns true if all arguments are true.
/// </summary>
[FunctionRegistration("and")]
public class AndFunction :
    IFunction<bool, bool, bool>,
    IFunction<bool, bool, bool, bool>
{
    public bool Execute(bool a, bool b)
    {
        return a && b;
    }

    public bool Execute(bool a, bool b, bool c)
    {
        return a && b && c;
    }
}
