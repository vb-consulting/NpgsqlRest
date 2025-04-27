// dotnet publish -r win-x64 -c Release
// dotnet publish -r linux-x64 -c Release
using System.Diagnostics;
using NpgsqlRest;
using NpgsqlRest.Defaults;
using NpgsqlRestClient;

using static NpgsqlRestClient.Config;
using static NpgsqlRestClient.Builder;
using static NpgsqlRestClient.App;
using Npgsql;

if (Arguments.Parse(args) is false)
{
    return;
}

Stopwatch sw = new();
sw.Start();

Build(args);
BuildInstance();
BuildLogger();
var connectionString = BuildConnectionString();
if (connectionString is null)
{
    return;
}
var connectionStrings = GetConfigBool("UseMultipleConnections", NpgsqlRestCfg, true) ? BuildConnectionStringDict() : null;
BuildAuthentication();
var usingCors = BuildCors();
var compressionEnabled = ConfigureResponseCompression();

var antiforgerUsed = ConfigureAntiForgery();

WebApplication app = Build();
Configure(app, () =>
{
    sw.Stop();
    var message = GetConfigStr("StartupMessage", Cfg);
    if (string.IsNullOrEmpty(message) is false)
    {
        Logger?.Information(message,
                sw,
                app.Urls, 
                System.Reflection.Assembly.GetAssembly(typeof(Program))?.GetName()?.Version?.ToString() ?? "-");
    }
});

if (usingCors)
{
    app.UseCors();
}
if (compressionEnabled)
{
    app.UseResponseCompression();
}
if (antiforgerUsed)
{
    app.UseAntiforgery();
}
ConfigureStaticFiles(app);

var refreshOptionsCfg = NpgsqlRestCfg.GetSection("RefreshOptions");

await using var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
var logConnectionNoticeEventsMode = GetConfigEnum<PostgresConnectionNoticeLoggingMode?>("LogConnectionNoticeEventsMode", NpgsqlRestCfg) ?? PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage;

var (paramHandler, defaultParser) = CreateParametersHandlers();
(string defaultUploadHandler, Dictionary<string, Func<IUploadHandler>>? uploadHandlers) = CreateUploadHandlers();

if (uploadHandlers is not null && uploadHandlers.Count > 1)
{
    Logger?.Information("Using {0} upload handlers where {1} is default.", uploadHandlers.Keys, defaultUploadHandler);
}

NpgsqlRestOptions options = new()
{
    DataSource = dataSource,
    ServiceProviderMode = ServiceProviderObject.None,
    ConnectionStrings = connectionStrings,
    SchemaSimilarTo = GetConfigStr("SchemaSimilarTo", NpgsqlRestCfg),
    SchemaNotSimilarTo = GetConfigStr("SchemaNotSimilarTo", NpgsqlRestCfg),
    IncludeSchemas = GetConfigEnumerable("IncludeSchemas", NpgsqlRestCfg)?.ToArray(),
    ExcludeSchemas = GetConfigEnumerable("ExcludeSchemas", NpgsqlRestCfg)?.ToArray(),
    NameSimilarTo = GetConfigStr("NameSimilarTo", NpgsqlRestCfg),
    NameNotSimilarTo = GetConfigStr("NameNotSimilarTo", NpgsqlRestCfg),
    IncludeNames = GetConfigEnumerable("IncludeNames", NpgsqlRestCfg)?.ToArray(),
    ExcludeNames = GetConfigEnumerable("ExcludeNames", NpgsqlRestCfg)?.ToArray(),
    UrlPathPrefix = GetConfigStr("UrlPathPrefix", NpgsqlRestCfg) ?? "/api",
    UrlPathBuilder = GetConfigBool("KebabCaseUrls", NpgsqlRestCfg, true) ? DefaultUrlBuilder.CreateUrl : App.CreateUrl,
    NameConverter = GetConfigBool("CamelCaseNames", NpgsqlRestCfg, true) ? DefaultNameConverter.ConvertToCamelCase : n => n?.Trim('"'),
    RequiresAuthorization = GetConfigBool("RequiresAuthorization", NpgsqlRestCfg, true),

    LoggerName = GetConfigStr("ApplicationName", Cfg),
    LogEndpointCreatedInfo = GetConfigBool("LogEndpointCreatedInfo", NpgsqlRestCfg, true),
    LogAnnotationSetInfo = GetConfigBool("LogEndpointCreatedInfo", NpgsqlRestCfg, true),
    LogConnectionNoticeEvents = GetConfigBool("LogConnectionNoticeEvents", NpgsqlRestCfg, true),
    LogCommands = GetConfigBool("LogCommands", NpgsqlRestCfg),
    LogCommandParameters = GetConfigBool("LogCommandParameters", NpgsqlRestCfg),
    LogConnectionNoticeEventsMode = logConnectionNoticeEventsMode,

    CommandTimeout = GetConfigInt("CommandTimeout", NpgsqlRestCfg),
    DefaultHttpMethod = GetConfigEnum<Method?>("DefaultHttpMethod", NpgsqlRestCfg),
    DefaultRequestParamType = GetConfigEnum<RequestParamType?>("DefaultRequestParamType", NpgsqlRestCfg),
    CommentsMode = GetConfigEnum<CommentsMode?>("CommentsMode", NpgsqlRestCfg) ?? CommentsMode.OnlyWithHttpTag,
    RequestHeadersMode = GetConfigEnum<RequestHeadersMode?>("RequestHeadersMode", NpgsqlRestCfg) ?? RequestHeadersMode.Ignore,
    RequestHeadersParameterName = GetConfigStr("RequestHeadersParameterName", NpgsqlRestCfg) ?? "_headers",

    EndpointCreated = CreateEndpointCreatedHandler(),
    ValidateParameters = paramHandler,
    ReturnNpgsqlExceptionMessage = GetConfigBool("ReturnNpgsqlExceptionMessage", NpgsqlRestCfg, true),
    PostgreSqlErrorCodeToHttpStatusCodeMapping = CreatePostgreSqlErrorCodeToHttpStatusCodeMapping(),
    BeforeConnectionOpen = BeforeConnectionOpen(connectionString),

    AuthenticationOptions = new()
    {
        DefaultAuthenticationType = GetConfigStr("DefaultAuthenticationType", AuthCfg)
    },

    EndpointCreateHandlers = CreateCodeGenHandlers(connectionString),
    CustomRequestHeaders = GetCustomHeaders(),

    RoutineSources = CreateRoutineSources(),
    RefreshEndpointEnabled = GetConfigBool("Enabled", refreshOptionsCfg, false),
    RefreshPath = GetConfigStr("Path", refreshOptionsCfg) ?? "/api/npgsqlrest/refresh",
    RefreshMethod = GetConfigStr("Method", refreshOptionsCfg) ?? "GET",
    DefaultResponseParser = defaultParser,

    UploadHandlers = uploadHandlers,
    DefaultUploadHandler = defaultUploadHandler,
};

app.UseNpgsqlRest(options);
ExternalAuth.Configure(app, options, logConnectionNoticeEventsMode);
TokenRefreshAuth.Configure(app);
app.Run();
