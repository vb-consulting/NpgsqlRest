using NpgsqlRest;
using NpgsqlRest.Defaults;
using NpgsqlRest.HttpFiles;

var builder = WebApplication.CreateEmptyBuilder(new ());
builder.WebHost.UseKestrelCore();

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

string? connectionName = GetStr("ConnectionName");
if (connectionName is null)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ConnectionName not found in appsettings.json");
    Console.ResetColor();
    return;
}
var connectionString = config.GetConnectionString(connectionName);
if (connectionString is null)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Connection string not found in appsettings.json");
    Console.ResetColor();
    return;
}

if (GetBool("UseLogging"))
{
    builder.Logging
        .AddConfiguration(config.GetSection("Logging"))
        .AddConsole();
}

var app = builder.Build();

var urls = GetArray("Urls");
if (urls != null)
{
    foreach (var url in urls)
    {
        app.Urls.Add(url);
    }
}

var httpFileOptions = config?.GetSection("HttpFileOptions");

var npgsqlRestOptions = new NpgsqlRestOptions()
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
    LogParameterMismatchWarnings = GetBool("LogParameterMismatchWarnings"),

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
        })
    ]
};

//npgsqlRestOptions.RoutineSources.Add(new )

app.UseNpgsqlRest(npgsqlRestOptions);
app.Run();
return;

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

static string CreateUrl(Routine routine, NpgsqlRestOptions options) =>
    string.Concat(
        string.IsNullOrEmpty(options.UrlPathPrefix) ? "/" : string.Concat("/", options.UrlPathPrefix.Trim('/')),
        routine.Schema == "public" ? "" : routine.Schema.Trim('"').Trim('/'),
        "/",
        routine.Name.Trim('"').Trim('/'),
        "/");

