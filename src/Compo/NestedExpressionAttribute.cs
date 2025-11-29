using System;

namespace Compo;

/// <summary>
/// Marks a function parameter as containing a nested expression that should be parsed to AST.
/// When this attribute is present, the expression evaluator will parse the string argument
/// as a Compo expression and provide access to the parsed AST.
/// </summary>
/// <example>
/// <code>
/// public class FilterFunction : IFunction&lt;object, string, object&gt;
/// {
///     public object Execute(object array, [NestedExpression] string predicateExpression)
///     {
///         // predicateExpression will be parsed: "equals(item()['field'], 'value')"
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class NestedExpressionAttribute : Attribute
{
}
