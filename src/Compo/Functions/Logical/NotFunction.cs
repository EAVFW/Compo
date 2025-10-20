namespace Compo.Functions.Logical;

/// <summary>
/// Logical NOT operation.
/// Returns the inverse of the boolean value.
/// </summary>
[FunctionRegistration("not")]
public class NotFunction : IFunction<bool, bool>
{
    public bool Execute(bool value)
    {
        return !value;
    }
}
