using System.Globalization;

namespace Compo.Functions.Conversion;

/// <summary>
/// Converts a value to a DateTime.
/// Supports conversion from string and long (ticks or Unix timestamp).
/// </summary>
[FunctionRegistration("datetime")]
public class DateTimeFunction :
    IFunction<string, DateTime>,
    IFunction<long, DateTime>,
    IFunction<DateTime, DateTime>
{
    public DateTime Execute(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;

        // Try ISO 8601 format
        if (DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var isoResult))
            return isoResult.ToUniversalTime();

        throw new InvalidOperationException($"Cannot convert '{value}' to DateTime");
    }

    public DateTime Execute(long value)
    {
        // Try as ticks first
        if (value > 0 && value < DateTime.MaxValue.Ticks)
        {
            try
            {
                return new DateTime(value, DateTimeKind.Utc);
            }
            catch
            {
                // Fall through to Unix timestamp
            }
        }

        // Try as Unix timestamp (seconds since 1970-01-01)
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
        }
        catch
        {
            throw new InvalidOperationException($"Cannot convert {value} to DateTime");
        }
    }

    public DateTime Execute(DateTime value)
    {
        return value;
    }
}
