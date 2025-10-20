namespace Compo.Functions.String;

/// <summary>
/// Returns the zero-based index of the first occurrence of a substring.
/// Returns -1 if not found.
/// </summary>
[FunctionRegistration("indexof")]
public class IndexOfFunction : IFunction<string, string, int>
{
    public int Execute(string text, string searchValue)
    {
        if (string.IsNullOrEmpty(text))
            return -1;

        if (searchValue == null)
            return -1;

        return text.IndexOf(searchValue, StringComparison.Ordinal);
    }
}
