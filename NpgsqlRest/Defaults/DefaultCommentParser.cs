using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace NpgsqlRest.Defaults;

internal static class DefaultCommentParser
{
    private static readonly char[] newlineSeparator = ['\r', '\n'];
    private static readonly char[] wordSeparators = [Consts.Space, Consts.Comma];

    private const string HttpKey = "http";
    private const string PathKey = "path";

    private static readonly string[] paramTypeKey = [
        "request_param_type",
        "param_type",
    ];
    private static readonly string[] queryKey = [
        "query_string",
        "query"
    ];
    private static readonly string[] jsonKey = [
        "body_json",
        "body"
    ];
    private static readonly string[] authorizeKey = [
        "authorize",
        "authorized",
        "requires_authorization",
    ];
    private static readonly string[] allowAnonymousKey = [
        "allow_anonymous",
        "anonymous",
        "allow_anon",
        "anon"
    ];
    private static readonly string[] timeoutKey = [
        "command_timeout",
        "timeout"
    ];
    private static readonly string[] ContentTypeKey = [
        "content-type", // content-type is header key
        "content_type",
    ];

    private static readonly string[] requestHeadersModeKey = [
        "request_headers_mode",
        "request_headers",
    ];

    private const string RequestHeaderModeIgnoreKey = "ignore";
    private const string RequestHeaderModeContextKey = "context";
    private const string RequestHeaderModeParameterKey = "parameter";

    private static readonly string[] requestHeadersParameterNameKey = [
        "request_headers_parameter_name",
        "request_headers_param_name",
        "request-headers-param-name",
    ];
    private static readonly string[] bodyParameterNameKey = [
        "body_parameter_name",
        "body_param_name"
    ];
    private static readonly string[] tagsKey = ["for", "tags", "tag"];

    private const string DisabledKey = "disabled";
    private const string EnabledKey = "enabled";

    private static readonly string[] textResponseNullHandlingKey = [
        "response_null_handling",
        "response_null",
    ];

    private static readonly string[] emptyStringKey = [
        "empty",
        "empty_string"
    ];

    private static readonly string[] nullLiteral = [
        "null_literal",
        "null"
    ];

    private static readonly string[] noContentKey = [
        "204",
        "204_no_content",
        "no_content",
    ];

    private static readonly string[] queryStringNullHandlingKey = [
        "query_string_null_handling",
        "query_null_handling",
        "query_string_null",
        "query_null",
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
        "buffer_rows",
        "buffer"
    ];

    private static readonly string[] rawKey = [
        "raw",
        "raw_mode",
        "raw_results",
    ];

    private static readonly string[] separatorKey = [
        "separator",
        "raw_separator",
    ];

    private static readonly string[] newLineKey = [
        "new_line",
        "raw_new_line",
    ];

    private static readonly string[] columnNamesKey = [
        "columns",
        "names",
        "column_names",
    ];

    private const string CacheKey = "cached";

    private static readonly string[] cacheExpiresInKey = [
        "cache_expires",
        "cache_expires_in",
    ];

    private static readonly string[] connectionNameKey = [
        "connection",
        "connection_name",
    ];

    private static readonly string[] securitySensitiveKey = [
        "sensitive",
        "security",
        "security_sensitive",
    ];

    private static readonly string[] userContextKey = [
        "user_context",
        "user_settings",
        "user_config"
    ];

    private static readonly string[] userParemetersKey = [
        "user_parameters",
        "user_params",
    ];

    private const string UploadKey = "upload";

    private static readonly string[] parameterKey = [
        "parameter",
        "param",
    ];

    private static readonly string[] infoEventsStreamingPathKey = [
        "info_path",
        "info_events_path",
        "info_streaming_path"
    ];

    private static readonly string[] infoEventsStreamingScopeKey = [
        "info_scope",
        "info_events_scope",
        "info_streaming_scope",
    ];

    public static RoutineEndpoint? Parse(
        Routine routine,
        RoutineEndpoint routineEndpoint,
        NpgsqlRestOptions options,
        ILogger? logger)
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
                string[] words = line.SplitWords();
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

                // key = value
                // custom_parameter_1 = custom parameter 1 value
                // custom_parameter_2 = custom parameter 2 value
                else if (haveTag is true && SplitBySeparatorChar(line, Consts.Equal, out var customParamName, out var customParamValue))
                {
                    if (customParamValue.Contains(Consts.OpenBrace) && customParamValue.Contains(Consts.CloseBrace))
                    {
                        routineEndpoint.CustomParamsNeedParsing = true;
                    }
                    SetCustomParameter(routineEndpoint, customParamName, customParamValue, logger);
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentSetCustomParemeter(description, customParamName, customParamValue);
                    }
                }

                // key: value
                // Content-Type: application/json
                else if (haveTag is true && SplitBySeparatorChar(line, Consts.Colon, out var headerName, out var headerValue))
                {
                    if (headerValue.Contains(Consts.OpenBrace) && headerValue.Contains(Consts.CloseBrace))
                    {
                        routineEndpoint.HeadersNeedParsing = true;
                    }
                    if (StrEqualsToArray(headerName, ContentTypeKey))
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
                // HTTP path
                else if (haveTag is true && StrEquals(words[0], HttpKey))
                {
                    hasHttpTag = true;
                    string? urlPathSegment = null;
                    if (len == 2 || len == 3)
                    {
                        if (Enum.TryParse<Method>(words[1], true, out var parsedMethod))
                        {
                            routineEndpoint.Method = parsedMethod;
                            routineEndpoint.RequestParamType = routineEndpoint.Method == Method.GET ? RequestParamType.QueryString : RequestParamType.BodyJson;
                        }
                        else
                        {
                            urlPathSegment = words[1];
                            //logger?.InvalidHttpMethodComment(words[1], description, routineEndpoint.Method);
                        }
                    }
                    if (len == 3)
                    {
                        urlPathSegment = words[2];
                    }
                    if (urlPathSegment is not null)
                    {
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

                // PATH path
                else if (haveTag is true && StrEquals(words[0], PathKey))
                {
                    if (len == 2)
                    {
                        string? urlPathSegment = words[1];
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

                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentSetHttp(description, routineEndpoint.Method, routineEndpoint.Url);
                            }
                            urlDescription = string.Concat(routineEndpoint.Method.ToString(), " ", routineEndpoint.Url);
                            description = string.Concat(routineDescription, " mapped to ", urlDescription);
                        }
                    }
                }

                // request_param_type  [ [ query_string | query ] | [ body_json |  body ] ]
                // param_type  [ [ query_string | query ] | [ body_json | body ] ]
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

                // authorize
                // requires_authorization
                // authorize [ role1, role2, role3 [, ...] ]
                // requires_authorization [ role1, role2, role3 [, ...] ]
                else if (haveTag is true && StrEqualsToArray(words[0], authorizeKey))
                {
                    routineEndpoint.RequiresAuthorization = true;
                    if (words.Length > 1)
                    {
                        routineEndpoint.AuthorizeRoles = [.. words[1..]];
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetAuthRoles(description, routineEndpoint.AuthorizeRoles);
                        }
                    }
                    else
                    {
                        if (options.LogAnnotationSetInfo)
                        {
                            logger?.CommentSetAuth(description);
                        }
                    }
                }

                // allow_anonymous
                // anonymous
                // allow_anon
                // anon
                else if (haveTag is true && StrEqualsToArray(words[0], allowAnonymousKey))
                {
                    routineEndpoint.RequiresAuthorization = false;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentSetAnon(description);
                    }
                }

                // command_timeout seconds
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

                // request_headers_mode [ ignore | context | parameter ]
                // request_headers [ ignore | context | parameter ]
                else if (haveTag is true && StrEqualsToArray(words[0], requestHeadersModeKey))
                {
                    if (StrEquals(words[1], RequestHeaderModeIgnoreKey))
                    {
                        routineEndpoint.RequestHeadersMode = RequestHeadersMode.Ignore;
                    }
                    else if (StrEquals(words[1], RequestHeaderModeContextKey))
                    {
                        routineEndpoint.RequestHeadersMode = RequestHeadersMode.Context;
                    }
                    else if (StrEquals(words[1], RequestHeaderModeParameterKey))
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

                // request_headers_parameter_name name
                // request_headers_param_name name
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

                // body_parameter_name name
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

                // response_null_handling [ empty_string | empty | null_literal | null | no_content | 204 | 204_no_content ]
                // response_null [ empty_string | empty | null_literal | null |  no_content | 204 | 204_no_content ]
                else if (haveTag is true && StrEqualsToArray(words[0], textResponseNullHandlingKey))
                {
                    if (StrEqualsToArray(words[1], emptyStringKey))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.EmptyString;
                    }
                    else if (StrEqualsToArray(words[1], nullLiteral))
                    {
                        routineEndpoint.TextResponseNullHandling = TextResponseNullHandling.NullLiteral;
                    }
                    else if (StrEqualsToArray(words[1], noContentKey))
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

                // query_string_null_handling [ empty_string | empty | null_literal | null |  ignore ]
                // query_null_handling [ empty_string | empty |null_literal | null |  ignore ]
                // query_string_null [ empty_string | empty |null_literal | null |  ignore ]
                // query_null [ empty_string | empty | null_literal | null |  ignore ]
                else if (haveTag is true && StrEqualsToArray(words[0], queryStringNullHandlingKey))
                {
                    if (StrEqualsToArray(words[1], emptyStringKey))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.EmptyString;
                    }
                    else if (StrEqualsToArray(words[1], nullLiteral))
                    {
                        routineEndpoint.QueryStringNullHandling = QueryStringNullHandling.NullLiteral;
                    }
                    else if (StrEquals(words[1], RequestHeaderModeIgnoreKey))
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

                // buffer_rows number
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
                // raw_mode
                // raw_results
                else if (haveTag is true && StrEqualsToArray(words[0], rawKey))
                {
                    logger?.CommentSetRawMode(description);
                    routineEndpoint.Raw = true;
                }

                // separator [ value ]
                // raw_separator [ value ]
                else if (haveTag is true && line.StartsWith(string.Concat(separatorKey[0], " ")))
                //else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], separatorKey))
                {
                    var sep = line[(words[0].Length + 1)..];
                    logger?.CommentSetRawValueSeparator(description, sep);
                    routineEndpoint.RawValueSeparator = Regex.Unescape(sep);
                }

                // new_line [ value ]
                // raw_new_line [ value ]
                else if (haveTag is true && len >= 2 && line.StartsWith(string.Concat(newLineKey[0], " ")))
                //else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], newLineKey))
                {
                    var nl = line[(words[0].Length + 1)..];
                    logger?.CommentSetRawNewLineSeparator(description, nl);
                    routineEndpoint.RawNewLineSeparator = Regex.Unescape(nl);
                }

                // columns
                // names
                // column_names
                else if (haveTag is true && StrEqualsToArray(words[0], columnNamesKey))
                {
                    routineEndpoint.RawColumnNames = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentRawSetColumnNames(description);
                    }
                }

                // sensitive
                // security_sensitive
                else if (haveTag is true && StrEqualsToArray(words[0], securitySensitiveKey))
                {
                    routineEndpoint.SecuritySensitive = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentSecuritySensitive(description);
                    }
                }

                // user_context
                // user_settings
                // user_config
                else if (haveTag is true && StrEqualsToArray(words[0], userContextKey))
                {
                    routineEndpoint.UserContext = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentUserContext(description);
                    }
                }

                // user_parameters
                // user_params
                else if (haveTag is true && StrEqualsToArray(words[0], userParemetersKey))
                {
                    routineEndpoint.UseUserParameters = true;
                    if (options.LogAnnotationSetInfo)
                    {
                        logger?.CommentUserParameters(description);
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
                        for (int j = 0; j < names.Length; j++)
                        {
                            var name = names[j];
                            if (!routine.OriginalParamsHash.Contains(name) && !routine.ParamsHash.Contains(name))
                            {
                                logger?.CommentInvalidCacheParam(description, name);
                            }
                            else
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

                // cache_expires
                // cache_expires_in
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], cacheExpiresInKey))
                {
                    var value = Parser.ParsePostgresInterval(string.Join(Consts.Space, words[1..]));
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
                // connection_name
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

                // upload
                // upload for handler_name1, handler_name2 [, ...]
                // upload param_name as metadata
                else if (haveTag is true && StrEquals(words[0], UploadKey))
                {
                    if (options.UploadOptions.UploadHandlers is null || options.UploadOptions.UploadHandlers.Count == 0)
                    {
                        logger?.CommentUploadNoHandlers(description);
                    }
                    else
                    {
                        if (routineEndpoint.Upload is false)
                        {
                            routineEndpoint.Upload = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentUpload(description);
                            }
                        }
                        if (routineEndpoint.RequestParamType != RequestParamType.QueryString)
                        {
                            routineEndpoint.RequestParamType = RequestParamType.QueryString;
                        }
                        if (routineEndpoint.Method != Method.POST)
                        {
                            routineEndpoint.Method = Method.POST;
                        }
                        if (len >= 3 && StrEquals(words[1], "for"))
                        {
                            HashSet<string> existingHandlers = options.UploadOptions.UploadHandlers?.Keys.ToHashSet() ?? [];
                            var handlers = words[2..]
                                .Select(w =>
                                {
                                    var handler = w.TrimEnd(',');
                                    bool exists = true;
                                    if (existingHandlers.Contains(handler) is false)
                                    {
                                        logger?.CommentUploadHandlerNotExists(description, handler, existingHandlers);
                                        exists = false;
                                    }
                                    return new { exists, handler };
                                })
                                .Where(x => x.exists is true)
                                .Select(x => x.handler)
                                .ToArray();

                            routineEndpoint.UploadHandlers = handlers;
                            if (options.LogAnnotationSetInfo)
                            {
                                if (handlers.Length == 0)
                                {
                                    var first = options.UploadOptions.UploadHandlers?.Keys.FirstOrDefault();
                                    logger?.CommentUploadFirstAvaialbleHandler(description, first);
                                }
                                if (handlers.Length == 1)
                                {
                                    logger?.CommentUploadSingleHandler(description, handlers[0]);
                                }
                                else
                                {
                                    logger?.CommentUploadHandlers(description, handlers);
                                }
                            }
                        }

                        else if (len >= 4 && StrEquals(words[2], "as") && StrEquals(words[3], "metadata"))
                        {
                            var paramName = words[1];
                            NpgsqlRestParameter? param = routine.Parameters.FirstOrDefault(x =>
                                    string.Equals(x.ActualName, paramName, StringComparison.Ordinal) ||
                                    string.Equals(x.ConvertedName, paramName, StringComparison.Ordinal));
                            if (param is null)
                            {
                                logger?.CommentUploadWrongMetadataParam(description, paramName);
                            }
                            else
                            {
                                param.IsUploadMetadata = true;
                                if (options.LogAnnotationSetInfo)
                                {
                                    logger?.CommentUploadMetadataParam(description, paramName);
                                }
                            }
                        }
                    }
                }

                // param param_name1 is hash of param_name2
                // param param_name is upload metadata
                else if (haveTag is true && StrEqualsToArray(words[0], parameterKey))
                {
                    // param param_name1 is hash of param_name2
                    if (len >= 6 && StrEquals(words[2], "is") && StrEquals(words[3], "hash") && StrEquals(words[4], "of"))
                    {
                        var paramName1 = words[1];
                        var paramName2 = words[5];

                        var found = true;
                        NpgsqlRestParameter? param = null;

                        if (routine.OriginalParamsHash.Contains(paramName1) is false &&
                            routine.ParamsHash.Contains(paramName1) is false)
                        {
                            logger?.CommentParamNotExistsCantHash(description, paramName1);
                            found = false;
                        }

                        if (found is true &&
                            routine.OriginalParamsHash.Contains(paramName2) is false &&
                            routine.ParamsHash.Contains(paramName2) is false)
                        {
                            logger?.CommentParamNotExistsCantHash(description, paramName2);
                            found = false;
                        }

                        if (found is true)
                        {
                            param = routine.Parameters.FirstOrDefault(x =>
                                string.Equals(x.ActualName, paramName1, StringComparison.Ordinal) ||
                                string.Equals(x.ConvertedName, paramName1, StringComparison.Ordinal));
                            if (param is not null)
                            {
                                param.HashOf = routine.Parameters.FirstOrDefault(x =>
                                    string.Equals(x.ActualName, paramName2, StringComparison.Ordinal) ||
                                    string.Equals(x.ConvertedName, paramName2, StringComparison.Ordinal));
                                if (param.HashOf is null)
                                {
                                    logger?.CommentParamNotExistsCantHash(description, paramName2);
                                }
                                else
                                {
                                    if (options.LogAnnotationSetInfo)
                                    {
                                        logger?.CommentParamIsHashOf(description, paramName1, paramName2);
                                    }
                                }
                            }
                            else
                            {
                                logger?.CommentParamNotExistsCantHash(description, paramName1);
                            }
                        }
                    }

                    // param param_name1 is upload metadata
                    if (len >= 5 && (
                        StrEquals(words[2], "is") && StrEquals(words[3], "upload") && StrEquals(words[4], "metadata")
                        ))
                    {
                        if (routineEndpoint.Upload is false)
                        {
                            routineEndpoint.Upload = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentUpload(description);
                            }
                        }
                        if (routineEndpoint.RequestParamType != RequestParamType.QueryString)
                        {
                            routineEndpoint.RequestParamType = RequestParamType.QueryString;
                        }
                        if (routineEndpoint.Method != Method.POST)
                        {
                            routineEndpoint.Method = Method.POST;
                        }

                        var paramName = words[1];
                        NpgsqlRestParameter? param = routine.Parameters.FirstOrDefault(x =>
                                string.Equals(x.ActualName, paramName, StringComparison.Ordinal) ||
                                string.Equals(x.ConvertedName, paramName, StringComparison.Ordinal));
                        if (param is null)
                        {
                            logger?.CommentUploadWrongMetadataParam(description, paramName);
                        }
                        else
                        {
                            param.IsUploadMetadata = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentUploadMetadataParam(description, paramName);
                            }
                        }
                    }

                    // param param_name1 is user_id
                    if (len >= 4 && (
                        StrEquals(words[2], "is") && StrEquals(words[3], "user_id")
                        ))
                    {
                        var paramName = words[1];
                        NpgsqlRestParameter? param = routine.Parameters.FirstOrDefault(x =>
                                string.Equals(x.ActualName, paramName, StringComparison.Ordinal) ||
                                string.Equals(x.ConvertedName, paramName, StringComparison.Ordinal));
                        if (param is null)
                        {
                            logger?.CommentWrongUserIdParam(description, paramName);
                        }
                        else
                        {
                            param.IsUserId = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentUserIdParam(description, paramName);
                            }
                        }
                    }

                    // param param_name1 is user_name
                    if (len >= 4 && (
                        StrEquals(words[2], "is") && StrEquals(words[3], "user_name")
                        ))
                    {
                        var paramName = words[1];
                        NpgsqlRestParameter? param = routine.Parameters.FirstOrDefault(x =>
                                string.Equals(x.ActualName, paramName, StringComparison.Ordinal) ||
                                string.Equals(x.ConvertedName, paramName, StringComparison.Ordinal));
                        if (param is null)
                        {
                            logger?.CommentWrongUserNameParam(description, paramName);
                        }
                        else
                        {
                            param.IsUserName = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentUserNameParam(description, paramName);
                            }
                        }
                    }

                    // param param_name1 is user_roles
                    if (len >= 4 && (
                        StrEquals(words[2], "is") && StrEquals(words[3], "user_roles")
                        ))
                    {
                        var paramName = words[1];
                        NpgsqlRestParameter? param = routine.Parameters.FirstOrDefault(x =>
                                string.Equals(x.ActualName, paramName, StringComparison.Ordinal) ||
                                string.Equals(x.ConvertedName, paramName, StringComparison.Ordinal));
                        if (param is null)
                        {
                            logger?.CommentWrongUserRolesParam(description, paramName);
                        }
                        else
                        {
                            param.IsUserRoles = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentUserRolesParam(description, paramName);
                            }
                        }
                    }

                    // param param_name1 is ip_address
                    if (len >= 4 && (
                        StrEquals(words[2], "is") && StrEquals(words[3], "ip_address")
                        ))
                    {
                        var paramName = words[1];
                        NpgsqlRestParameter? param = routine.Parameters.FirstOrDefault(x =>
                                string.Equals(x.ActualName, paramName, StringComparison.Ordinal) ||
                                string.Equals(x.ConvertedName, paramName, StringComparison.Ordinal));
                        if (param is null)
                        {
                            logger?.CommentWrongIpAddressParam(description, paramName);
                        }
                        else
                        {
                            param.IsIpAddress = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentIpAddressParam(description, paramName);
                            }
                        }
                    }

                    // param param_name1 is user_claims
                    if (len >= 4 && (
                        StrEquals(words[2], "is") && StrEquals(words[3], "user_claims")
                        ))
                    {
                        var paramName = words[1];
                        NpgsqlRestParameter? param = routine.Parameters.FirstOrDefault(x =>
                                string.Equals(x.ActualName, paramName, StringComparison.Ordinal) ||
                                string.Equals(x.ConvertedName, paramName, StringComparison.Ordinal));
                        if (param is null)
                        {
                            logger?.CommentWrongUserClaimsParam(description, paramName);
                        }
                        else
                        {
                            param.IsUserClaims = true;
                            if (options.LogAnnotationSetInfo)
                            {
                                logger?.CommentUserClaimsParam(description, paramName);
                            }
                        }
                    }
                }

                // info_path [ true | false | path ]
                // info_events_path [ true | false | path ]
                // info_streaming_path [ true |false | path ]
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], infoEventsStreamingPathKey))
                {
                    if (bool.TryParse(words[1], out var parseredStreamingPath))
                    {
                        if (parseredStreamingPath is true)
                        {
                            routineEndpoint.InfoEventsStreamingPath = Consts.DefaultInfoPath;
                        }
                    }
                    else
                    {
                        routineEndpoint.InfoEventsStreamingPath = words[1];
                    }
                    logger?.CommentInfoStreamingPath(description, routineEndpoint.InfoEventsStreamingPath);
                }

                // info_scope [ [ self | matching | authorize | all ] | [ authorize [ role1, role2, role3 [, ...] ] ] ] 
                // info_events_scope [ [ self | matching | authorize | all ] | [ authorize [ role1, role2, role3 [, ...] ] ] ] 
                // info_streaming_scope [ [ self | matching | authorize | all ] | [ authorize [ role1, role2, role3 [, ...] ] ] ] 
                else if (haveTag is true && len >= 2 && StrEqualsToArray(words[0], infoEventsStreamingScopeKey))
                {
                    if (words.Length > 1 && Enum.TryParse<InfoEventsScope>(words[1], true, out var parsedScope))
                    {
                        routineEndpoint.InfoEventsScope = parsedScope;
                        if (parsedScope == InfoEventsScope.Authorize && words.Length > 2)
                        {
                            routineEndpoint.InfoEventsRoles ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var word in words[2..])
                            {
                                if (string.IsNullOrWhiteSpace(word) is false)
                                {
                                    routineEndpoint.InfoEventsRoles.Add(word);
                                }
                            }
                            logger?.CommentInfoStreamingScopeRoles(description, routineEndpoint.InfoEventsRoles);
                        }
                        else
                        {
                            logger?.CommentInfoStreamingScope(description, routineEndpoint.InfoEventsScope);
                        }
                    }
                    else
                    {
                        logger?.LogError("Could not recognize valid value for parameter key {key}. Valid values are: {values}. Provided value is {provided}.",
                            words[0], string.Join(", ", Enum.GetNames<InfoEventsScope>()), line);
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

    public static void SetCustomParameter(RoutineEndpoint endpoint, string name, string value, ILogger? logger)
    {
        value = Regex.Unescape(value);

        if (StrEqualsToArray(name, bufferRowsKey))
        {
            if (ulong.TryParse(value, out var parsedBuffer))
            {
                endpoint.BufferRows = parsedBuffer;
            }
        }
        else if (StrEqualsToArray(name, rawKey))
        {
            if (bool.TryParse(value, out var parsedRaw))
            {
                endpoint.Raw = parsedRaw;
            }
        }
        else if (StrEqualsToArray(name, separatorKey))
        {
            endpoint.RawValueSeparator = value;
        }
        else if (StrEqualsToArray(name, newLineKey))
        {
            endpoint.RawNewLineSeparator = value;
        }
        else if (StrEqualsToArray(name, columnNamesKey))
        {
            if (bool.TryParse(value, out var parsedRawColumnNames))
            {
                endpoint.RawColumnNames = parsedRawColumnNames;
            }
        }
        else if (StrEqualsToArray(name, connectionNameKey))
        {
            //if (options.ConnectionStrings is not null && options.ConnectionStrings.ContainsKey(value) is true)
            //{
            //    endpoint.ConnectionName = value;
            //}
            endpoint.ConnectionName = value;
        }
        else if (StrEqualsToArray(name, userContextKey))
        {
            if (bool.TryParse(value, out var parserUserContext))
            {
                endpoint.UserContext = parserUserContext;
            }
        }
        else if (StrEqualsToArray(name, userParemetersKey))
        {
            if (bool.TryParse(value, out var parserUserParameters))
            {
                endpoint.UseUserParameters = parserUserParameters;
            }
        }

        else if (StrEqualsToArray(name, infoEventsStreamingPathKey))
        {
            if (bool.TryParse(value, out var parseredStreamingPath))
            {
                if (parseredStreamingPath is true)
                {
                    endpoint.InfoEventsStreamingPath = Consts.DefaultInfoPath;
                }
            }
            else
            {
                endpoint.InfoEventsStreamingPath = value;
            }
        }

        else if (StrEqualsToArray(name, infoEventsStreamingScopeKey))
        {
            var words = value.SplitWords();
            if (words.Length > 0 && Enum.TryParse<InfoEventsScope>(words[0], true, out var parsedScope))
            {
                endpoint.InfoEventsScope = parsedScope;
                if (parsedScope == InfoEventsScope.Authorize && words.Length > 1)
                {
                    endpoint.InfoEventsRoles ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var word in words[1..])
                    {
                        if (string.IsNullOrWhiteSpace(word) is false)
                        {
                            endpoint.InfoEventsRoles.Add(word);
                        }
                    }
                }
            }
            else
            {
                logger?.LogError("Could not recognize valid value for parameter key {key}. Valid values are: {values}. Provided value is {provided}.", 
                    name, string.Join(", ", Enum.GetNames<InfoEventsScope>()), value);
            }
        }

        else
        {
            if (endpoint.CustomParameters is null)
            {
                endpoint.CustomParameters = new()
                {
                    [name] = value
                };
            }
            else
            {
                endpoint.CustomParameters[name] = value;
            }
        }
    }

    public static bool StrEquals(string str1, string str2) => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

    public static bool StrEqualsToArray(string str, params string[] arr)
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

    public static string[] SplitWords(this string str)
    {
        if (str is null)
        {
            return [];
        }
        return [.. str
            .Split(wordSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().ToLowerInvariant())
        ];
    }

    public static bool SplitBySeparatorChar(string str, char sep, out string part1, out string part2)
    {
        part1 = null!;
        part2 = null!;
        if (str.Contains(sep) is false)
        {
            return false;
        }

        var parts = str.Split(sep, 2);
        if (parts.Length == 2)
        {
            part1 = parts[0].Trim();
            part2 = parts[1].Trim();
            if (ContainsValidNameCharacter(part1))
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public static bool ContainsValidNameCharacter(string input)
    {
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c) is false && c != '-' && c != '_')
            {
                return true;
            }
        }
        return false;
    }
}