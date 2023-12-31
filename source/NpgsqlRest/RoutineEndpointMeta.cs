﻿using Microsoft.Extensions.Primitives;

namespace NpgsqlRest;

public class RoutineEndpointMeta(
    string url,
    Method method,
    EndpointParameters parameters = EndpointParameters.QueryString,
    bool requiresAuthorization = false,
    string[]? returnRecordNames = null,
    string[]? paramNames = null,
    int? commandTimeout = null,
    string? responseContentType = null,
    Dictionary<string, StringValues>? responseHeaders = null)
{
    public string Url { get; set;  } = url;
    public Method HttpMethod { get; set; } = method;
    public EndpointParameters Parameters { get; set; } = parameters;
    public bool RequiresAuthorization { get; set; } = requiresAuthorization;
    public string[] ReturnRecordNames { get; set; } = returnRecordNames ?? [];
    public string[] ParamNames { get; } = paramNames ?? [];
    public int? CommandTimeout { get; set; } = commandTimeout;
    public string? ResponseContentType { get; set; } = responseContentType;
    public Dictionary<string, StringValues> ResponseHeaders { get; set; } = responseHeaders ?? [];
}