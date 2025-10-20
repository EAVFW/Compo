namespace Compo.Functions.String;

/// <summary>
/// Replaces all occurrences of a substring with another substring.
/// </summary>
[FunctionRegistration("replace")]
public class ReplaceFunction : IFunction<string, string, string, string>
{
    public string Execute(string text, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (string.IsNullOrEmpty(oldValue))
            throw new InvalidOperationException("Old value cannot be null or empty");

        return text.Replace(oldValue, newValue ?? string.Empty);
    }
}
