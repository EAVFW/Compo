namespace Compo.Functions.String;

/// <summary>
/// Checks if a string is null, empty, or contains only whitespace.
/// isEmpty(string) - returns true if null, empty, or whitespace
///
/// Examples:
/// - isEmpty('') returns true
/// - isEmpty('  ') returns true
/// - isEmpty(null) returns true
/// - isEmpty('hello') returns false
/// </summary>
[FunctionRegistration("isEmpty")]
public class IsEmptyFunction : IFunction<string?, bool>
{
    public bool Execute(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
}