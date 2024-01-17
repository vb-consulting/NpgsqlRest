using Microsoft.Extensions.Primitives;

namespace NpgsqlRest;

public readonly struct RoutineEndpoint(
    string url,
    Method method,
    RequestParamType requestParamType = RequestParamType.QueryString,
    bool requiresAuthorization = false,
    string[]? returnRecordNames = null,
    string[]? paramNames = null,
    int? commandTimeout = null,
    string? responseContentType = null,
    Dictionary<string, StringValues>? responseHeaders = null)
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
}