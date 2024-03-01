using System.Text;

namespace NpgsqlRest.TsClient;

public class TsClient(TsClientOptions options) : IEndpointCreateHandler
{
    private IApplicationBuilder _builder = default!;
    private ILogger? _logger;
    private NpgsqlRestOptions _options;

    public void Setup(IApplicationBuilder builder, ILogger? logger, NpgsqlRestOptions options)
    {
        _builder = builder;
        _logger = logger;
        _options = options;
    }

    public void Cleanup(ref (Routine routine, RoutineEndpoint endpoint)[] endpoints)
    {
        HashSet<string> models___ = [];
        Dictionary<string, string> modelsDict = [];
        Dictionary<string, int> names = [];
        StringBuilder content = new();
        StringBuilder interfaces = new();

        content.AppendLine(string.Format(
            """
            const _baseUrl = "{0}";

            const _parseQuery = (query: Record<any, any>) => "?" + Object.keys(query)
                .map(key => {{
                    const value = query[key] ? query[key] : "";
                    if (Array.isArray(value)) {{
                        return value.map(s => s ? `${{key}}=${{encodeURIComponent(s)}}` : `${{key}}=`).join("&");
                    }}
                    return `${{key}}=${{encodeURIComponent(value)}}`;
                }})
                .join("&");
            """, GetHost())
        );

        foreach (var (routine, endpoint) in endpoints
            .Where(e => e.routine.Type == RoutineType.Table || e.routine.Type == RoutineType.View)
            .OrderBy(e => e.routine.Schema)
            .ThenBy(e => e.routine.Type)
            .ThenBy(e => e.routine.Name))
        {
            Handle(routine, endpoint);
        }

        foreach (var (routine, endpoint) in endpoints
            .Where(e => (e.routine.Type == RoutineType.Table || e.routine.Type == RoutineType.View) is false)
            .OrderBy(e => e.routine.Schema)
            .ThenBy(e => e.routine.Name))
        {
            Handle(routine, endpoint);
        }

        if (!options.FileOverwrite && File.Exists(options.FilePath))
        {
            return;
        }
        interfaces.AppendLine(content.ToString());
        File.WriteAllText(options.FilePath, interfaces.ToString());
        _logger?.LogInformation("Created Typescript file: {0}", options.FilePath);

        return;

        void Handle(Routine routine, RoutineEndpoint endpoint)
        {
            var name = string.IsNullOrEmpty(_options.UrlPathPrefix) ? endpoint.Url : endpoint.Url[_options.UrlPathPrefix.Length..];
            if (routine.Type == RoutineType.Table || routine.Type == RoutineType.View)
            {
                name = string.Concat(name, "-", endpoint.Method.ToString().ToLowerInvariant());
            }

            if (names.TryGetValue(name, out var count))
            {
                names[name] = count + 1;
                name = string.Concat(name, "-", count);
            }
            else
            {
                names.Add(name, 1);
            }
            var pascal = ConvertToPascalCase(name);
            var camel = ConvertToCamelCase(name);

            content.AppendLine();

            string? requestName = null;
            if (routine.ParamCount > 0)
            {
                StringBuilder req = new();
                requestName = $"I{pascal}Request";

                for (var i = 0; i < routine.ParamCount; i++)
                {
                    var descriptor = routine.ParamTypeDescriptor[i];
                    var nameSuffix = descriptor.HasDefault ? "?" : "";
                    var type = GetTsType(descriptor, true);

                    req.AppendLine($"    {endpoint.ParamNames[i]}{nameSuffix}: {type} | null;");
                }

                if (modelsDict.TryGetValue(req.ToString(), out var newName))
                {
                    requestName = newName;
                }
                else
                {
                    modelsDict.Add(req.ToString(), requestName);
                    req.Insert(0, $"interface {requestName} {{{Environment.NewLine}");
                    req.AppendLine("}");
                    req.AppendLine();
                    interfaces.Append(req);
                }
            }

            string responseName = "void";
            bool json = false;
            string? returnExp = null;
            if (routine.IsVoid is false)
            {
                if (routine.ReturnsSet == false && routine.ReturnRecordCount == 1 && routine.ReturnsRecordType is false)
                {
                    var descriptor = routine.ReturnTypeDescriptor[0];
                    responseName = GetTsType(descriptor, true);
                    if (descriptor.IsArray)
                    {
                        json = true;
                        returnExp = $"return await response.json() as {responseName}[];";
                    }
                    else
                    {
                        if (descriptor.IsDate || descriptor.IsDateTime)
                        {
                            returnExp = "return new Date(await response.text());";
                        }
                        else if (descriptor.IsNumeric)
                        {
                            returnExp = "return Number(await response.text());";
                        }
                        else if (descriptor.IsBoolean)
                        {
                            returnExp = "return (await response.text()).toLowerCase() == \"true\";";
                        }
                        else
                        {
                            returnExp = "return await response.text();";
                        }
                    }
                }
                else
                {
                    json = true;
                    if (routine.ReturnsUnnamedSet)
                    {
                        responseName = "string[]";
                    }
                    else
                    {
                        StringBuilder resp = new();
                        responseName = $"I{pascal}Response";

                        for (var i = 0; i < routine.ReturnRecordCount; i++)
                        {
                            var descriptor = routine.ReturnTypeDescriptor[i];
                            var type = GetTsType(descriptor, false);

                            resp.AppendLine($"    {endpoint.ReturnRecordNames[i]}: {type} | null;");
                        }

                        if (modelsDict.TryGetValue(resp.ToString(), out var newName))
                        {
                            responseName = newName;
                        }
                        else
                        {
                            modelsDict.Add(resp.ToString(), responseName);
                            resp.Insert(0, $"interface {responseName} {{{Environment.NewLine}");
                            resp.AppendLine("}");
                            resp.AppendLine();
                            interfaces.Append(resp);
                        }
                    }
                    if (routine.ReturnsSet)
                    {
                        responseName = string.Concat(responseName, "[]");
                    }
                    returnExp = $"return await response.json() as {responseName};";
                    /*
                    if (routine.ReturnsUnnamedSet)
                    {
                        responseName = "string[][]";
                        returnExp = "return await response.json() as string[][];";
                    }
                    else
                    {
                        StringBuilder resp = new();
                        responseName = $"I{pascal}Response";

                        for (var i = 0; i < routine.ReturnRecordCount; i++)
                        {
                            var descriptor = routine.ReturnTypeDescriptor[i];
                            var type = GetTsType(descriptor, false);

                            resp.AppendLine($"    {endpoint.ReturnRecordNames[i]}: {type} | null;");
                        }

                        if (modelsDict.TryGetValue(resp.ToString(), out var newName))
                        {
                            responseName = newName;
                        }
                        else
                        {
                            modelsDict.Add(resp.ToString(), responseName);
                            resp.Insert(0, $"interface {responseName} {{{Environment.NewLine}");
                            resp.AppendLine("}");
                            resp.AppendLine();
                            interfaces.Append(resp);
                        }
                        returnExp = $"return await response.json() as {responseName};";
                    }
                    */
                } 
            }

            string NewLine(string? input, int ident) =>
                input is null ? "" : string.Concat(Environment.NewLine, string.Concat(Enumerable.Repeat("    ", ident)), input);

            var headers = json ?
                @"headers: { ""Content-Type"": ""application/json"" }," : null;

            var body = endpoint.RequestParamType == RequestParamType.BodyJson && requestName is not null ?
                @"body: JSON.stringify(request)" : null;

            var qs = endpoint.RequestParamType == RequestParamType.QueryString && requestName is not null ? " + _parseQuery(request)" : "";

            var funcBody = string.Format(
                """
                {0}await fetch(_baseUrl + "{1}"{2}, {{
                    method: "{3}",{4}{5}
                }});{6}
            """,
                routine.IsVoid ? "" : "const response = ",
                endpoint.Url,
                qs,
                endpoint.Method,
                NewLine(headers, 2),
                NewLine(body, 2),
                NewLine(returnExp, 1));

            content.AppendLine(string.Format(
                """
            /**
            {0}
            */
            export async function {1}({2}) : Promise<{3}> {{
            {4}
            """,
                GetComment(routine, endpoint),
                camel,
                requestName is null ? "" : string.Concat("request: ", requestName),
                responseName,
                funcBody));
            content.AppendLine("}");

        } // void Handle
    }

    private string GetComment(Routine routine, RoutineEndpoint endpoint)
    {
        StringBuilder sb = new();
        if (options.CommentHeader != CommentHeader.None)
        {
            var comment = options.CommentHeader switch
            {
                CommentHeader.Simple => routine.SimpleDefinition,
                CommentHeader.Full => routine.FullDefinition,
                _ => "",
            };
            foreach (var line in comment.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line == "\r")
                {
                    continue;
                }
                sb.AppendLine(string.Concat("* ", line.TrimEnd('\r')));
            }
            sb.AppendLine("* ");
            sb.AppendLine("* @remarks");
            sb.AppendLine(string.Format("* {0} {1}", endpoint.Method, endpoint.Url));
        }
        else
        {
            sb.AppendLine(string.Format("* {0} {1}", endpoint.Method, endpoint.Url));
        }
        sb.AppendLine("* ");
        sb.Append(string.Format("* @see {0} {1}.{2}", routine.Type.ToString().ToUpperInvariant(), routine.Schema, routine.Name));
        return sb.ToString();
    }

    private static string GetTsType(TypeDescriptor descriptor, bool useDateType)
    {
        var type = "string";
        
        if (useDateType && (descriptor.IsDate || descriptor.IsDateTime))
        {
            type = "Date";
        }
        else if (descriptor.IsNumeric)
        {
            type = "number";
        }
        else if (descriptor.IsBoolean)
        {
            type = "boolean";
        }

        if (descriptor.IsArray)
        {
            type = string.Concat(type, "[]");
        }
        return type;
    }

    private static readonly char[] separator = ['_', '-', '/', '\\'];

    public static string ConvertToPascalCase(string value)
    {
        return value
            .Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select((s, i) =>
                string.Concat(i == 0 ? char.ToUpperInvariant(s[0]) : char.ToUpperInvariant(s[0]), s[1..]))
            .Aggregate(string.Empty, string.Concat)
            .Trim('"');
    }

    public static string ConvertToCamelCase(string value)
    {
        return value
            .Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select((s, i) =>
                string.Concat(i == 0 ? char.ToLowerInvariant(s[0]) : char.ToUpperInvariant(s[0]), s[1..]))
            .Aggregate(string.Empty, string.Concat)
            .Trim('"');
    }

    private string GetHost()
    {
        if (options.IncludeHost is false)
        {
            return "";
        }
        if (options.CustomHost is not null)
        {
            return options.CustomHost;
        }
        string? host = null;
        if (_builder is WebApplication app)
        {
            if (app.Urls.Count != 0)
            {
                host = app.Urls.FirstOrDefault();
            }
            else
            {
                var section = app.Configuration?.GetSection("ASPNETCORE_URLS");
                if (section?.Value is not null)
                {
                    host = section.Value.Split(";")?.LastOrDefault();
                }
            }
        }
        // default, assumed host
        host ??= "http://localhost:5000";
        return host;
    }
}