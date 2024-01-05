namespace NpgsqlRest;

public enum EndpointParameters
{
    QueryString, BodyJson
}

public class RoutineEndpointMeta(
    string url,
    HttpMethod method,
    EndpointParameters parameters = EndpointParameters.QueryString,
    bool requiresAuthorization = false,
    string[]? returnRecordNames = null,
    string[]? paramNames = null)
{
    public string Url { get; set;  } = url;
    public HttpMethod HttpMethod { get; set; } = method;
    public EndpointParameters Parameters { get; set; } = parameters;
    public bool RequiresAuthorization { get; set; } = requiresAuthorization;
    public string[] ReturnRecordNames { get; set; } = returnRecordNames ?? [];
    public string[] ParamNames { get; } = paramNames ?? [];
}