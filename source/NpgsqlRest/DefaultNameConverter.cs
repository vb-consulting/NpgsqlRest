namespace NpgsqlRest;

internal static partial class Defaults
{
    private static readonly string[] separator = ["_"];

    internal static string? CamelCaseNameConverter(string? value)
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
            {
                if (i == 0)
                {
                    return string.Concat(char.ToLowerInvariant(s[0]), s[1..]);
                }
                return string.Concat(char.ToUpperInvariant(s[0]), s[1..]);
            })
            .Aggregate(string.Empty, string.Concat);
    }
}
