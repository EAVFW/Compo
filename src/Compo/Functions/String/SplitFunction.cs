namespace Compo.Functions.String;

/// <summary>
/// Splits a string into an array of substrings based on a separator.
/// split(string, separator) - splits on separator with default options
/// split(string, separator, removeEmptyEntries) - optionally removes empty entries
///
/// Examples:
/// - split('one two three', ' ') returns ["one", "two", "three"]
/// - split('a,b,c', ',') returns ["a", "b", "c"]
/// - split('a,,b,,c', ',', true) returns ["a", "b", "c"] (empty entries removed)
/// </summary>
[FunctionRegistration("split")]
public class SplitFunction :
    IFunction<string, string, IEnumerable<string>>,
    IFunction<string, string, bool, IEnumerable<string>>
{
    public IEnumerable<string> Execute(string text, string separator)
    {
        return Execute(text, separator, false);
    }

    public IEnumerable<string> Execute(string text, string separator, bool removeEmptyEntries)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();

        if (separator == null)
            throw new ArgumentNullException(nameof(separator), "Separator cannot be null");

        var options = removeEmptyEntries
            ? StringSplitOptions.RemoveEmptyEntries
            : StringSplitOptions.None;

        return text.Split(new[] { separator }, options);
    }
}
