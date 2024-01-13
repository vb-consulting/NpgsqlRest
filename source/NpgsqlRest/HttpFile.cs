using System;
using System.Text;
using Npgsql;

namespace NpgsqlRest;

internal class HttpFile(IApplicationBuilder builder, NpgsqlRestOptions options, ILogger? logger)
{
    private readonly HashSet<string> initializedFiles = [];
    private readonly IApplicationBuilder builder = builder;
    private readonly Dictionary<string, StringBuilder> fileContent = [];

    private NpgsqlRestOptions options = options;
    private ILogger? logger = logger;

    private bool endpoint = false;
    private bool file = false;

    internal void HandleEntry(Routine routine, RoutineEndpointMeta meta)
    {
        if (options.HttpFileOptions.Option == HttpFileOption.Disabled)
        {
            return;
        }

        endpoint = options.HttpFileOptions.Option == HttpFileOption.Endpoint || options.HttpFileOptions.Option == HttpFileOption.Both;
        file = options.HttpFileOptions.Option == HttpFileOption.File || options.HttpFileOptions.Option == HttpFileOption.Both;

        string formatfileName()
        {
            var name = GetName(options);
            var schema = options.HttpFileOptions.FileMode != HttpFileMode.Schema ? "" : string.Concat("_", routine.Schema);
            return string.Concat(string.Format(options.HttpFileOptions.NamePattern, name, schema), ".http");
        }
        var fileName = formatfileName();
        if (initializedFiles.Add(fileName))
        {
            StringBuilder content = new();
            content.AppendLine(string.Concat("@host=", GetHost(builder)));
            content.AppendLine();
            fileContent[fileName] = content;
        }

        StringBuilder sb = new();

        if (options.HttpFileOptions.CommentHeader != CommentHeader.None)
        {
            if (options.HttpFileOptions.CommentHeader == CommentHeader.Simple)
            {
                sb.AppendLine(string.Concat("// ",
                    routine.TypeInfo, " ",
                    routine.Schema, ".",
                    routine.Name, "(",
                    routine.ParamCount == 0 ? ")" : ""));
                if (routine.ParamCount > 0)
                {
                    for (var i = 0; i < routine.ParamCount; i++)
                    {
                        var name = routine.ParamNames[i];
                        var defaultValue = routine.ParamDefaults[i];
                        var paramType = routine.ParamTypes[i];
                        var type = defaultValue == null ? paramType : $"{paramType} DEFAULT {defaultValue}";
                        sb.AppendLine(string.Concat("//     ", name, " ", type, i == routine.ParamCount - 1 ? "" : ","));
                    }
                    sb.AppendLine("// )");
                }
                if (!routine.ReturnsRecord)
                {
                    sb.AppendLine(string.Concat("// returns ", routine.ReturnType));
                }
                else
                {
                    if (routine.ReturnsUnnamedSet)
                    {
                        sb.AppendLine(string.Concat("// returns setof ", routine.ReturnRecordTypes[0]));
                    }
                    else
                    {
                        sb.AppendLine("// returns table(");

                        for (var i = 0; i < routine.ReturnRecordCount; i++)
                        {
                            var name = routine.ReturnRecordNames[i];
                            var type = routine.ReturnRecordTypes[i];
                            sb.AppendLine(string.Concat("//     ", name, " ", type, i == routine.ReturnRecordCount - 1 ? "" : ","));
                        }
                        sb.AppendLine("// )");
                    }
                }
            }
            else if (options.HttpFileOptions.CommentHeader == CommentHeader.Full)
            {
                foreach (var line in routine.Definition.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line == "\r")
                    {
                        continue;
                    }
                    sb.AppendLine(string.Concat("// ", line.TrimEnd('\r')));
                }
            }
        }
        if (meta.ParamNames.Length == 0 || meta.Parameters != EndpointParameters.QueryString)
        {
            sb.AppendLine(string.Concat(meta.HttpMethod, " {{host}}", meta.Url));
        }

        if (meta.ParamNames.Length > 0 && meta.Parameters == EndpointParameters.QueryString)
        {
            sb.AppendLine(string.Concat(meta.HttpMethod, " {{host}}", meta.Url, "?",
                string.Join("&", meta
                    .ParamNames
                    .Select((p, i) =>
                    {
                        var descriptor = routine.ParamTypeDescriptor[i];
                        var value = Uri.EscapeDataString(SampleValueUnquoted(i, descriptor));
                        if (descriptor.IsArray)
                        {
                            return string.Join("&", value.Split(',').Select(v => $"{p}={v}"));
                        }
                        return $"{p}={value}";
                    }))));
        }

        if (meta.ParamNames.Length > 0 && meta.Parameters == EndpointParameters.BodyJson)
        {
            sb.AppendLine("content-type: application/json");
            sb.AppendLine();
            sb.AppendLine("{");
            foreach(var (p, i)  in meta.ParamNames.Select((p, i) => (p, i)))
            {
                sb.AppendLine(string.Concat(
                    "    \"", p, 
                    "\": ",
                    SampleValue(i, routine.ParamTypeDescriptor[i]),
                    i == meta.ParamNames.Length - 1 ? "" : ","));
            }
            sb.AppendLine("}");
        }

        sb.AppendLine();
        sb.AppendLine("###");
        sb.AppendLine();

        fileContent[fileName].Append(sb);
    }

    internal void FinalizeHttpFile()
    {
        if (endpoint && builder is IEndpointRouteBuilder app)
        {
            foreach(var (fileName, content) in fileContent)
            {
                var url = $"/{fileName}";
                app?.MapGet(url, () => content.ToString());
                Logging.LogInfo(ref logger, ref options, "Exposed HTTP file content on URL: {0}{1}", GetHost(builder), url);
            }
        }

        if (file)
        {
            foreach(var (fileName, content) in fileContent)
            {
                var fullFileName = Path.Combine(Environment.CurrentDirectory, fileName);
                if (!options.HttpFileOptions.FileOverwrite && File.Exists(fullFileName))
                {
                    continue;
                }
                File.WriteAllText(fullFileName, content.ToString());
                Logging.LogInfo(ref logger, ref options, "Created HTTP file: {0}", fullFileName);
            }
        }
    }

    private static string SampleValueUnquoted(int i, TypeDescriptor type)
    {
        return SampleValue(i, type, isQuery: true).Trim('"');
    }

    private static string SampleValue(int i, TypeDescriptor type, bool isQuery = false)
    {
        var counter = i;
        string GetSubstring(int? value = null) => ((value ?? counter) % 3) switch
        {
            0 => "ABC",
            1 => "XYZ",
            2 => "IJK",
            _ => "WTF"
        };

        static string Quote(string value) => string.Concat("\"", value, "\"");

        string GetArray(string v1, string v2, string v3, bool quoted)
        {
            if (isQuery)
            {
                return string.Concat(v1, ",", v2, ",", v3);
            }
            return string.Concat("[", Quote(v1), ", ", Quote(v2), ", ", Quote(v3), "]");
        }

        if (type.IsNumeric)
        {
            if (string.Equals(type.Type, "real", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type.Type, "double precision", StringComparison.OrdinalIgnoreCase))
            {
                if (type.IsArray)
                {
                    return GetArray(
                        $"{counter + 2}.{counter + 3}",
                        $"{counter + 3}.{counter + 4}",
                        $"{counter + 4}.{counter + 5}",
                        false);
                }
                return $"{counter+1}.{counter+2}";
            }
            else

            if (type.IsArray)
            {
                return GetArray((counter + 1).ToString(), (counter + 2).ToString(), (counter + 3).ToString(), false);
            }
            return (counter + 1).ToString();
        }

        if (type.IsBoolean)
        {
            if (type.IsArray)
            {
                return GetArray(
                    ((counter + 1) % 2 == 1).ToString().ToLower(), 
                    ((counter + 2) % 2 == 1).ToString().ToLower(), 
                    ((counter + 3) % 2 == 1).ToString().ToLower(),
                    false);
            }
            return (counter % 2 == 1).ToString().ToLower();
        }

        if (type.Type == "uuid")
        {
            if (type.IsArray)
            {
                return GetArray(
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    true);
            }
            return Quote(Guid.NewGuid().ToString());
        }

        if (type.IsDateTime)
        {
            if (type.IsArray)
            {
                return GetArray(
                    DateTime.Now.AddDays(-2).ToString("O")[..22],
                    DateTime.Now.AddDays(-1).ToString("O")[..22],
                    DateTime.Now.ToString("O")[..22],
                    true);
            }
            return Quote(DateTime.Now.ToString("O")[..22]);
        }

        if (type.IsDate)
        {
            if (type.IsArray)
            {
                return GetArray(
                    DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"),
                    DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    true);
            }
            return Quote(DateTime.Now.ToString("yyyy-MM-dd"));
        }

        if (type.IsJson)
        {
            if (type.IsArray)
            {
                return GetArray("{}", "{}", "{}", false);
            }
            return "{}";
        }

        if (string.Equals(type.Type, "jsonpath", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    $"$.user.addresses[0].city",
                    $"$.user.addresses[1].street",
                    $"$.user.addresses[2].postalNumber",
                true);
            }
            return Quote($"$.user.addresses[0].city");
        }

        if (string.Equals(type.Type, "time without time zone", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    DateTime.Now.AddDays(-2).TimeOfDay.ToString()[..^1],
                    DateTime.Now.AddDays(-1).TimeOfDay.ToString()[..^1],
                    DateTime.Now.TimeOfDay.ToString()[..^1],
                true);
            }
            return Quote(DateTime.Now.TimeOfDay.ToString()[..^1]);
        }

        if (string.Equals(type.Type, "time with time zone", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    string.Concat(DateTime.Now.AddDays(-2).TimeOfDay.ToString()[..^1], "+01:00"),
                    string.Concat(DateTime.Now.AddDays(-1).TimeOfDay.ToString()[..^1], "+01:00"),
                    string.Concat(DateTime.Now.TimeOfDay.ToString()[..^1], "+01:00"),
                true);
            }
            return Quote(string.Concat(DateTime.Now.TimeOfDay.ToString()[..^1], "+01:00"));
        }

        if (string.Equals(type.Type, "interval", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    $"{counter + 1} minutes {(counter + 2) % 60} seconds",
                    $"{counter + 2} minutes {(counter + 3) % 60} seconds",
                    $"{counter + 3} minutes {(counter + 4) % 60} seconds",
                true);
            }
            return Quote($"{counter} minutes {(counter + 1) % 60} seconds");
        }

        if (string.Equals(type.Type, "varbit", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(type.Type, "bit varying", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    $"110",
                    $"101",
                    $"011",
                true);
            }
            return Quote($"{(counter) % 2}{(counter+1) % 2}{(counter+2) % 2}");
        }

        if (string.Equals(type.Type, "inet", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    $"192.168.5.{counter}",
                    $"192.168.5.{counter+1}",
                    $"192.168.5.{counter+2}",
                true);
            }
            return Quote($"192.168.5.{counter}");
        }

        if (string.Equals(type.Type, "macaddr", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    $"00-B0-D0-63-C2-26",
                    $"00-B0-D0-63-C2-26",
                    $"00-B0-D0-63-C2-26",
                true);
            }
            return Quote($"00-B0-D0-63-C2-26");
        }

        if (string.Equals(type.Type, "bytea", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsArray)
            {
                return GetArray(
                    $"\\\\xFEADBAEF",
                    $"\\\\xAEADBDEF",
                    $"\\\\xEEADEEEF",
                true);
            }
            return Quote($"\\\\xDEADBEEF");
        }

        if (type.IsArray)
        {
            return GetArray(GetSubstring(counter), GetSubstring(counter + 1), GetSubstring(counter + 2), true);
        }

        return Quote(GetSubstring());
    }

    private static string GetHost(IApplicationBuilder builder)
    {
        string? host = null;
        if (builder is WebApplication app)
        {
            if (app.Urls.Count != 0)
            {
                host = app.Urls.FirstOrDefault();
            }
            else
            {
                host = app.Configuration.GetValue<string>("ASPNETCORE_URLS")?.Split(";")?.LastOrDefault();
            }
        }
        // default, assumed host
        host ??= "http://localhost:5000";
        return host;
    }

    private static string GetName(NpgsqlRestOptions options)
    {
        return new NpgsqlConnectionStringBuilder(options.ConnectionString).Database ??
            options?.ConnectionString?.Split(";").Where(s => s.StartsWith("Database=")).FirstOrDefault()?.Split("=")?.Last() ??
            "npgsql";
    }
}