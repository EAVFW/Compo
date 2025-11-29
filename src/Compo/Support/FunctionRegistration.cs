using System.Reflection;

namespace Compo;

public record FunctionRegistration
{
    public required Type FunctionType { get; set; }

    public required string FunctionName { get; set; }

    /// <summary>
    /// Pre-parsed argument types for the Execute method of this function.
    /// Cached during registration for fast overload resolution.
    /// For IFunction&lt;T1, T2, ..., TN, TResult&gt;, this contains [T1, T2, ..., TN] (excluding TResult).
    /// </summary>
    public Type[]? ArgumentTypes { get; set; }

    /// <summary>
    /// Pre-parsed parameter information for the Execute method.
    /// Cached during registration for checking attributes like [NestedExpression].
    /// </summary>
    public ParameterInfo[]? Parameters { get; set; }

    /// <summary>
    /// Indicates if this is an open generic type that needs to be constructed at runtime.
    /// </summary>
    public bool IsOpenGeneric { get; set; }
}