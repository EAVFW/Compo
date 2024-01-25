namespace Compo;

public record FunctionRegistration
{
    public required Type FunctionType { get; set; }

    public required string FunctionName { get; set; }
}
