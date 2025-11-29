namespace Compo;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class FunctionRegistrationAttribute(string functionName) : Attribute
{
    public string FunctionName { get; } = functionName;
}