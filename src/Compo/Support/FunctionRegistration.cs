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
}
