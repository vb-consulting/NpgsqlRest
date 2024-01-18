using Microsoft.Extensions.Primitives;

namespace NpgsqlRest;

public readonly struct RoutineEndpoint(
    string url,
    Method method,
    RequestParamType requestParamType,
    bool requiresAuthorization,
    string[]? returnRecordNames,
    string[]? paramNames,
    int? commandTimeout,
    string? responseContentType,
    Dictionary<string, StringValues>? responseHeaders,
    RequestHeadersMode requestHeadersMode,
    string requestHeadersParameterName)
{
    public readonly string Url = url;
    public readonly Method HttpMethod = method;
    public readonly RequestParamType RequestParamType = requestParamType;
    public readonly bool RequiresAuthorization = requiresAuthorization;
    public readonly string[] ReturnRecordNames = returnRecordNames ?? [];
    public readonly string[] ParamNames { get; } = paramNames ?? [];
    public readonly int? CommandTimeout = commandTimeout;
    public readonly string? ResponseContentType = responseContentType;
    public readonly Dictionary<string, StringValues> ResponseHeaders = responseHeaders ?? [];
    public readonly RequestHeadersMode RequestHeadersMode = requestHeadersMode;
    public readonly string RequestHeadersParameterName = requestHeadersParameterName;
}