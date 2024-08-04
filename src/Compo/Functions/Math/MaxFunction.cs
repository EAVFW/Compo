namespace Compo.Functions.Math;

/// <summary>
/// Return the highest value form a list or array of numbers.
/// <example>
/// <code>
/// max(1, 2, 3)
/// max([1, 2, 3])
/// </code>
/// Both will return <c>3</c>
/// </example>
/// </summary>
[FunctionRegistration("max")]
public class MaxFunction :
    IFunctionParams<int, int>,
    IFunctionParams<decimal, decimal>
{
    /// <summary>
    /// Return the highest value from the list of numbers.
    /// </summary>
    /// <param name="ts"></param>
    /// <returns></returns>
    public int Execute(params int[] ts)
    {
        return ts.Max();
    }

    /// <duplicate/>
    public decimal Execute(params decimal[] ts)
    {
        return ts.Max();
    }
}
