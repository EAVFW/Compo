namespace Compo.Functions.String;

/// <summary>
/// Returns the length of a string.
/// </summary>
[FunctionRegistration("length")]
public class LengthFunction : IFunction<string, int>
{
    public int Execute(string value)
    {
        return value?.Length ?? 0;
    }
}
