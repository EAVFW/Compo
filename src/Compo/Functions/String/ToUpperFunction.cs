namespace Compo.Functions.String;

/// <summary>
/// Converts a string to uppercase.
/// </summary>
[FunctionRegistration("toupper")]
public class ToUpperFunction : IFunction<string, string>
{
    public string Execute(string value)
    {
        return value?.ToUpperInvariant() ?? string.Empty;
    }
}
