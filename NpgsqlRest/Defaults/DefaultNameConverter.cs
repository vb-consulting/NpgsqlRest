namespace NpgsqlRest.Defaults;

public static class DefaultNameConverter
{
    private static readonly string[] separator = ["_"];

    public static string? ConvertToCamelCase(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }
        if (value.Length == 0)
        {
            return string.Empty;
        }
        return value
            .Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select((s, i) => 
                string.Concat(i == 0 ? char.ToLowerInvariant(s[0]) : char.ToUpperInvariant(s[0]), s[1..]))
            .Aggregate(string.Empty, string.Concat)
            .Trim('"');
    }
}
