namespace Compo.Functions.String;

/// <summary>
/// Checks if a string ends with a specified substring.
/// </summary>
[FunctionRegistration("endswith")]
public class EndsWithFunction : IFunction<string, string, bool>
{
    public bool Execute(string text, string searchValue)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        if (searchValue == null)
            return false;

        return text.EndsWith(searchValue, StringComparison.Ordinal);
    }
}
