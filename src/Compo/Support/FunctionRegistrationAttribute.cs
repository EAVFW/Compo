namespace Compo;

public class FunctionRegistrationAttribute(string functionName) : Attribute
{
    public string FunctionName { get; } = functionName;
}
