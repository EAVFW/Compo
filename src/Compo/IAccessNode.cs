namespace Compo;

/// <summary>
/// Defines an interface for objects that support bracket notation access in Compo expressions.
/// This provides an extension point for custom objects to participate in Compo's access semantics
/// without implementing the full IDictionary interface.
/// </summary>
/// <remarks>
/// When an object implements this interface, it can be accessed using bracket notation in expressions:
/// <code>
/// @customObject['fieldName']
/// </code>
///
/// This is particularly useful for wrapper objects that want to delegate field access to an
/// underlying data structure without exposing the full dictionary interface.
/// </remarks>
public interface IAccessNode
{
    /// <summary>
    /// Attempts to retrieve a value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, null.
    /// </param>
    /// <returns>
    /// true if the object contains an element with the specified key; otherwise, false.
    /// </returns>
    bool TryGetValue(string key, out object? value);
}
