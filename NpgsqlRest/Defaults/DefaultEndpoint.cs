using Microsoft.Extensions.Primitives;

namespace NpgsqlRest.Defaults;

internal static class DefaultEndpoint
{
    internal static RoutineEndpoint? Create(
        Routine routine,
        NpgsqlRestOptions options,
        ILogger? logger)
    {
        var url = options.UrlPathBuilder(routine, options);
        if (routine.FormatUrlPattern is not null)
        {
            url = string.Format(routine.FormatUrlPattern, url);
        }

        var method = routine.CrudType switch
        {
            CrudType.Select => Method.GET,
            CrudType.Update => Method.POST,
            CrudType.Insert => Method.PUT,
            CrudType.Delete => Method.DELETE,
            _ => Method.POST
        };
        var requestParamType = method == Method.GET || method == Method.DELETE ?
            RequestParamType.QueryString :
            RequestParamType.BodyJson;

        string[] returnRecordNames = routine.ReturnRecordNames.Select(s => options.NameConverter(s) ?? "").ToArray();
        string[] paramNames = routine
            .ParamNames
            .Select((s, i) =>
            {
                var result = options.NameConverter(s) ?? "";
                if (string.IsNullOrEmpty(result))
                {
                    result = $"${i + 1}";
                }
                return result;
            })
            .ToArray();

        var requiresAuthorization = options.RequiresAuthorization;
        var commandTimeout = options.CommandTimeout;
        string? responseContentType = null;
        Dictionary<string, StringValues>? responseHeaders = null;
        RequestHeadersMode requestHeadersMode = options.RequestHeadersMode;
        string requestHeadersParameterName = options.RequestHeadersParameterName;
        string? bodyParameterName = null;

        RoutineEndpoint routineEndpoint = new(
                url: url,
                method: method,
                requestParamType: requestParamType,
                requiresAuthorization: requiresAuthorization,
                returnRecordNames: returnRecordNames,
                paramNames: paramNames,
                commandTimeout: commandTimeout,
                responseContentType: responseContentType,
                responseHeaders: responseHeaders ?? [],
                requestHeadersMode: requestHeadersMode,
                requestHeadersParameterName: requestHeadersParameterName,
                bodyParameterName: bodyParameterName);


        return DefaultCommentParser.Parse(
            ref routine, 
            ref options, 
            ref logger,
            ref routineEndpoint);
    }
}