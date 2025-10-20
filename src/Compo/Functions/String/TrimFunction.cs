namespace Compo.Functions.String;

/// <summary>
/// Removes whitespace from the beginning and end of a string.
/// </summary>
[FunctionRegistration("trim")]
public class TrimFunction : IFunction<string, string>
{
    public string Execute(string value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
