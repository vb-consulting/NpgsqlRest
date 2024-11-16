namespace NpgsqlRest.Defaults;

public static class DefaultUrlBuilder
{
    public static string CreateUrl(Routine routine, NpgsqlRestOptions options)
    {
        var schema = routine.Schema.ToLowerInvariant()
            .Replace("_", "-")
            .Replace(" ", "-")
            .Replace("\"", "")
            .Trim('/');

        schema = string.IsNullOrEmpty(schema) || string.Equals(schema, "public", StringComparison.OrdinalIgnoreCase) ? "" : string.Concat(schema, "/");
        var name = routine.Name.ToLowerInvariant()
            .Replace("_", "-")
            .Replace(" ", "-")
            .Replace("\"", "")
            .Trim('/');

        var prefix = options.UrlPathPrefix is null ? "/" : 
            string.Concat("/", options.UrlPathPrefix
                .ToLowerInvariant()
                .Replace("_", "-")
                .Replace(" ", "-")
                .Replace("\"", "")
                .Trim('/'),
            "/");

        return string.Concat(prefix, schema, name).TrimEnd('/').Trim(Consts.DoubleQuote);
    }
}
