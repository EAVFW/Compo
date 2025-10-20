namespace Compo.Functions.String;

/// <summary>
/// Extracts a substring from a string.
/// substring(string, startIndex) - from start index to end
/// substring(string, startIndex, length) - specific length from start index
/// </summary>
[FunctionRegistration("substring")]
public class SubstringFunction :
    IFunction<string, int, string>,
    IFunction<string, int, int, string>
{
    public string Execute(string value, int startIndex)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (startIndex < 0 || startIndex >= value.Length)
            throw new InvalidOperationException($"Start index {startIndex} is out of range for string of length {value.Length}");

        return value.Substring(startIndex);
    }

    public string Execute(string value, int startIndex, int length)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (startIndex < 0 || startIndex >= value.Length)
            throw new InvalidOperationException($"Start index {startIndex} is out of range for string of length {value.Length}");

        if (length < 0 || startIndex + length > value.Length)
            throw new InvalidOperationException($"Length {length} is invalid for string of length {value.Length} starting at index {startIndex}");

        return value.Substring(startIndex, length);
    }
}
