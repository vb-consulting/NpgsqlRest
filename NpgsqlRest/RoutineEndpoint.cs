using Microsoft.Extensions.Primitives;

namespace NpgsqlRest;

public readonly record struct RoutineEndpoint(
    string Url,
    Method Method,
    RequestParamType RequestParamType,
    bool RequiresAuthorization,
    string[] ReturnRecordNames,
    string[] ParamNames,
    int? CommandTimeout,
    string? ResponseContentType,
    Dictionary<string, StringValues> ResponseHeaders,
    RequestHeadersMode RequestHeadersMode,
    string RequestHeadersParameterName);
