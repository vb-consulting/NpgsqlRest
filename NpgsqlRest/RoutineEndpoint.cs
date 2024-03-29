using Microsoft.Extensions.Primitives;
namespace NpgsqlRest;

public struct RoutineEndpoint(
    string url,
    Method method,
    RequestParamType requestParamType,
    bool requiresAuthorization,
    string[] returnRecordNames,
    string[] paramNames,
    int? commandTimeout,
    string? responseContentType,
    Dictionary<string, StringValues> responseHeaders,
    RequestHeadersMode requestHeadersMode,
    string requestHeadersParameterName,
    string? bodyParameterName,
    TextResponseNullHandling textResponseNullHandling,
    QueryStringNullHandling queryStringNullHandling,
    HashSet<string>? authorizeRoles = null)
{
    internal HashSet<string> ParamsNameHash { get; } = new(paramNames);
    internal Action<ILogger, string, string, Exception?>? LogCallback { get; set; }
    public string Url { get; set; } = url;
    public Method Method { get; set; } = method;
    public RequestParamType RequestParamType { get; set; } = requestParamType;
    public bool RequiresAuthorization { get; set; } = requiresAuthorization;
    public string[] ReturnRecordNames { get; set; } = returnRecordNames;
    public string[] ParamNames { get; set; } = paramNames;
    public int? CommandTimeout { get; set; } = commandTimeout;
    public string? ResponseContentType { get; set; } = responseContentType;
    public Dictionary<string, StringValues> ResponseHeaders { get; set; } = responseHeaders;
    public RequestHeadersMode RequestHeadersMode { get; set; } = requestHeadersMode;
    public string RequestHeadersParameterName { get; set; } = requestHeadersParameterName;
    public string? BodyParameterName { get; set; } = bodyParameterName;
    public TextResponseNullHandling TextResponseNullHandling { get; set; } = textResponseNullHandling;
    public QueryStringNullHandling QueryStringNullHandling { get; set; } = queryStringNullHandling;
    public HashSet<string>? AuthorizeRoles { get; set; } = authorizeRoles;
}