namespace Compo.Functions.Conversion;

/// <summary>
/// Generates a new GUID or parses a string to GUID.
/// With no arguments, generates a new GUID as string.
/// With one string argument, parses it to a GUID.
/// </summary>
[FunctionRegistration("guid")]
public class GuidFunction :
    IFunction<string>,
    IFunction<string, Guid>
{
    public string Execute()
    {
        return Guid.NewGuid().ToString("D");
    }

    public Guid Execute(string value)
    {
        if (Guid.TryParse(value, out var result))
            return result;

        throw new InvalidOperationException($"Cannot convert '{value}' to Guid");
    }
}
