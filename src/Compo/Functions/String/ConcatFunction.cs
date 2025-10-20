namespace Compo.Functions.String;

/// <summary>
/// Concatenates two or more strings.
/// </summary>
[FunctionRegistration("concat")]
public class ConcatFunction :
    IFunction<string, string, string>,
    IFunction<string, string, string, string>
{
    public string Execute(string s1, string s2)
    {
        return string.Concat(s1 ?? "", s2 ?? "");
    }

    public string Execute(string s1, string s2, string s3)
    {
        return string.Concat(s1 ?? "", s2 ?? "", s3 ?? "");
    }
}
