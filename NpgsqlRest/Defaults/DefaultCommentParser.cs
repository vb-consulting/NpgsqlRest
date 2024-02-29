﻿using Microsoft.Extensions.Primitives;

namespace NpgsqlRest.Defaults;

internal static class DefaultCommentParser
{
    private static readonly char[] newlineSeparator = ['\r', '\n'];
    private static readonly char[] wordSeparators = [' ', ','];
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
    private static readonly string[] tagsKey = ["for", "tags", "tag"];

    private const string DisabledKey = "disabled";
    private const string EnabledKey = "enabled";

    private static readonly string[] textResponseNullHandlingKey = [
        "responsenullhandling",
        "response_null_handling",
        "response-null-handling",
    ];
    private const string EmptyStringKey = "emptystring";
    private const string NullLiteral = "nullliteral";
    private const string NoContentKey = "nocontent";

    private static readonly string[] queryStringNullHandlingKey = [
        "querystringnullhandling",
        "query-string-null-handling",
        "query_string_null_handling",
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
        var disabled = false;
        bool haveTag = true;
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
                string[] words = Split(ref line);
                var len = words.Length;
                if (len == 0)
                {
                    continue;
                }

                // for tag1, tag2, tag3 [, ...]
                // tag tag1, tag2, tag3 [, ...]
                // tags tag1, tag2, tag3 [, ...]
                if (routine.Tags is not null && routine.Tags.Length > 0 && StrEqualsToArray(ref words[0], tagsKey))
                {
                    string[] arr = words[1..];
                    bool found = false;
                    for (var j = 0; j < routine.Tags.Length; j++)
                    {
                        var tag = routine.Tags[j];
                        if (StrEqualsToArray(ref tag, arr))
                        {
                            found = true;
                            break;
                        }
                    }
                    haveTag = found;
                }

                // disabled
                // disabled tag1, tag2, tag3 [, ...]
                else if (haveTag is true && StrEquals(ref words[0], DisabledKey))
                {
                    if (len == 1)
                    {
                        disabled = true;
                    }
                    else if (routine.Tags is not null && routine.Tags.Length > 0)
                    {
                        string[] arr = words[1..];
                        for (var j = 0; j < routine.Tags.Length; j++)
                        {
                            var tag = routine.Tags[j];
                            if (StrEqualsToArray(ref tag, arr))
                            {
                                disabled = true;
                                break;
                            }
                        }
                    }
                }

                // enabled
                // enabled [ tag1, tag2, tag3 [, ...] ]
                else if (haveTag is true && StrEquals(ref words[0], EnabledKey))
                {
                    if (len == 1)
                    {
                        disabled = false;
                    }
                    else if (routine.Tags is not null && routine.Tags.Length > 0)
                    {
                        string[] arr = words[1..];
                        for (var j = 0; j < routine.Tags.Length; j++)
                        {
                            var tag = routine.Tags[j];
                            if (StrEqualsToArray(ref tag, arr))
                            {
                                disabled = false;
                                break;
                            }
                        }
                    }
                }

                // HTTP 
                // HTTP [ GET | POST | PUT | DELETE ]
                // HTTP [ GET | POST | PUT | DELETE ] path
                else if (haveTag is true && StrEquals(ref words[0], HttpKey))
                {
                    hasHttpTag = true;
                    if (len == 2 || len == 3)
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
                    if (len == 3)
                    {
                        string urlPathSegment = words[2];
                        if (!Uri.TryCreate(urlPathSegment, UriKind.Relative, out Uri? uri))
                        {
                            logger?.LogWarning($"Invalid URL path segment '{urlPathSegment}' in comment for routine '{routine.Schema}.{routine.Name}'. Using default '{routineEndpoint.Url}'");
                        }
                        else
                        {
                            routineEndpoint.Url = uri.ToString();
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

                // requestparamtype [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // paramtype  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // request_param_type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // param_type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // request-param-type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // param-type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                else if (haveTag is true && len >= 2 && StrEqualsToArray(ref words[0], paramTypeKey))
                {
                    if (StrEqualsToArray(ref words[1], queryKey))
                    {
                        routineEndpoint.RequestParamType = RequestParamType.QueryString;
                    }
                    else if (StrEqualsToArray(ref words[1], jsonKey))
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

                // requiresauthorization
                // authorize
                // requires_authorization
                // requires-authorization
                else if (haveTag is true && StrEqualsToArray(ref words[0], authorizeKey))
                {
                    routineEndpoint.RequiresAuthorization = true;
                    Info(ref logger, ref options, ref routine, $"has set REQUIRED AUTHORIZATION by comment annotations.");
                }

                // commandtimeout seconds
                // command_timeout seconds
                // command-timeout seconds
                // timeout seconds
                else if (haveTag is true && len >= 2 && StrEqualsToArray(ref words[0], timeoutKey))
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

                // requestheadersmode [ ignore | context | parameter ]
                // request_headers_mode [ ignore | context | parameter ]
                // request-headers-mode [ ignore | context | parameter ]
                // requestheaders [ ignore | context | parameter ]
                // request_headers [ ignore | context | parameter ]
                // request-headers [ ignore | context | parameter ]
                else if (haveTag is true && StrEqualsToArray(ref words[0], requestHeadersModeKey))
                {
                    if (StrEquals(ref words[1], IgnoreKey))
                    {
                        routineEndpoint.RequestHeadersMode = RequestHeadersMode.Ignore;
                    }
                    else if (StrEquals(ref words[1], ContextKey))
                    {
                        routineEndpoint.RequestHeadersMode = RequestHeadersMode.Context;
                    }
                    else if (StrEquals(ref words[1], ParameterKey))
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

                // requestheadersparametername name
                // requestheadersparamname name
                // request_headers_parameter_name name
                // request_headers_param_name name
                // request-headers-parameter-name name
                // request-headers-param-name name
                else if (haveTag is true && StrEqualsToArray(ref words[0], requestHeadersParameterNameKey))
                {
                    if (len == 2)
                    {
                        if (!string.Equals(routineEndpoint.RequestHeadersParameterName, words[1]))
                        {
                            Info(ref logger, ref options, ref routine, $"has set REQUEST HEADERS PARAMETER NAME by comment annotations to \"{words[1]}\"");
                        }
                        routineEndpoint.RequestHeadersParameterName = words[1];
                    }
                }

                // bodyparametername name
                // body-parameter-name name
                // body_parameter_name name
                // bodyparamname name
                // body-param-name name
                // body_param_name name
                else if (haveTag is true && StrEqualsToArray(ref words[0], bodyParameterNameKey))
                {
                    if (len == 2)
                    {
                        if (!string.Equals(routineEndpoint.BodyParameterName, words[1]))
                        {
                            Info(ref logger, ref options, ref routine, $"has set BODY PARAMETER NAME by comment annotations to \"{words[1]}\"");
                        }
                        routineEndpoint.BodyParameterName = words[1];
                    }
                }

                // responsenullhandling [ emptystring | nullliteral | nocontent ]
                // response_null_handling [ emptystring | nullliteral | nocontent ]
                // response-null-handling [ emptystring | nullliteral | nocontent ]
                else if (haveTag is true && StrEqualsToArray(ref words[0], textResponseNullHandlingKey))
                {
                    if (StrEquals(ref words[1], EmptyStringKey))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.EmptyString;
                    }
                    else if (StrEquals(ref words[1], NullLiteral))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.NullLiteral;
                    }
                    else if (StrEquals(ref words[1], NoContentKey))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.NoContent;
                    }
                    else
                    {
                        logger?.LogWarning($"Invalid parameter type '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}' Allowed values are EmptyString or NullLiteral or NoContent. Using default '{routineEndpoint.TextResponseNullHandling}'");
                    }
                    if (routineEndpoint.TextResponseNullHandling != options.TextResponseNullHandling)
                    {
                        Info(ref logger, ref options, ref routine, $"has set TEXT RESPONSE HANDLING by comment annotations to \"{routineEndpoint.TextResponseNullHandling}\"");
                    }
                }

                // querystringnullhandling [ emptystring | nullliteral | ignore ]
                // query_string_null_handling [ emptystring | nullliteral | ignore ]
                // query-string-null-handling [ emptystring | nullliteral | ignore ]
                else if (haveTag is true && StrEqualsToArray(ref words[0], queryStringNullHandlingKey))
                {
                    if (StrEquals(ref words[1], EmptyStringKey))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.EmptyString;
                    }
                    else if (StrEquals(ref words[1], NullLiteral))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.NullLiteral;
                    }
                    else if (StrEquals(ref words[1], IgnoreKey))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.Ignore;
                    }
                    else
                    {
                        logger?.LogWarning($"Invalid parameter type '{words[1]}' in comment for routine '{routine.Schema}.{routine.Name}' Allowed values are EmptyString or NullLiteral or Ignore. Using default '{routineEndpoint.QueryStringNullHandling}'");
                    }
                    if (routineEndpoint.TextResponseNullHandling != options.TextResponseNullHandling)
                    {
                        Info(ref logger, ref options, ref routine, $"has set QUERY STRING NULL HANDLING by comment annotations to \"{routineEndpoint.QueryStringNullHandling}\"");
                    }
                }

                // key: value
                // Content-Type: application/json
                else if (haveTag is true && line.Contains(':'))
                {
                    var parts = line.Split(headerSeparator, 2);
                    if (parts.Length == 2)
                    {
                        var headerName = parts[0].Trim();
                        var headerValue = parts[1].Trim();
                        if (StrEquals(ref headerName, ContentTypeKey))
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
            if (disabled)
            {
                return null;
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

    private static bool StrEquals(ref string str1, string str2) => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

    private static bool StrEqualsToArray(ref string str, string[] arr)
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

    private static string[] Split(ref string str)
    {
        return str
            .Split(wordSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToArray();
    }
}