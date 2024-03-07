using Npgsql;
using NpgsqlRest;
using NpgsqlRest.CrudSource;
using NpgsqlRest.Defaults;
using NpgsqlRest.HttpFiles;
using NpgsqlRest.TsClient;
using Serilog;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
var builder = WebApplication.CreateBuilder([]);
var config = builder.Configuration;
builder.Host.UseSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(config.GetSection("Logging"))
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger());

var app = builder.Build();
app.UseSerilogRequestLogging();

var httpFileOptions = config?.GetSection("HttpFileOptions");
var crudSource = config?.GetSection("CrudSource");
var tsClient = config?.GetSection("TsClient");
var connectionString = GetConnectionString(config);

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,

    SchemaSimilarTo = GetStr("SchemaSimilarTo"),
    SchemaNotSimilarTo = GetStr("SchemaNotSimilarTo"),
    IncludeSchemas = GetArray("IncludeSchemas"),
    ExcludeSchemas = GetArray("ExcludeSchemas"),
    NameSimilarTo = GetStr("NameSimilarTo"),
    NameNotSimilarTo = GetStr("NameNotSimilarTo"),
    IncludeNames = GetArray("IncludeNames"),
    ExcludeNames = GetArray("ExcludeNames"),
    UrlPathPrefix = GetStr("UrlPathPrefix"),
    UrlPathBuilder = GetBool("KebabCaseUrls") ? DefaultUrlBuilder.CreateUrl : CreateUrl,

    NameConverter = GetBool("CamelCaseNames") ? DefaultNameConverter.ConvertToCamelCase : n => n?.Trim('"'),
    RequiresAuthorization = GetBool("RequiresAuthorization"),

    LoggerName = GetStr("LoggerName"),
    LogEndpointCreatedInfo = GetBool("LogEndpointCreatedInfo"),
    LogAnnotationSetInfo = GetBool("LogAnnotationSetInfo"),
    LogConnectionNoticeEvents = GetBool("LogConnectionNoticeEvents"),
    LogCommands = GetBool("LogCommands"),

    CommandTimeout = GetInt("CommandTimeout"),
    DefaultHttpMethod = GetEnum<Method?>("DefaultHttpMethod"),
    DefaultRequestParamType = GetEnum<RequestParamType?>("DefaultRequestParamType"),
    CommentsMode = GetEnum<CommentsMode>("CommentsMode"),
    RequestHeadersMode = GetEnum<RequestHeadersMode>("RequestHeadersMode"),
    RequestHeadersParameterName = GetStr("RequestHeadersParameterName") ?? "headers",

    EndpointCreateHandlers = [
        new HttpFile(new HttpFileOptions
        {
            Option = GetEnum<HttpFileOption>("Option", httpFileOptions),
            NamePattern = GetStr("NamePattern", httpFileOptions) ?? "{0}{1}",
            CommentHeader = GetEnum<CommentHeader>("CommentHeader", httpFileOptions),
            CommentHeaderIncludeComments = GetBool("CommentHeaderIncludeComments", httpFileOptions),
            FileMode = GetEnum<HttpFileMode>("FileMode", httpFileOptions),
            FileOverwrite = GetBool("FileOverwrite", httpFileOptions),
            ConnectionString = connectionString
        }),
        new TsClient(new TsClientOptions
        {
            FilePath = GetStr("FilePath", tsClient),
            FileOverwrite = GetBool("FileOverwrite", tsClient),
            IncludeHost = GetBool("IncludeHost", tsClient),
            CustomHost = GetStr("CustomHost", tsClient),
            CommentHeader = GetEnum<CommentHeader>("CommentHeader", tsClient)
        })
    ],

    SourcesCreated = sources =>
    {
        sources.Add(new CrudSource
        {
            SchemaSimilarTo = GetStr("SchemaSimilarTo", crudSource),
            SchemaNotSimilarTo = GetStr("SchemaNotSimilarTo", crudSource),
            IncludeSchemas = GetArray("IncludeSchemas", crudSource),
            ExcludeSchemas = GetArray("ExcludeSchemas", crudSource),
            NameSimilarTo = GetStr("NameSimilarTo", crudSource),
            NameNotSimilarTo = GetStr("NameNotSimilarTo", crudSource),
            IncludeNames = GetArray("IncludeNames", crudSource),
            ExcludeNames = GetArray("ExcludeNames", crudSource),
            CrudTypes = GetFlag<CrudCommandType>("CrudTypes", crudSource),
            ReturningUrlPattern = GetStr("ReturningUrlPattern", crudSource) ?? "{0}/returning",
            OnConflictDoNothingUrlPattern = GetStr("OnConflictDoNothingUrlPattern", crudSource) ?? "{0}/on-conflict-do-nothing",
            OnConflictDoNothingReturningUrlPattern = GetStr("OnConflictDoNothingReturningUrlPattern", crudSource) ?? "{0}/on-conflict-do-nothing/returning",
            OnConflictDoUpdateUrlPattern = GetStr("OnConflictDoUpdateUrlPattern", crudSource) ?? "{0}/on-conflict-do-update",
            OnConflictDoUpdateReturningUrlPattern = GetStr("OnConflictDoUpdateReturningUrlPattern", crudSource) ?? "{0}/on-conflict-do-update/returning",
            CommentsMode = GetEnum<CommentsMode>("CommentsMode", crudSource),
        });
    },
});
app.Run();
return;

string? GetConnectionString(ConfigurationManager? config)
{
    string? connectionName = GetStr("ConnectionName");
    if (connectionName is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ConnectionName not found in appsettings.json");
        Console.ResetColor();
        return null;
    }
    var connectionString = config?.GetConnectionString(connectionName);
    if (connectionString is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Connection string not found in appsettings.json");
        Console.ResetColor();
        return null;
    }

    return connectionString;
}

string? GetStr(string key, IConfiguration? subsection = null)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    return string.IsNullOrEmpty(section?.Value) ? null : section.Value;
}

bool GetBool(string key, IConfiguration? subsection = null)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    return string.Equals(section?.Value, "true", StringComparison.OrdinalIgnoreCase);
}

string[]? GetArray(string key, IConfiguration? subsection = null)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    var children = section?.GetChildren();
    if (children is null)
    {
        return null;
    }
    return children.Select(c => c.Value ?? "").ToArray();
}

int? GetInt(string key, IConfiguration? subsection = null)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    if (section?.Value is null)
    {
        return null;
    }
    if (int.TryParse(section.Value, out var value))
    {
        return value;
    }
    return null;
}

T? GetEnum<T>(string key, IConfiguration? subsection = null)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    if (string.IsNullOrEmpty(section?.Value))
    {
        return default;
    }
    var type = typeof(T);
    var nullable = Nullable.GetUnderlyingType(type);
    var names = Enum.GetNames(nullable ?? type);
    foreach (var name in names)
    {
        if (string.Equals(section.Value, name, StringComparison.OrdinalIgnoreCase))
        {
            return (T)Enum.Parse(nullable ?? type, name);
        }
    }
    return default;
}

T? GetFlag<T>(string key, IConfiguration? subsection = null)
{
    var array = GetArray(key, subsection);
    if (array is null)
    {
        return default;
    }

    var type = typeof(T);
    var nullable = Nullable.GetUnderlyingType(type);
    var names = Enum.GetNames(nullable ?? type);

    T? result = default;
    foreach (var value in array)
    {
        foreach (var name in names)
        {
            if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
            {
                var e = (T)Enum.Parse(nullable ?? type, name);
                if (result is null)
                {
                    result = e;
                }
                else
                {
                    result = (T)Enum.ToObject(type, Convert.ToInt32(result) | Convert.ToInt32(e));
                }
            }
        }
    }
    return result;
}

static string CreateUrl(Routine routine, NpgsqlRestOptions options) =>
    string.Concat(
        string.IsNullOrEmpty(options.UrlPathPrefix) ? "/" : string.Concat("/", options.UrlPathPrefix.Trim('/')),
        routine.Schema == "public" ? "" : routine.Schema.Trim('"').Trim('/'),
        "/",
        routine.Name.Trim('"').Trim('/'),
        "/");