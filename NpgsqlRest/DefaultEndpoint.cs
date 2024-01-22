using Microsoft.Extensions.Primitives;

namespace NpgsqlRest;

internal static class DefaultEndpoint
{
    internal static readonly char[] newlineSeparator = ['\r', '\n'];
    internal static readonly char[] wordSeparator = [' '];
    internal static readonly char[] headerSeparator = [':'];

    private const string httpKey = "http";
    private static readonly string[] paramTypeKey = [
        "requestparamtype", 
        "paramtype", 
        "request_param_type",
        "param_type",
        "request-param-type",
        "param-type"
    ];
    private static readonly string[] queryKey = [
        "querystring",
        "query_string",
        "query-string",
        "query"
    ];
    private static readonly string[] jsonKey = [
        "bodyjson",
        "body_json",
        "json",
        "body"
    ];
    private static readonly string[] authorizeKey = [
        "requiresauthorization",
        "authorize",
        "requires_authorization",
        "requires-authorization"
    ];
    private static readonly string[] timeoutKey = [
        "commandtimeout",
        "command_timeout",
        "timeout"
    ];
    private const string contentTypeKey = "content-type";
    private static readonly string[] requestHeadersModeKey = [
        "requestheadersmode",
        "request_headers_mode",
        "request-headers-mode",
        "requestheaders",
        "request_headers",
        "request-headers"
    ];
    private const string ignoreKey = "ignore";
    private const string contextKey = "context";
    private const string parameterKey = "parameter";
    private static readonly string[] requestHeadersParameterNameKey = [
        "requestheadersparametername",
        "requestheadersparamname",
        "request_headers_parameter_name",
        "request_headers_param_name",
        "request-headers-parameter-name",
        "request-headers-param-name",
    ];
    private static readonly string[] bodyParameterNameKey = [
        "bodyparametername",
        "body-parameter-name",
        "body_parameter_name",
        "bodyparamname",
        "body-param-name",
        "body_param_name"
    ];

    internal static RoutineEndpoint? Create(
        Routine routine, 
        NpgsqlRestOptions options, 
        ILogger? logger)
    {
        var url = options.UrlPathBuilder(routine, options);
        var hasGet = 
            routine.Name.Contains("_get_", StringComparison.OrdinalIgnoreCase) ||
            routine.Name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) ||
            routine.Name.EndsWith("_get", StringComparison.OrdinalIgnoreCase);
        var method = hasGet ? Method.GET : (routine.VolatilityOption == VolatilityOption.Volatile ? Method.POST : Method.GET);
        var requestParamType = method == Method.GET ? RequestParamType.QueryString : RequestParamType.BodyJson;
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

        if (options.CommentsMode != CommentsMode.Ignore)
        {
            var comment = routine.Comment;
            if (string.IsNullOrEmpty(comment))
            {
                if (options.CommentsMode == CommentsMode.OnlyWithHttpTag)
                {
                    return null;
                }
            }
            else
            {
                string[] lines = comment.Split(newlineSeparator, StringSplitOptions.RemoveEmptyEntries);
                bool hasHttpTag = false;
                for (var i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    string[] words = line.Split(wordSeparator, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 0)
                    {
                        if (StringEquals(ref words[0], httpKey))
                        {
                            hasHttpTag = true;
                            if (words.Length == 2 || words.Length == 3)
                            {
                                if (Enum.TryParse<Method>(words[1], true, out var parsedMethod))
                                {
                                    method = parsedMethod;
                                    requestParamType = method == Method.GET ? RequestParamType.QueryString : RequestParamType.BodyJson;
                                }
                                else
                                {
                                    Logging.LogWarning(
                                        ref logger,
                                        ref options,
                                        $"Invalid HTTP method '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}'. Using default '{method}'");
                                }
                            }
                            if (words.Length == 3)
                            {
                                string urlPathSegment = words[2];
                                if (!Uri.TryCreate(urlPathSegment, UriKind.Relative, out Uri? uri))
                                {
                                    Logging.LogWarning(
                                        ref logger,
                                        ref options,
                                        $"Invalid URL path segment '{urlPathSegment}' in comment for routine '{routine.Schema}.{routine.Name}'. Using default '{url}'");
                                }
                                else
                                {
                                    url = Uri.EscapeDataString(uri.ToString());
                                    if (!url.StartsWith('/'))
                                    {
                                        url = string.Concat("/", url);
                                    }
                                }
                            }
                        }
                        
                        else if (hasHttpTag && words.Length >= 2 && StringEqualsToArray(ref words[0], paramTypeKey))
                        {
                            if (StringEqualsToArray(ref words[1], queryKey))
                            {
                                requestParamType = RequestParamType.QueryString;
                            }
                            else if (StringEqualsToArray(ref words[1], jsonKey))
                            {
                                requestParamType = RequestParamType.BodyJson;
                            }
                            else
                            {
                                Logging.LogWarning(
                                    ref logger,
                                    ref options,
                                    $"Invalid parameter type '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}' Allowed values are QueryString or Query or BodyJson or Json. Using default '{requestParamType}'");
                            }
                        }
                        
                        else if (hasHttpTag && StringEqualsToArray(ref words[0], authorizeKey))
                        {
                            requiresAuthorization = true;
                        }
                        
                        else if (hasHttpTag && words.Length >= 2 && StringEqualsToArray(ref words[0], timeoutKey))
                        {
                            if (int.TryParse(words[1], out var parsedTimeout))
                            {
                                commandTimeout = parsedTimeout;
                            }
                            else
                            {
                                Logging.LogWarning(ref logger,
                                    ref options,
                                    $"Invalid command timeout '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}'. Using default command timeout '{commandTimeout}'");
                            }
                        }

                        else if (hasHttpTag && StringEqualsToArray(ref words[0], requestHeadersModeKey)) 
                        {
                            if (StringEquals(ref words[1], ignoreKey))
                            {
                                requestHeadersMode = RequestHeadersMode.Ignore;
                            }
                            else if (StringEquals(ref words[1], contextKey))
                            {
                                requestHeadersMode = RequestHeadersMode.Context;
                            }
                            else if (StringEquals(ref words[1], parameterKey))
                            {
                                requestHeadersMode = RequestHeadersMode.Parameter;
                            }
                            else
                            {
                                Logging.LogWarning(
                                    ref logger,
                                    ref options,
                                    $"Invalid parameter type '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}' Allowed values are Ignore or Context or Parameter. Using default '{requestHeadersMode}'");
                            }
                        }

                        else if (hasHttpTag && StringEqualsToArray(ref words[0], requestHeadersParameterNameKey))
                        { 
                            if (words.Length == 2)
                            {
                                requestHeadersParameterName = words[1];
                            }
                        }

                        else if (hasHttpTag && StringEqualsToArray(ref words[0], bodyParameterNameKey))
                        {
                            if (words.Length == 2)
                            {
                                bodyParameterName = words[1];
                            }
                        }

                        else if (hasHttpTag && line.Contains(':'))
                        {
                            var parts = line.Split(headerSeparator, 2);
                            if (parts.Length == 2)
                            {
                                var headerName = parts[0].Trim();
                                var headerValue = parts[1].Trim();
                                if (StringEquals(ref headerName, contentTypeKey))
                                {
                                    responseContentType = headerValue;
                                }
                                else
                                {
                                    if (responseHeaders is null)
                                    {
                                        responseHeaders = new()
                                        {
                                            [headerName] = new StringValues(headerValue)
                                        };
                                    }
                                    else
                                    {
                                        if (responseHeaders.TryGetValue(headerName, out StringValues values))
                                        {
                                            responseHeaders[headerName] = StringValues.Concat(values, headerValue);
                                        }
                                        else
                                        {
                                            responseHeaders.Add(headerName, new StringValues(headerValue));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (options.CommentsMode == CommentsMode.OnlyWithHttpTag && !hasHttpTag)
                {
                    return null;
                }
            }
        }

        return new(
            Url: url,
            Method: method,
            RequestParamType: requestParamType,
            RequiresAuthorization: requiresAuthorization,
            ReturnRecordNames: returnRecordNames,
            ParamNames: paramNames,
            CommandTimeout: commandTimeout,
            ResponseContentType: responseContentType,
            ResponseHeaders: responseHeaders ?? [],
            RequestHeadersMode: requestHeadersMode,
            RequestHeadersParameterName: requestHeadersParameterName,
            BodyParameterName: bodyParameterName);
    }

    private static bool StringEquals(ref string str1, string str2) => 
            str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

    private static bool StringEqualsToArray(ref string str, string[] arr)
    {
        for (var i = 0; i < arr.Length; i++)
        {
            if (str.Equals(arr[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}