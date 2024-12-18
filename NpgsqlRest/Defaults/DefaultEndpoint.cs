﻿namespace NpgsqlRest.Defaults;

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

        string[] returnRecordNames = new string[routine.ColumnNames.Length];
        for (int i = 0; i < routine.ColumnNames.Length; i++)
        {
            returnRecordNames[i] = options.NameConverter(routine.ColumnNames[i]) ?? "";
        }

        string[] paramNames = new string[routine.ParamNames.Length];
        for (int i = 0; i < routine.ParamNames.Length; i++)
        {
            var result = options.NameConverter(routine.ParamNames[i]) ?? "";
            if (string.IsNullOrEmpty(result))
            {
                result = $"${i + 1}";
            }
            paramNames[i] = result;
        }

        RoutineEndpoint routineEndpoint = new(
                url: url,
                method: method,
                requestParamType: requestParamType,
                requiresAuthorization: options.RequiresAuthorization,
                columnNames: returnRecordNames,
                paramNames: paramNames,
                commandTimeout: options.CommandTimeout,
                responseContentType: null,
                responseHeaders: [],
                requestHeadersMode: options.RequestHeadersMode,
                requestHeadersParameterName: options.RequestHeadersParameterName,
                bodyParameterName: null,
                textResponseNullHandling: options.TextResponseNullHandling,
                queryStringNullHandling: options.QueryStringNullHandling);

        if (options.LogCommands && logger != null)
        {
            routineEndpoint.LogCallback = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(5, nameof(routineEndpoint.LogCallback)),
                "{parameters}{command}",
                NpgsqlRestLogger.LogDefineOptions);
        }
        else
        {
            routineEndpoint.LogCallback = null;
        }

        if (routine.EndpointHandler is not null)
        {
            var parsed = DefaultCommentParser.Parse(
                ref routine,
                ref options,
                ref logger,
                ref routineEndpoint);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return routine.EndpointHandler(parsed);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
        return DefaultCommentParser.Parse(
            ref routine, 
            ref options, 
            ref logger,
            ref routineEndpoint);
    }
}