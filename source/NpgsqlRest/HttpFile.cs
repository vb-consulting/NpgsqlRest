using System;
using System.Reflection.Metadata;
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
        var fullFileName = Path.Combine(Environment.CurrentDirectory, fileName);
        if (initializedFiles.Add(fileName))
        {
            if (endpoint)
            {
                StringBuilder content = new();
                content.AppendLine(string.Concat("@host=", GetHost(builder)));
                content.AppendLine();
                fileContent[fileName] = content;
            }
            else
            {
                var exists = File.Exists(fullFileName);
                if (!options.HttpFileOptions.FileOverwrite && exists)
                {
                    return;
                }
                File.WriteAllLines(fullFileName,
                [
                    string.Concat("@host=", GetHost(builder)),
                    ""
                ]);
                Logging.LogInfo(ref logger, ref options, "Created http file {0}", fullFileName);
            }
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
                        var type = routine.ParamTypes[i];
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
                string.Join("&", meta.ParamNames.Select((p, i) => $"{p}={SampleValueUnquoted(i, routine.ParamTypeDescriptor[i])}"))));
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

        if (endpoint)
        {
            fileContent[fileName].Append(sb);
        }
        
        if (file)
        {
            File.AppendAllText(fullFileName, sb.ToString());
        }
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
    }

    private static readonly string abc = new(Enumerable.Range('A', 26).Select(x => (char)x).ToArray());

    private static string SampleValueUnquoted(int i, TypeDescriptor type)
    {
        return SampleValue(i, type).Trim('"');
    }

    private static string SampleValue(int i, TypeDescriptor type)
    {
        var counter = i;
        string GetSubstring()
        {
            if (counter + 3 > abc.Length || counter <= 0)
            {
                counter = 1;
            }
            return string.Concat("\"", abc.Substring(counter-1, 3), "\"");
        }
        string GetArray(string v1, string v2, string v3)
        {
            return string.Concat("[", v1, ", ", v2, ", ", v3, "]");
        }

        if (type.IsNumeric)
        {
            if (type.IsArray)
            {
                return GetArray((counter + 1).ToString(), (counter + 2).ToString(), (counter + 3).ToString());
            }
            return (counter + 1).ToString();
        }

        if (type.IsBoolean)
        {
            if (type.IsArray)
            {
                return GetArray(((counter + 1) % 2 == 1).ToString().ToLower(), ((counter + 2) % 2 == 1).ToString().ToLower(), ((counter + 3) % 2 == 1).ToString().ToLower());
            }
            return (counter % 2 == 1).ToString().ToLower();
        }

        if (type.Type == "uuid")
        {
            return string.Concat("\"", Guid.NewGuid().ToString(), "\"");
        }

        if (type.IsDate)
        {
            return string.Concat("\"", DateTime.Now.ToString("yyyy-MM-dd"), "\"");
        }

        if (type.IsDateTime)
        {
            return string.Concat("\"", DateTime.Now.ToString("O")[..22], "\"");
        }

        if (type.IsArray)
        {
            return GetArray(GetSubstring(), GetSubstring(), GetSubstring());
        }

        if (type.IsJson)
        {
            return "{}";
        }

        return GetSubstring();
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