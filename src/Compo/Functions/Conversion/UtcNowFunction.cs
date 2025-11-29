namespace Compo.Functions.Conversion;

/// <summary>
/// Returns the current UTC date and time.
/// </summary>
[FunctionRegistration("utcNow")]
public class UtcNowFunction : IFunction<DateTime>
{
    public DateTime Execute()
    {
        return DateTime.UtcNow;
    }
}