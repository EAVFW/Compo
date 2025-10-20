namespace Compo.Functions.String;

/// <summary>
/// Checks if a string starts with a specified substring.
/// </summary>
[FunctionRegistration("startswith")]
public class StartsWithFunction : IFunction<string, string, bool>
{
    public bool Execute(string text, string searchValue)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        if (searchValue == null)
            return false;

        return text.StartsWith(searchValue, StringComparison.Ordinal);
    }
}
