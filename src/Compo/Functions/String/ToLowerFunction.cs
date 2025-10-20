namespace Compo.Functions.String;

/// <summary>
/// Converts a string to lowercase.
/// </summary>
[FunctionRegistration("tolower")]
public class ToLowerFunction : IFunction<string, string>
{
    public string Execute(string value)
    {
        return value?.ToLowerInvariant() ?? string.Empty;
    }
}
