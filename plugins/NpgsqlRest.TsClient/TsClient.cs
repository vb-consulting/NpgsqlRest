using System.Text;

namespace NpgsqlRest.TsClient;

public class TsClient(TsClientOptions options) : IEndpointCreateHandler
{
    private IApplicationBuilder _builder = default!;
    private ILogger? _logger;
    private NpgsqlRestOptions? _npgsqlRestoptions;

    public TsClient(string filePath) : this(new TsClientOptions(filePath)) { }

    public TsClient(string filePath, 
        bool fileOverwrite = false, 
        bool includeHost = false, 
        string? customHost = null, 
        CommentHeader commentHeader = CommentHeader.Simple,
        bool includeStatusCode = false) : this(new TsClientOptions(filePath, fileOverwrite, includeHost, customHost, commentHeader, includeStatusCode)) { }

    public void Setup(IApplicationBuilder builder, ILogger? logger, NpgsqlRestOptions options)
    {
        if (builder is WebApplication app)
        {
            var factory = app.Services.GetRequiredService<ILoggerFactory>();
            if (factory is not null)
            {
                _logger = factory.CreateLogger(options.LoggerName ?? typeof(TsClient).Namespace ?? "NpgsqlRest.HttpFiles");
            }
            else
            {
                _logger = app.Logger;
            }
        }
        else
        {
            _logger = logger;
        }
        _builder = builder;
        _npgsqlRestoptions = options;
    }

    public void Cleanup(ref (Routine routine, RoutineEndpoint endpoint)[] endpoints)
    {
        if (options.FilePath is null)
        {
            return;
        }

        if (options.BySchema is false)
        {
            Run(endpoints, options.FilePath);
        }
        else
        {
            if (options.FilePath.Contains("{0}") is false)
            {
                _logger?.LogError("TsClient Option FilePath doesn't contain {{0}} formatter and BySchema options is true. Some files may be overwritten! Existing...");
                return;
            }

            foreach (var group in endpoints.GroupBy(e => e.routine.Schema))
            {
                var filename = string.Format(options.FilePath, ConvertToCamelCase(group.Key));
                Run([.. group], filename);
            }
        }
    }

    private void Run((Routine routine, RoutineEndpoint endpoint)[] endpoints, string fileName)
    {
        if (fileName is null)
        {
            return;
        }
        Dictionary<string, string> modelsDict = [];
        Dictionary<string, int> names = [];
        StringBuilder content = new();
        StringBuilder interfaces = new();

        if (endpoints.Where(e => e.endpoint.RequestParamType == RequestParamType.QueryString).Any())
        {
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
        }
        else
        {
            content.AppendLine(string.Format(
      """
            const _baseUrl = "{0}";
            """, GetHost()));
        }

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

        if (!options.FileOverwrite && File.Exists(fileName))
        {
            return;
        }

        var dir = Path.GetDirectoryName(fileName);
        if (dir is not null && Directory.Exists(dir) is false)
        {
            Directory.CreateDirectory(dir);
        }

        if (options.CreateSeparateTypeFile is false)
        {
            interfaces.AppendLine(content.ToString());
            AddHeader(interfaces);
            File.WriteAllText(fileName, interfaces.ToString());
            _logger?.LogInformation("Created Typescript file: {0}", fileName);
        }
        else
        {
            var typeFile = fileName.Replace(".ts", "Types.d.ts");
            AddHeader(interfaces);
            File.WriteAllText(typeFile, interfaces.ToString());
            _logger?.LogInformation("Created Typescript type file: {0}", typeFile);

            AddHeader(content);
            File.WriteAllText(fileName, content.ToString());
            _logger?.LogInformation("Created Typescript file: {0}", fileName);
        }
        return;

        void AddHeader(StringBuilder sb)
        {
            if (options.HeaderLines.Count == 0)
            {
                return;
            }
            var now = DateTime.Now.ToString("O");
            sb.Insert(0, string.Join(Environment.NewLine, options.HeaderLines.Select(l => string.Format(l, now))));
        }

        void Handle(Routine routine, RoutineEndpoint endpoint)
        {
            var name = string.IsNullOrEmpty(_npgsqlRestoptions?.UrlPathPrefix) ? endpoint.Url : endpoint.Url[_npgsqlRestoptions.UrlPathPrefix.Length..];
            var routineType = routine.Type;
            var paramCount = routine.ParamCount;
            var paramTypeDescriptors = routine.ParamTypeDescriptor;
            var isVoid = routine.IsVoid;
            var returnsSet = routine.ReturnsSet;
            var columnCount = routine.ColumnCount;
            var returnsRecordType = routine.ReturnsRecordType;
            var columnsTypeDescriptor = routine.ColumnsTypeDescriptor;
            var returnsUnnamedSet = routine.ReturnsUnnamedSet;

            if (endpoint.Login is true)
            {
                isVoid = false;
                returnsSet = false;
                columnCount = 1;
                returnsRecordType = false;
                columnsTypeDescriptor = [new TypeDescriptor("text")];
            }

            if (endpoint.Logout is true)
            {
                isVoid = true;
            }

            if (routineType == RoutineType.Table || routineType == RoutineType.View)
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
            if (paramCount > 0)
            {
                StringBuilder req = new();
                requestName = $"I{pascal}Request";

                for (var i = 0; i < paramCount; i++)
                {
                    var descriptor = paramTypeDescriptors[i];
                    var nameSuffix = descriptor.HasDefault ? "?" : "";
                    var type = GetTsType(descriptor, false);

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
            string GetReturnExp(string responseExp)
            {
                if (options.IncludeStatusCode)
                {
                    //return string.Concat("return {status: response.status, response: ", responseExp, "};");
                    return string.Concat(
                        "return {", 
                        Environment.NewLine,
                        "        status: response.status,",
                        Environment.NewLine,
                        "        response: ", 
                        (responseExp == "await response.text()" ? responseExp : string.Concat("response.status == 200 ? ", responseExp, " : await response.text()")),
                        Environment.NewLine,
                        "    };");
                }
                return string.Concat("return ", responseExp, ";");
            }

            if (isVoid is false)
            {
                if (returnsSet == false && columnCount == 1 && returnsRecordType is false)
                {
                    var descriptor = columnsTypeDescriptor[0];
                    responseName = GetTsType(descriptor, true);
                    if (descriptor.IsArray)
                    {
                        json = true;
                        returnExp = GetReturnExp($"await response.json() as {responseName}[]");
                    }
                    else
                    {
                        if (descriptor.IsDate || descriptor.IsDateTime)
                        {
                            returnExp = GetReturnExp("new Date(await response.text())");
                        }
                        else if (descriptor.IsNumeric)
                        {
                            returnExp = GetReturnExp("Number(await response.text())");
                        }
                        else if (descriptor.IsBoolean)
                        {
                            returnExp = GetReturnExp("(await response.text()).toLowerCase() == \"true\"");
                        }
                        else
                        {
                            returnExp = GetReturnExp("await response.text()");
                        }
                    }
                }
                else
                {
                    json = true;
                    if (returnsUnnamedSet)
                    {
                        responseName = "string[]";
                    }
                    else
                    {
                        StringBuilder resp = new();
                        responseName = $"I{pascal}Response";

                        for (var i = 0; i < columnCount; i++)
                        {
                            var descriptor = columnsTypeDescriptor[i];
                            var type = GetTsType(descriptor, false);

                            resp.AppendLine($"    {endpoint.ColumnNames[i]}: {type} | null;");
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
                    if (returnsSet)
                    {
                        responseName = string.Concat(responseName, "[]");
                    }
                    returnExp = GetReturnExp($"await response.json() as {responseName}"); 
                }
            }
            else
            {
                if (options.IncludeStatusCode)
                {
                    returnExp = "return response.status;";
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
                isVoid && options.IncludeStatusCode is false ? "" : "const response = ",
                endpoint.Url,
                qs,
                endpoint.Method,
                NewLine(headers, 2),
                NewLine(body, 2),
                NewLine(returnExp, 1));

            string resultType;
            if (string.Equals(responseName, "void", StringComparison.OrdinalIgnoreCase))
            {
                resultType = options.IncludeStatusCode ?
                    "number" :
                    responseName;
            }
            else
            {
                resultType = options.IncludeStatusCode ?
                    string.Concat("{status: number, response: ", responseName, (responseName == "string" ? "" : " | string"), "}") :
                    responseName;
            }

            content.AppendLine(string.Format(
                """
            /**
            {0}
            */
            export async function {1}({2}) : Promise<{3}> {{
            {4}
            """,
                GetComment(routine),
                camel,
                requestName is null ? "" : string.Concat("request: ", requestName),
                resultType,
                funcBody));
            content.AppendLine("}");

        } // void Handle
    }

    private string GetComment(Routine routine)
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

            //sb.AppendLine("* ");
            //sb.AppendLine("* @remarks");
            //sb.AppendLine(string.Format("* {0} {1}", endpoint.Method, endpoint.Url));
            if (options.CommentHeaderIncludeComments is true && string.IsNullOrEmpty(routine.Comment?.Trim()) is false)
            {
                sb.AppendLine("* ");
                sb.AppendLine("* @remarks");
                var lines = routine
                    .Comment
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .ToArray();
                foreach (var (line, index) in lines.Select((l, i) => (l, i)))
                {
                    if (line == "\r" && index > 0)
                    {
                        continue;
                    }
                    var commentLine = line.Replace("'", "''").TrimEnd('\r');
                    if (index == 0)
                    {
                        commentLine = string.Concat($"comment on function {routine.Schema}.{routine.Name} is '", commentLine);
                    }
                    else if (index == lines.Length - 1)
                    {
                        commentLine = string.Concat(commentLine, "';");
                    }
                    sb.AppendLine(string.Concat("* ", commentLine));
                }
            }
        }
        else
        {
            //sb.AppendLine(string.Format("* {0} {1}", endpoint.Method, endpoint.Url));
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
                if (host is null && app.Configuration?["urls"] is not null)
                {
                    host = app.Configuration?["urls"];
                }
            }
        }
        // default, assumed host
        host ??= "http://localhost:5000";
        return host;
    }
}