using System.Text.RegularExpressions;

namespace NpgsqlRest;

public static partial class TimeSpanParser
{
    [GeneratedRegex(@"^(\d*\.?\d+)\s*([a-z]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex IntervalRegex();

    public static TimeSpan? ParsePostgresInterval(string interval)
    {
        if (string.IsNullOrWhiteSpace(interval))
        {
            return null;
        }

        interval = interval.Trim().ToLowerInvariant();

        // Match number (integer or decimal) followed by optional space and unit
        var match = IntervalRegex().Match(interval);
        if (!match.Success)
        {
            return null;
        }

        string numberPart = match.Groups[1].Value;
        string unitPart = match.Groups[2].Value;

        if (!double.TryParse(numberPart, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double value))
        {
            return null;
        }

        // Map PostgreSQL units to TimeSpan conversions
        return unitPart switch
        {
            "s" or "sec" or "second" or "seconds" => TimeSpan.FromSeconds(value),
            "m" or "min" or "minute" or "minutes" => TimeSpan.FromMinutes(value),
            "h" or "hour" or "hours" => TimeSpan.FromHours(value),
            "d" or "day" or "days" => TimeSpan.FromDays(value),
            _ => null
        };
    }
}