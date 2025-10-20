namespace Compo.Functions.Logical;

/// <summary>
/// Logical OR operation.
/// Returns true if any argument is true.
/// </summary>
[FunctionRegistration("or")]
public class OrFunction :
    IFunction<bool, bool, bool>,
    IFunction<bool, bool, bool, bool>
{
    public bool Execute(bool a, bool b)
    {
        return a || b;
    }

    public bool Execute(bool a, bool b, bool c)
    {
        return a || b || c;
    }
}
