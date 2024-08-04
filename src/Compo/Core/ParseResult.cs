using Pidgin;

namespace Compo;

public class ParseResult
{
    public ParseResult()
    {
    }

    public ParseResult(Result<char, Node> result)
    {
        Success = result.Success;
        ErrorMessage = result.Success ? string.Empty : result.Error?.RenderErrorMessage();
        Value = result.Value;
    }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Node? Value { get; set; }
}
