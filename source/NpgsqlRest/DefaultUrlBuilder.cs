namespace NpgsqlRest;

internal static partial class Defaults
{
    internal static string DefaultUrlBuilder((Routine routine, NpgsqlRestOptions options) parameters)
    {
        var (routine, options) = parameters;
        var schema = routine.Schema.ToLowerInvariant()
            .Replace("_", "-")
            .Replace(" ", "-")
            .Replace("\"", "")
            .Trim('/');
        if (schema == "public")
        {
            schema = "";
        }
        else
        {
            schema = string.Concat(schema, "/");
        }
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
        return string.Concat(string.Concat(prefix, schema, name).TrimEnd('/'), '/');
    }
}
