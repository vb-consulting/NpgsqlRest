namespace NpgsqlRest;

internal static partial class Defaults
{
    internal static RoutineEndpointMeta? DefaultMetaBuilder(Routine routine, NpgsqlRestOptions options, string url)
    {
        var hasGet = routine.Name.Contains("get", StringComparison.OrdinalIgnoreCase);
        var method = hasGet ? Method.GET : Method.POST;
        var requestParamType = method == Method.GET ? RequestParamType.QueryString : RequestParamType.BodyJson;
        string[] returnRecordNames = routine.ReturnRecordNames.Select(s => options.NameConverter(s) ?? "").ToArray();
        string[] paramNames = routine
            .ParamNames
            .Select((s, i) => 
            {
                var result = options.NameConverter(s) ?? "";
                if (string.IsNullOrEmpty(result))
                {
                    result = $"${i + 1}";
                }
                return result;
            })
            .ToArray();
        return new(
                url: url,
                method: method,
                requestParamType: requestParamType,
                requiresAuthorization: options.RequiresAuthorization,
                returnRecordNames: returnRecordNames,
                paramNames: paramNames,
                commandTimeout: options.CommandTimeout,
                responseContentType: null);
    }
}
