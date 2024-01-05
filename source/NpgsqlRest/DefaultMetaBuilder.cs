namespace NpgsqlRest;

internal static partial class Defaults
{
    internal static RoutineEndpointMeta? DefaultMetaBuilder(Routine routine, NpgsqlRestOptions options, string url)
    {
        var isPost = routine.Name.Contains("post", StringComparison.OrdinalIgnoreCase);
        return new(
                url: url,
                method: isPost ? HttpMethod.Post : HttpMethod.Get,
                parameters: isPost ? EndpointParameters.BodyJson : EndpointParameters.QueryString,
                requiresAuthorization: options.RequiresAuthorization,
                returnRecordNames: routine.ReturnRecordNames.Select(s => options.NameConverter(s)).ToArray(),
                paramNames: routine.ParamNames.Select(s => options.NameConverter(s)).ToArray());
    }
}
