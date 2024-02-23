using Microsoft.Extensions.Primitives;

namespace NpgsqlRest.Defaults;

internal static class DefaultCommentParser
{
    private static readonly char[] newlineSeparator = ['\r', '\n'];
    private static readonly char[] wordSeparator = [' '];
    private static readonly char[] headerSeparator = [':'];

    private const string HttpKey = "http";
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
        "body-json",
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
        "command-timeout",
        "timeout"
    ];
    private const string ContentTypeKey = "content-type";
    private static readonly string[] requestHeadersModeKey = [
        "requestheadersmode",
        "request_headers_mode",
        "request-headers-mode",
        "requestheaders",
        "request_headers",
        "request-headers"
    ];
    private const string IgnoreKey = "ignore";
    private const string ContextKey = "context";
    private const string ParameterKey = "parameter";
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

    public static RoutineEndpoint? Parse(ref Routine routine, ref NpgsqlRestOptions options, ref ILogger? logger, ref RoutineEndpoint routineEndpoint)
    {
        if (options.CommentsMode == CommentsMode.Ignore)
        {
            return routineEndpoint;
        }

        var originalUrl = routineEndpoint.Url;
        var originalMethod = routineEndpoint.Method;
        var originalParamType = routineEndpoint.RequestParamType;

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
                    if (StringEquals(ref words[0], HttpKey))
                    {
                        hasHttpTag = true;
                        if (words.Length == 2 || words.Length == 3)
                        {
                            if (Enum.TryParse<Method>(words[1], true, out var parsedMethod))
                            {
                                routineEndpoint.Method = parsedMethod;
                                routineEndpoint.RequestParamType = routineEndpoint.Method == Method.GET ? RequestParamType.QueryString : RequestParamType.BodyJson;
                            }
                            else
                            {
                                logger?.LogWarning($"Invalid HTTP method '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}'. Using default '{routineEndpoint.Method}'");
                            }
                        }
                        if (words.Length == 3)
                        {
                            string urlPathSegment = words[2];
                            if (!Uri.TryCreate(urlPathSegment, UriKind.Relative, out Uri? uri))
                            {
                                logger?.LogWarning($"Invalid URL path segment '{urlPathSegment}' in comment for routine '{routine.Schema}.{routine.Name}'. Using default '{routineEndpoint.Url}'");
                            }
                            else
                            {
                                routineEndpoint.Url = Uri.EscapeDataString(uri.ToString());
                                if (!routineEndpoint.Url.StartsWith('/'))
                                {
                                    routineEndpoint.Url = string.Concat("/", routineEndpoint.Url);
                                }
                            }
                        }
                        if (routineEndpoint.Method != originalMethod || !string.Equals(routineEndpoint.Url, originalUrl))
                        {
                            Info(ref logger, ref options, ref routine, $"has set HTTP by comment annotations to \"{routineEndpoint.Method} {routineEndpoint.Url}\"");
                        }
                    }

                    else if (/*hasHttpTag && */words.Length >= 2 && StringEqualsToArray(ref words[0], paramTypeKey))
                    {
                        if (StringEqualsToArray(ref words[1], queryKey))
                        {
                            routineEndpoint.RequestParamType = RequestParamType.QueryString;
                        }
                        else if (StringEqualsToArray(ref words[1], jsonKey))
                        {
                            routineEndpoint.RequestParamType = RequestParamType.BodyJson;
                        }
                        else
                        {
                            logger?.LogWarning($"Invalid parameter type '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}' Allowed values are QueryString or Query or BodyJson or Json. Using default '{routineEndpoint.RequestParamType}'");
                        }

                        if (originalParamType != routineEndpoint.RequestParamType)
                        {
                            Info(ref logger, ref options, ref routine, $"has set REQUEST PARAMETER TYPE by comment annotations to \"{routineEndpoint.RequestParamType}\"");
                        }
                    }

                    else if (/*hasHttpTag && */StringEqualsToArray(ref words[0], authorizeKey))
                    {
                        routineEndpoint.RequiresAuthorization = true;
                        Info(ref logger, ref options, ref routine, $"has set REQUIRED AUTHORIZATION by comment annotations.");
                    }

                    else if (/*hasHttpTag && */words.Length >= 2 && StringEqualsToArray(ref words[0], timeoutKey))
                    {
                        if (int.TryParse(words[1], out var parsedTimeout))
                        {
                            if (routineEndpoint.CommandTimeout != parsedTimeout)
                            {
                                Info(ref logger, ref options, ref routine, $"has set COMMAND TIMEOUT by comment annotations to {parsedTimeout} seconds");
                            }
                            routineEndpoint.CommandTimeout = parsedTimeout;
                        }
                        else
                        {
                            logger?.LogWarning($"Invalid command timeout '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}'. Using default command timeout '{routineEndpoint.CommandTimeout}'");
                        }
                    }

                    else if (/*hasHttpTag && */StringEqualsToArray(ref words[0], requestHeadersModeKey))
                    {
                        if (StringEquals(ref words[1], IgnoreKey))
                        {
                            routineEndpoint.RequestHeadersMode = RequestHeadersMode.Ignore;
                        }
                        else if (StringEquals(ref words[1], ContextKey))
                        {
                            routineEndpoint.RequestHeadersMode = RequestHeadersMode.Context;
                        }
                        else if (StringEquals(ref words[1], ParameterKey))
                        {
                            routineEndpoint.RequestHeadersMode = RequestHeadersMode.Parameter;
                        }
                        else
                        {
                            logger?.LogWarning($"Invalid parameter type '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}' Allowed values are Ignore or Context or Parameter. Using default '{routineEndpoint.RequestHeadersMode}'");
                        }
                        if (routineEndpoint.RequestHeadersMode != options.RequestHeadersMode)
                        {
                            Info(ref logger, ref options, ref routine, $"has set REQUEST HEADERS MODE by comment annotations to \"{routineEndpoint.RequestHeadersMode}\"");
                        }
                    }

                    else if (/*hasHttpTag && */StringEqualsToArray(ref words[0], requestHeadersParameterNameKey))
                    {
                        if (words.Length == 2)
                        {
                            if (!string.Equals(routineEndpoint.RequestHeadersParameterName, words[1]))
                            {
                                Info(ref logger, ref options, ref routine, $"has set REQUEST HEADERS PARAMETER NAME by comment annotations to \"{words[1]}\"");
                            }
                            routineEndpoint.RequestHeadersParameterName = words[1];
                        }
                    }

                    else if (/*hasHttpTag && */StringEqualsToArray(ref words[0], bodyParameterNameKey))
                    {
                        if (words.Length == 2)
                        {
                            if (!string.Equals(routineEndpoint.BodyParameterName, words[1]))
                            {
                                Info(ref logger, ref options, ref routine, $"has set BODY PARAMETER NAME by comment annotations to \"{words[1]}\"");
                            }
                            routineEndpoint.BodyParameterName = words[1];
                        }
                    }

                    else if (/*hasHttpTag && */line.Contains(':'))
                    {
                        var parts = line.Split(headerSeparator, 2);
                        if (parts.Length == 2)
                        {
                            var headerName = parts[0].Trim();
                            var headerValue = parts[1].Trim();
                            if (StringEquals(ref headerName, ContentTypeKey))
                            {
                                if (!string.Equals(routineEndpoint.ResponseContentType, headerValue))
                                {
                                    Info(ref logger, ref options, ref routine, $"has set Content-Type HEADER by comment annotations to \"{headerValue}\"");
                                }
                                routineEndpoint.ResponseContentType = headerValue;
                            }
                            else
                            {
                                if (routineEndpoint.ResponseHeaders is null)
                                {
                                    routineEndpoint.ResponseHeaders = new()
                                    {
                                        [headerName] = new StringValues(headerValue)
                                    };
                                }
                                else
                                {
                                    if (routineEndpoint.ResponseHeaders.TryGetValue(headerName, out StringValues values))
                                    {
                                        routineEndpoint.ResponseHeaders[headerName] = StringValues.Concat(values, headerValue);
                                    }
                                    else
                                    {
                                        routineEndpoint.ResponseHeaders.Add(headerName, new StringValues(headerValue));
                                    }
                                }
                                if (!string.Equals(routineEndpoint.ResponseContentType, headerValue))
                                {
                                    Info(ref logger, ref options, ref routine, $"has set {headerName} HEADER by comment annotations to \"{headerValue}\"");
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

        return routineEndpoint;
    }

    private static void Info(ref ILogger? logger, ref NpgsqlRestOptions options, ref Routine routine, string message)
    {
        if (!options.LogAnnotationSetInfo)
        {
            return;
        }
        logger?.LogInformation(string.Concat($"{routine.Type} {routine.Schema}.{routine.Name} ", message));
    }

    private static bool StringEquals(ref string str1, string str2) => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

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