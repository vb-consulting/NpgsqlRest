// dotnet publish -r win-x64 -c Release
// dotnet publish -r linux-x64 -c Release
using System.Diagnostics;
using NpgsqlRest;
using NpgsqlRest.Defaults;
using NpgsqlRestClient;

using Npgsql;

Stopwatch sw = new();
sw.Start();

var arguments = new Arguments();
if (arguments.Parse(args) is false)
{
    return;
}

var config = new Config();
var builder = new Builder(config);
var appInstance = new App(config, builder);

config.Build(args);
builder.BuildInstance();
builder.BuildLogger();
var (connectionString, retryOpts) = builder.BuildConnectionString();
if (connectionString is null)
{
    return;
}
var connectionStrings = config.GetConfigBool("UseMultipleConnections", config.NpgsqlRestCfg, true) ? builder.BuildConnectionStringDict() : null;
builder.BuildDataProtection();
builder.BuildAuthentication();
var usingCors = builder.BuildCors();
var compressionEnabled = builder.ConfigureResponseCompression();

var antiforgerUsed = builder.ConfigureAntiForgery();

WebApplication app = builder.Build();
appInstance.Configure(app, () =>
{
    
    sw.Stop();
    var message = config.GetConfigStr("StartupMessage", config.Cfg) ?? "Started in {0}, listening on {1}, version {2}";
    if (string.IsNullOrEmpty(message) is false)
    {
        builder.Logger?.Information(message,
                sw,
                app.Urls, 
                System.Reflection.Assembly.GetAssembly(typeof(Program))?.GetName()?.Version?.ToString() ?? "-",
                builder.Instance.Environment.EnvironmentName,
                builder.Instance.Environment.ApplicationName);

    }
});

var (authenticationOptions, authCfg) = appInstance.CreateNpgsqlRestAuthenticationOptions();

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
appInstance.ConfigureStaticFiles(app, authenticationOptions);

var refreshOptionsCfg = config.NpgsqlRestCfg.GetSection("RefreshOptions");

await using var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
var logConnectionNoticeEventsMode = config.GetConfigEnum<PostgresConnectionNoticeLoggingMode?>("LogConnectionNoticeEventsMode", config.NpgsqlRestCfg) ?? PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage;

appInstance.ConfigureThreadPool();

NpgsqlRestOptions options = new()
{
    DataSource = dataSource,
    ServiceProviderMode = ServiceProviderObject.None,
    ConnectionStrings = connectionStrings,
    ConnectionRetryOptions = retryOpts,
    SchemaSimilarTo = config.GetConfigStr("SchemaSimilarTo", config.NpgsqlRestCfg),
    SchemaNotSimilarTo = config.GetConfigStr("SchemaNotSimilarTo", config.NpgsqlRestCfg),
    IncludeSchemas = config.GetConfigEnumerable("IncludeSchemas", config.NpgsqlRestCfg)?.ToArray(),
    ExcludeSchemas = config.GetConfigEnumerable("ExcludeSchemas", config.NpgsqlRestCfg)?.ToArray(),
    NameSimilarTo = config.GetConfigStr("NameSimilarTo", config.NpgsqlRestCfg),
    NameNotSimilarTo = config.GetConfigStr("NameNotSimilarTo", config.NpgsqlRestCfg),
    IncludeNames = config.GetConfigEnumerable("IncludeNames", config.NpgsqlRestCfg)?.ToArray(),
    ExcludeNames = config.GetConfigEnumerable("ExcludeNames", config.NpgsqlRestCfg)?.ToArray(),
    UrlPathPrefix = config.GetConfigStr("UrlPathPrefix", config.NpgsqlRestCfg) ?? "/api",
    UrlPathBuilder = config.GetConfigBool("KebabCaseUrls", config.NpgsqlRestCfg, true) ? DefaultUrlBuilder.CreateUrl : appInstance.CreateUrl,
    NameConverter = config.GetConfigBool("CamelCaseNames", config.NpgsqlRestCfg, true) ? DefaultNameConverter.ConvertToCamelCase : n => n?.Trim('"'),
    RequiresAuthorization = config.GetConfigBool("RequiresAuthorization", config.NpgsqlRestCfg, true),

    LoggerName = config.GetConfigStr("ApplicationName", config.Cfg),
    LogEndpointCreatedInfo = config.GetConfigBool("LogEndpointCreatedInfo", config.NpgsqlRestCfg, true),
    LogAnnotationSetInfo = config.GetConfigBool("LogEndpointCreatedInfo", config.NpgsqlRestCfg, true),
    LogConnectionNoticeEvents = config.GetConfigBool("LogConnectionNoticeEvents", config.NpgsqlRestCfg, true),
    LogCommands = config.GetConfigBool("LogCommands", config.NpgsqlRestCfg),
    LogCommandParameters = config.GetConfigBool("LogCommandParameters", config.NpgsqlRestCfg),
    LogConnectionNoticeEventsMode = logConnectionNoticeEventsMode,

    CommandTimeout = config.GetConfigInt("CommandTimeout", config.NpgsqlRestCfg),
    DefaultHttpMethod = config.GetConfigEnum<Method?>("DefaultHttpMethod", config.NpgsqlRestCfg),
    DefaultRequestParamType = config.GetConfigEnum<RequestParamType?>("DefaultRequestParamType", config.NpgsqlRestCfg),
    CommentsMode = config.GetConfigEnum<CommentsMode?>("CommentsMode", config.NpgsqlRestCfg) ?? CommentsMode.OnlyWithHttpTag,
    RequestHeadersMode = config.GetConfigEnum<RequestHeadersMode?>("RequestHeadersMode", config.NpgsqlRestCfg) ?? RequestHeadersMode.Ignore,
    RequestHeadersContextKey = config.GetConfigStr("RequestHeadersContextKey", config.NpgsqlRestCfg) ?? "request.headers",
    RequestHeadersParameterName = config.GetConfigStr("RequestHeadersParameterName", config.NpgsqlRestCfg) ?? "_headers",

    EndpointCreated = appInstance.CreateEndpointCreatedHandler(authCfg),
    ValidateParameters = null,
    ReturnNpgsqlExceptionMessage = config.GetConfigBool("ReturnNpgsqlExceptionMessage", config.NpgsqlRestCfg, true),
    PostgreSqlErrorCodeToHttpStatusCodeMapping = appInstance.CreatePostgreSqlErrorCodeToHttpStatusCodeMapping(),
    BeforeConnectionOpen = appInstance.BeforeConnectionOpen(connectionString, authenticationOptions),
    AuthenticationOptions = authenticationOptions,
    EndpointCreateHandlers = appInstance.CreateCodeGenHandlers(connectionString),
    CustomRequestHeaders = builder.GetCustomHeaders(),
    ExecutionIdHeaderName = config.GetConfigStr("ExecutionIdHeaderName", config.NpgsqlRestCfg) ?? "X-NpgsqlRest-ID",
    CustomServerSentEventsResponseHeaders = builder.GetCustomServerSentEventsResponseHeaders(),

    RoutineSources = appInstance.CreateRoutineSources(),
    RefreshEndpointEnabled = config.GetConfigBool("Enabled", refreshOptionsCfg, false),
    RefreshPath = config.GetConfigStr("Path", refreshOptionsCfg) ?? "/api/npgsqlrest/refresh",
    RefreshMethod = config.GetConfigStr("Method", refreshOptionsCfg) ?? "GET",
    UploadOptions = appInstance.CreateUploadOptions(),
};

app.UseNpgsqlRest(options);
// Create instances with extracted configuration values - allows initialization objects to be GC'd
var externalAuth = new ExternalAuth(builder.ExternalAuthConfig, connectionString, builder.Logger);
var tokenRefreshAuth = new TokenRefreshAuth(builder.BearerTokenConfig);
// Call instance methods - maintains same API behavior
externalAuth.Configure(app, options, logConnectionNoticeEventsMode);
tokenRefreshAuth.Configure(app);
app.Run();
