using System.Text;

namespace NpgsqlRest.TsClient;

public class TsClient(TsClientOptions options) : IEndpointCreateHandler
{
    private readonly StringBuilder _content = new();
    private IApplicationBuilder _builder = default!;
    private ILogger? _logger;
    private readonly HashSet<string> _models = [];
    private readonly Dictionary<string, int> _names = [];

    public void Setup(IApplicationBuilder builder, ILogger? logger)
    {
        _builder = builder;
        _logger = logger;
        _models.Clear();
        _content.Clear();
        _names.Clear();
        _content.AppendLine(
            """
            const parseQuery = (query: Record<any, any>) => "?" + Object.keys(query)
                .map(key => {
                    const value = query[key] ? query[key] : "";
                    if (Array.isArray(value)) {
                        return value.map(s => s ? `${key}=${encodeURIComponent(s)}` : `${key}=`).join("&");
                    }
                    return `${key}=${encodeURIComponent(value)}`;
                })
                .join("&");

            """);

        _content.AppendFormat("const baseUrl = \"{0}\";", GetHost());
        _content.AppendLine();
    }

    public void Handle(Routine routine, RoutineEndpoint endpoint)
    {
        var name = string.Concat(endpoint.Url, "-", endpoint.Method.ToString().ToLowerInvariant());
        if (_names.TryGetValue(name, out var count))
        {
            _names[name] = count + 1;
            name = string.Concat(name, "-", count);
        }
        else
        {
            _names.Add(name, 1);
        }
        var pascal = ConvertToPascalCase(name);
        var camel = ConvertToCamelCase(name);

        _content.AppendLine();

        string? requestName = null;
        if (routine.ParamCount > 0)
        {
            requestName = $"I{pascal}Request";
            if (_models.Contains(requestName) is false)
            {
                _content.AppendLine($"interface {requestName} {{");
                for (var i = 0; i < routine.ParamCount; i++)
                {
                    var descriptor = routine.ParamTypeDescriptor[i];
                    var nameSuffix = descriptor.HasDefault ? "?" : "";
                    var type = GetTsType(descriptor, true);

                    _content.AppendLine($"    {endpoint.ParamNames[i]}{nameSuffix}: {type} | null;");
                }
                _content.AppendLine("}");
                _content.AppendLine();
                _models.Add(requestName);
            }
        }

        string responseName = "void";
        bool json = false;
        string? returnExp = null;
        if (routine.IsVoid is false)
        {
            if (routine.ReturnsRecord is false)
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
                    responseName = "string[][]";
                    returnExp = "return await response.json() as string[][];";
                }
                else
                {
                    responseName = $"I{pascal}Response";
                    returnExp = $"return await response.json() as {responseName};";

                    if (_models.Contains(responseName) is false)
                    {
                        _content.AppendLine($"interface {responseName} {{");
                        for (var i = 0; i < routine.ReturnRecordCount; i++)
                        {
                            var descriptor = routine.ReturnTypeDescriptor[i];
                            var type = GetTsType(descriptor, false);

                            _content.AppendLine($"    {endpoint.ReturnRecordNames[i]}: {type} | null;");
                        }
                        _content.AppendLine("}");
                        _content.AppendLine();
                        _models.Add(responseName);
                    }
                }
            }
        }

        string NewLine(string? input, int ident) => 
            input is null ? "" : string.Concat(Environment.NewLine, string.Concat(Enumerable.Repeat("    ", ident)), input);

        var headers = json ? 
            @"headers: { ""Content-Type"": ""application/json"" }," : null;

        var body = endpoint.RequestParamType == RequestParamType.BodyJson && requestName is not null ?
            @"body: JSON.stringify(request)" : null;

        var qs = endpoint.RequestParamType == RequestParamType.QueryString && requestName is not null ? " + parseQuery(request)" : "";

        var funcBody = string.Format(
            """
                {0}await fetch(baseUrl + "{1}"{2}, {{
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

        _content.AppendLine(string.Format(
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
        _content.AppendLine("}");
    }

    public void Cleanup()
    {
        if (!options.FileOverwrite && File.Exists(options.FilePath))
        {
            return;
        }
        File.WriteAllText(options.FilePath, _content.ToString());
        _logger?.LogInformation("Created Typescript file: {0}", options.FilePath);
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