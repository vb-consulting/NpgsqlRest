﻿namespace NpgsqlRest;

internal static class DefaultUrlBuilder
{
    internal static string CreateUrl(Routine routine, NpgsqlRestOptions options)
    {
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
        //return string.Concat(string.Concat(prefix, schema, name).TrimEnd('/'), '/');
        return string.Concat(prefix, schema, name).TrimEnd('/');
    }
}
