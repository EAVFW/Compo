namespace Compo.Functions.String;

/// <summary>
/// Removes whitespace and null characters (\0) from the beginning and end of a string.
/// </summary>
[FunctionRegistration("trim")]
public class TrimFunction : IFunction<string, string>
{
    public string Execute(string value)
    {
        if (value == null)
            return string.Empty;

        // Trim both whitespace and null characters in one pass
        return value.TrimStart(' ', '\t', '\r', '\n', '\0')
                    .TrimEnd(' ', '\t', '\r', '\n', '\0');
    }
}
