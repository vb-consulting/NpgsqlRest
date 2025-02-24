using Microsoft.Extensions.Primitives;

namespace NpgsqlRest;

public class RoutineEndpoint(
    string url,
    Method method,
    RequestParamType requestParamType,
    bool requiresAuthorization,
    int? commandTimeout,
    string? responseContentType,
    Dictionary<string, StringValues> responseHeaders,
    RequestHeadersMode requestHeadersMode,
    string requestHeadersParameterName,
    string? bodyParameterName,
    TextResponseNullHandling textResponseNullHandling,
    QueryStringNullHandling queryStringNullHandling,
    HashSet<string>? authorizeRoles = null,
    bool login = false,
    bool logout = false,
    ulong? bufferRows = null,
    bool raw = false,
    string? rawValueSeparator = null,
    string? rawNewLineSeparator = null,
    bool rawColumnNames = false,
    bool cached = false,
    string[]? cachedParams = null)
{
    private string? _bodyParameterName = bodyParameterName;
    internal bool HasBodyParameter = !string.IsNullOrWhiteSpace(bodyParameterName);

    internal Action<ILogger, string, string, Exception?>? LogCallback { get; set; }
    internal bool NeedsParsing { get; set; } = false;
    public string Url { get; set; } = url;
    public Method Method { get; set; } = method;
    public RequestParamType RequestParamType { get; set; } = requestParamType;
    public bool RequiresAuthorization { get; set; } = requiresAuthorization;
    public int? CommandTimeout { get; set; } = commandTimeout;
    public string? ResponseContentType { get; set; } = responseContentType;
    public Dictionary<string, StringValues> ResponseHeaders { get; set; } = responseHeaders;
    public RequestHeadersMode RequestHeadersMode { get; set; } = requestHeadersMode;
    public string RequestHeadersParameterName { get; set; } = requestHeadersParameterName;
    public string? BodyParameterName
    {
        get => _bodyParameterName;
        set
        {
            HasBodyParameter = !string.IsNullOrWhiteSpace(value);
            _bodyParameterName = value;
        }
    }
    public TextResponseNullHandling TextResponseNullHandling { get; set; } = textResponseNullHandling;
    public QueryStringNullHandling QueryStringNullHandling { get; set; } = queryStringNullHandling;
    public HashSet<string>? AuthorizeRoles { get; set; } = authorizeRoles;
    public bool Login { get; set; } = login;
    public bool Logout { get; set; } = logout;
    public bool IsAuth => Login || Logout;
    public ulong? BufferRows { get; set; } = bufferRows;
    public bool Raw { get; set; } = raw;
    public string? RawValueSeparator { get; set; } = rawValueSeparator;
    public string? RawNewLineSeparator { get; set; } = rawNewLineSeparator;
    public bool RawColumnNames { get; set; } = rawColumnNames;
    public string[][]? CommentWordLines { get; internal set; }
    public bool Cached { get; set; } = cached;
    public HashSet<string>? CachedParams { get; set; } = cachedParams?.ToHashSet();
}