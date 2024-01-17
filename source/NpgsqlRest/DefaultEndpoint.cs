using Microsoft.Extensions.Primitives;

namespace NpgsqlRest;

internal static class DefaultEndpoint
{
    internal static readonly char[] newlineSeparator = ['\r', '\n'];
    internal static readonly char[] wordSeparator = [' '];
    internal static readonly char[] headerSeparator = [':'];

    private const string http = "http";
    private const string paramType1 = "requestparamtype";
    private const string paramType2 = "paramtype";
    private const string queryString = "querystring";
    private const string query = "query";
    private const string bodyJson = "bodyjson";
    private const string json = "json";
    private const string body = "body";
    private const string authorize1 = "requiresauthorization";
    private const string authorize2 = "authorize";
    private const string timeout1 = "commandtimeout";
    private const string timeout2 = "timeout";
    private const string contentType = "content-type";

    internal static RoutineEndpoint? Create(Routine routine, NpgsqlRestOptions options, ILogger? logger)
    {
        var url = options.UrlPathBuilder(routine, options);
        var hasGet = routine.Name.Contains("get", StringComparison.OrdinalIgnoreCase);
        var method = hasGet ? Method.GET : Method.POST;
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
                        if (StrEquals(ref words[0], http))
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
                        else if (hasHttpTag && words.Length >= 2 && (StrEquals(ref words[0], paramType1) || StrEquals(ref words[0], paramType2)))
                        {
                            if (StrEquals(ref words[1], queryString) || StrEquals(ref words[1], query))
                            {
                                requestParamType = RequestParamType.QueryString;
                            }
                            else if (StrEquals(ref words[1], bodyJson) || StrEquals(ref words[1], json))
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
                        else if (hasHttpTag && (StrEquals(ref words[0], authorize1) || StrEquals(ref words[0], authorize2)))
                        {
                            requiresAuthorization = true;
                        }
                        else if (hasHttpTag && words.Length >= 2 && (StrEquals(ref words[0], timeout1) || StrEquals(ref words[0], timeout2)))
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
                        else if (hasHttpTag && line.Contains(':'))
                        {
                            var parts = line.Split(headerSeparator, 2);
                            if (parts.Length == 2)
                            {
                                var headerName = parts[0].Trim();
                                var headerValue = parts[1].Trim();
                                if (StrEquals(ref headerName, contentType))
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
                url: url,
                method: method,
                requestParamType: requestParamType,
                requiresAuthorization: requiresAuthorization,
                returnRecordNames: returnRecordNames,
                paramNames: paramNames,
                commandTimeout: commandTimeout,
                responseContentType: responseContentType,
                responseHeaders: responseHeaders);
    }

    private static bool StrEquals(ref string str1, string str2) => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
}