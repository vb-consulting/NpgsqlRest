using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace NpgsqlRest.Defaults;

internal static class DefaultCommentParser
{
    private static readonly char[] newlineSeparator = ['\r', '\n'];
    private static readonly char[] wordSeparators = [Consts.Space, Consts.Comma];
    private static readonly char[] headerSeparator = [Consts.Colon];

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
    private static readonly string[] allowAnonymousKey = [
        "allowanonymous",
        "allow_anonymous",
        "allow-anonymous",
        "anonymous",
        "anon"
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

    private static readonly string[] LoginKey = [
        "login",
        "signin",
    ];

    private static readonly string[] LogoutKey = [
        "logout",
        "signout",
    ];

    private static readonly string[] bufferRowsKey = [
        "bufferrows",
        "buffer_rows",
        "buffer-rows",
        "buffer"
    ];

    private const string RawKey = "raw";
    private const string SeparatorKey = "separator";
    private const string NewLineKey = "newline";
    private static readonly string[] columnNamesKey = [
        "columnnames",
        "column_names",
        "column-names"
    ];

    private const string CacheKey = "cached";

    private static readonly string[] parseResponseKey = [
        "parse",
        "parseresponse",
        "parse_response",
        "parse-response"
    ];

    private static readonly string[] cacheExpiresInKey = [
        "cacheexpires",
        "cacheexpiresin",
        "cache-expires",
        "cache-expires-in",
        "cache_expires",
        "cache_expires_in",
    ];

    private static readonly string[] connectionNameKey = [
        "connection",
        "connectionname",
        "connection_name",
        "connection-name"
    ];

    public static RoutineEndpoint? Parse(Routine routine, RoutineEndpoint routineEndpoint, NpgsqlRestOptions options, ILogger? logger)
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

            var routineDescription = string.Concat(routine.Type, " ", routine.Schema, ".", routine.Name);
            var urlDescription = string.Concat(routineEndpoint.Method.ToString(), " ", routineEndpoint.Url);
            var description = string.Concat(routineDescription, " mapped to ", urlDescription);

            string[] lines = comment.Split(newlineSeparator, StringSplitOptions.RemoveEmptyEntries);
            routineEndpoint.CommentWordLines = new string[lines.Length][];
            bool hasHttpTag = false;
            for (var i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                string[] words = Split(line);
                routineEndpoint.CommentWordLines[i] = words;
                var len = words.Length;
                if (len == 0)
                {
                    continue;
                }

                // for tag1, tag2, tag3 [, ...]
                // tag tag1, tag2, tag3 [, ...]
                // tags tag1, tag2, tag3 [, ...]
                if (routine.Tags is not null && routine.Tags.Length > 0 && StrEqualsToArray(words[0], tagsKey))
                {
                    string[] arr = words[1..];
                    bool found = false;
                    for (var j = 0; j < routine.Tags.Length; j++)
                    {
                        var tag = routine.Tags[j];
                        if (StrEqualsToArray(tag, arr))
                        {
                            found = true;
                            break;
                        }
                    }
                    haveTag = found;
                }

                // disabled
                // disabled tag1, tag2, tag3 [, ...]
                else if (haveTag is true && StrEquals(words[0], DisabledKey))
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
                            if (StrEqualsToArray(tag, arr))
                            {
                                disabled = true;
                                break;
                            }
                        }
                    }
                }

                // enabled
                // enabled [ tag1, tag2, tag3 [, ...] ]
                else if (haveTag is true && StrEquals(words[0], EnabledKey))
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
                            if (StrEqualsToArray(tag, arr))
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
                else if (haveTag is true && StrEquals(words[0], HttpKey))
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
                            logger?.InvalidHttpMethodComment(words[1], description, routineEndpoint.Method);
                        }
                    }
                    if (len == 3)
                    {
                        string urlPathSegment = words[2];
                        if (!Uri.TryCreate(urlPathSegment, UriKind.Relative, out Uri? uri))
                        {
                            logger?.InvalidUrlPathSegmentComment(urlPathSegment, description, routineEndpoint.Url);
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
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetHttp(description, routineEndpoint.Method, routineEndpoint.Url);
                        }
                        urlDescription = string.Concat(routineEndpoint.Method.ToString(), " ", routineEndpoint.Url);
                        description = string.Concat(routineDescription, " mapped to ", urlDescription);
                    }
                }

                // requestparamtype [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // paramtype  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // request_param_type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // param_type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // request-param-type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                // param-type  [ [ querystring | query_string | query-string | query ] | [ bodyjson | body_json | body-json | json | body ] ]
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], paramTypeKey))
                {
                    if (StrEqualsToArray(words[1], queryKey))
                    {
                        routineEndpoint.RequestParamType = RequestParamType.QueryString;
                    }
                    else if (StrEqualsToArray(words[1], jsonKey))
                    {
                        routineEndpoint.RequestParamType = RequestParamType.BodyJson;
                    }
                    else
                    {
                        logger?.InvalidParameterTypeComment(words[1], description, routineEndpoint.RequestParamType);
                    }

                    if (originalParamType != routineEndpoint.RequestParamType)
                    {
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetParameterType(description, routineEndpoint.RequestParamType);
                        }
                    }
                }

                // requiresauthorization
                // authorize
                // requires_authorization
                // requires-authorization
                else if (haveTag is true && StrEqualsToArray(words[0], authorizeKey))
                {
                    routineEndpoint.RequiresAuthorization = true;
                    if (words.Length > 1)
                    {
                        routineEndpoint.AuthorizeRoles = new(words[1..]);
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetAuthRoles(description, routineEndpoint.AuthorizeRoles);
                        }
                    } else
                    {
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetAuth(description);
                        }
                    }
                }

                // allowanonymous
                // allow_anonymous
                // allow-anonymous
                // anonymous
                // anon
                else if (haveTag is true && StrEqualsToArray(words[0], allowAnonymousKey))
                {
                    routineEndpoint.RequiresAuthorization = false;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentSetAnon(description);
                    }
                }

                // commandtimeout seconds
                // command_timeout seconds
                // command-timeout seconds
                // timeout seconds
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], timeoutKey))
                {
                    if (int.TryParse(words[1], out var parsedTimeout))
                    {
                        if (routineEndpoint.CommandTimeout != parsedTimeout)
                        {
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentSetTimeout(description, words[1]);
                            }
                        }
                        routineEndpoint.CommandTimeout = parsedTimeout;
                    }
                    else
                    {
                        logger?.InvalidTimeoutComment(words[1], description, routineEndpoint.CommandTimeout);
                    }
                }

                // requestheadersmode [ ignore | context | parameter ]
                // request_headers_mode [ ignore | context | parameter ]
                // request-headers-mode [ ignore | context | parameter ]
                // requestheaders [ ignore | context | parameter ]
                // request_headers [ ignore | context | parameter ]
                // request-headers [ ignore | context | parameter ]
                else if (haveTag is true && StrEqualsToArray(words[0], requestHeadersModeKey))
                {
                    if (StrEquals(words[1], IgnoreKey))
                    {
                        routineEndpoint.RequestHeadersMode = RequestHeadersMode.Ignore;
                    }
                    else if (StrEquals(words[1], ContextKey))
                    {
                        routineEndpoint.RequestHeadersMode = RequestHeadersMode.Context;
                    }
                    else if (StrEquals(words[1], ParameterKey))
                    {
                        routineEndpoint.RequestHeadersMode = RequestHeadersMode.Parameter;
                    }
                    else
                    {
                        logger?.InvalidRequestHeadersModeComment(words[1], description, routineEndpoint.RequestHeadersMode);
                    }
                    if (routineEndpoint.RequestHeadersMode != options.RequestHeadersMode)
                    {
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetRequestHeadersMode(description, words[1]);
                        }
                    }
                }

                // requestheadersparametername name
                // requestheadersparamname name
                // request_headers_parameter_name name
                // request_headers_param_name name
                // request-headers-parameter-name name
                // request-headers-param-name name
                else if (haveTag is true && StrEqualsToArray(words[0], requestHeadersParameterNameKey))
                {
                    if (len == 2)
                    {
                        if (!string.Equals(routineEndpoint.RequestHeadersParameterName, words[1]))
                        {
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentSetRequestHeadersParamName(description, words[1]);
                            }
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
                else if (haveTag is true && StrEqualsToArray(words[0], bodyParameterNameKey))
                {
                    if (len == 2)
                    {
                        if (!string.Equals(routineEndpoint.BodyParameterName, words[1]))
                        {
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentSetBodyParamName(description, words[1]);
                            }
                        }
                        routineEndpoint.BodyParameterName = words[1];
                    }
                }

                // responsenullhandling [ emptystring | nullliteral | nocontent ]
                // response_null_handling [ emptystring | nullliteral | nocontent ]
                // response-null-handling [ emptystring | nullliteral | nocontent ]
                else if (haveTag is true && StrEqualsToArray(words[0], textResponseNullHandlingKey))
                {
                    if (StrEquals(words[1], EmptyStringKey))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.EmptyString;
                    }
                    else if (StrEquals(words[1], NullLiteral))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.NullLiteral;
                    }
                    else if (StrEquals(words[1], NoContentKey))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.NoContent;
                    }
                    else
                    {
                        logger?.InvalidResponseNullHandlingModeComment(words[1], description, routineEndpoint.TextResponseNullHandling);
                    }
                    if (routineEndpoint.TextResponseNullHandling != options.TextResponseNullHandling)
                    {
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetTextResponseNullHandling(description, words[1]);
                        }
                    }
                }

                // querystringnullhandling [ emptystring | nullliteral | ignore ]
                // query_string_null_handling [ emptystring | nullliteral | ignore ]
                // query-string-null-handling [ emptystring | nullliteral | ignore ]
                else if (haveTag is true && StrEqualsToArray(words[0], queryStringNullHandlingKey))
                {
                    if (StrEquals(words[1], EmptyStringKey))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.EmptyString;
                    }
                    else if (StrEquals(words[1], NullLiteral))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.NullLiteral;
                    }
                    else if (StrEquals(words[1], IgnoreKey))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.Ignore;
                    }
                    else
                    {
                        logger?.InvalidQueryStringNullHandlingComment(words[1], description, routineEndpoint.QueryStringNullHandling);
                    }
                    if (routineEndpoint.TextResponseNullHandling != options.TextResponseNullHandling)
                    {
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetQueryStringNullHandling(description, routineEndpoint.QueryStringNullHandling);
                        }
                    }
                }

                // login
                // signin
                else if (haveTag is true && StrEqualsToArray(words[0], LoginKey))
                {
                    routineEndpoint.Login = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentSetLogin(description);
                    }
                }

                // logout
                // signout
                else if (haveTag is true && StrEqualsToArray(words[0], LogoutKey))
                {
                    routineEndpoint.Logout = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentSetLogout(description);
                    }
                }

                // bufferrows number
                // buffer_rows number
                // buffer-rows number
                // buffer number
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], bufferRowsKey))
                {
                    if (ulong.TryParse(words[1], out var parsedBuffer))
                    {
                        if (routineEndpoint.BufferRows != parsedBuffer)
                        {
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentBufferRows(description, words[1]);
                            }
                        }
                        routineEndpoint.BufferRows = parsedBuffer;
                    }
                    else
                    {
                        logger?.InvalidBufferRows(words[1], description, options.BufferRows);
                    }
                }

                // raw
                else if (haveTag is true && StrEquals(words[0], RawKey))
                {
                    logger?.CommentSetRawMode(description);
                    routineEndpoint.Raw = true;
                }

                // separator [ value ]
                else if (haveTag is true && line.StartsWith(string.Concat(SeparatorKey, " ")))
                {
                    var sep = line[(words[0].Length + 1)..];
                    logger?.CommentSetRawValueSeparator(description, sep);
                    routineEndpoint.RawValueSeparator = Regex.Unescape(sep);
                }

                // newline [ value ]
                else if (haveTag is true && len >= 2 && line.StartsWith(string.Concat(NewLineKey, " ")))
                {
                    var nl = line[(words[0].Length + 1)..];
                    logger?.CommentSetRawNewLineSeparator(description, nl);
                    routineEndpoint.RawNewLineSeparator = Regex.Unescape(nl);
                }

                // columnnames
                // column_names
                // column-names
                else if (haveTag is true && StrEqualsToArray(words[0], columnNamesKey))
                {
                    routineEndpoint.RawColumnNames = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentRawSetColumnNames(description);
                    }
                }

                // parse
                // parseresponse
                // parse_response
                // parse-response
                else if (haveTag is true && StrEqualsToArray(words[0], parseResponseKey))
                {
                    if (!(routine.ReturnsSet == false && routine.ColumnCount == 1 && routine.ReturnsRecordType is false))
                    {
                        logger?.CommentInvalidParseResponse(description);
                    }
                    routineEndpoint.ParseResponse = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentParseResponse(description);
                    }
                }

                // cached
                // cached [ param1, param2, param3 [, ...] ]
                else if (haveTag is true && StrEquals(words[0], CacheKey))
                {
                    if (!(routine.ReturnsSet == false && routine.ColumnCount == 1 && routine.ReturnsRecordType is false))
                    {
                        logger?.CommentInvalidCache(description);
                    }
                    routineEndpoint.Cached = true;
                    if (len > 1)
                    {
                        var names = words[1..];
                        HashSet<string> result = new(names.Length);
                        for(int j = 0; j < names.Length; j++)
                        {
                            var name = names[j];
                            if (!routine.OriginalParamsHash.Contains(name) && !routine.ParamsHash.Contains(name))
                            {
                                logger?.CommentInvalidCacheParam(description, name);
                            } else
                            {
                                result.Add(name);
                            }
                        }
                        routineEndpoint.CachedParams = result;
                    }

                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentCached(description, routineEndpoint.CachedParams ?? []);
                    }
                }

                // cacheexpires
                // cacheexpiresin
                // cache-expires
                // cache-expires-in
                // cache_expires
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], cacheExpiresInKey))
                {
                    var value = TimeSpanParser.ParsePostgresInterval(string.Join(Consts.Space, words[1..]));
                    if (value is not null)
                    {
                        routineEndpoint.CacheExpiresIn = value.Value;
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentCacheExpiresIn(description, value.Value);
                        }
                    }
                    else
                    {
                        logger?.InvalidCacheExpiresIn(description, string.Join(Consts.Space, words[1..]));
                    }
                }

                // connection
                // connectionname
                // connection_name
                // connection-name
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], connectionNameKey))
                {
                    var name = string.Join(Consts.Space, words[1..]);
                    if (string.IsNullOrEmpty(name) is false)
                    {
                        if (options.ConnectionStrings is null || options.ConnectionStrings.ContainsKey(name) is false)
                        {
                            logger?.CommentInvalidConnectionName(description, name);
                        }
                        routineEndpoint.ConnectionName = name;
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentConnectionName(description, name);
                        }
                    }
                    else
                    {
                        logger?.CommentEmptyConnectionName(description);
                    }
                }

                // key: value
                // Content-Type: application/json
                else if (haveTag is true && line.Contains(Consts.Colon))
                {
                    var parts = line.Split(headerSeparator, 2);
                    if (parts.Length == 2)
                    {
                        var headerName = parts[0].Trim();
                        var headerValue = parts[1].Trim();
                        if (headerValue.Contains(Consts.OpenBrace) && headerValue.Contains(Consts.CloseBrace))
                        {
                            routineEndpoint.NeedsParsing = true;
                        }
                        if (StrEquals(headerName, ContentTypeKey))
                        {
                            if (!string.Equals(routineEndpoint.ResponseContentType, headerValue))
                            {
                                if (options.LogAnnotationSetInfo)
                                {
                                    logger?.CommentSetContentType(description, headerValue);
                                }
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
                                if (options.LogAnnotationSetInfo)
                                {
                                    logger?.CommentSetHeader(description, headerName, headerValue);
                                }
                            }
                        }
                    }
                }
            }
            if (disabled)
            {
                if (options.LogAnnotationSetInfo)
                {
                    logger?.CommentDisabled(description);
                }
                return null;
            }
            if (options.CommentsMode == CommentsMode.OnlyWithHttpTag && !hasHttpTag)
            {
                return null;
            }
        }

        return routineEndpoint;
    }

    private static bool StrEquals(string str1, string str2) => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

    private static bool StrEqualsToArray(string str, string[] arr)
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

    private static string[] Split(string str)
    {
        return str
            .Split(wordSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToArray();
    }
}