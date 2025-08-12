// dotnet publish -r win-x64 -c Release
// dotnet publish -r linux-x64 -c Release
using System.Diagnostics;
using NpgsqlRest;
using NpgsqlRest.Defaults;
using NpgsqlRestClient;

using Npgsql;

var arguments = new Arguments();
if (arguments.Parse(args) is false)
{
    return;
}

var config = new Config();
var builder = new Builder(config);
var app_instance = new App(config, builder);

Stopwatch sw = new();
sw.Start();

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
app_instance.Configure(app, () =>
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

var (authenticationOptions, authCfg) = app_instance.CreateNpgsqlRestAuthenticationOptions();

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
app_instance.ConfigureStaticFiles(app, authenticationOptions);

var refreshOptionsCfg = config.NpgsqlRestCfg.GetSection("RefreshOptions");

await using var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
var logConnectionNoticeEventsMode = config.GetConfigEnum<PostgresConnectionNoticeLoggingMode?>("LogConnectionNoticeEventsMode", config.NpgsqlRestCfg) ?? PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage;

app_instance.ConfigureThreadPool();

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
    UrlPathBuilder = config.GetConfigBool("KebabCaseUrls", config.NpgsqlRestCfg, true) ? DefaultUrlBuilder.CreateUrl : app_instance.CreateUrl,
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

    EndpointCreated = app_instance.CreateEndpointCreatedHandler(authCfg),
    ValidateParameters = null,
    ReturnNpgsqlExceptionMessage = config.GetConfigBool("ReturnNpgsqlExceptionMessage", config.NpgsqlRestCfg, true),
    PostgreSqlErrorCodeToHttpStatusCodeMapping = app_instance.CreatePostgreSqlErrorCodeToHttpStatusCodeMapping(),
    BeforeConnectionOpen = app_instance.BeforeConnectionOpen(connectionString, authenticationOptions),
    AuthenticationOptions = authenticationOptions,
    EndpointCreateHandlers = app_instance.CreateCodeGenHandlers(connectionString),
    CustomRequestHeaders = builder.GetCustomHeaders(),
    ExecutionIdHeaderName = config.GetConfigStr("ExecutionIdHeaderName", config.NpgsqlRestCfg) ?? "X-NpgsqlRest-ID",
    CustomServerSentEventsResponseHeaders = builder.GetCustomServerSentEventsResponseHeaders(),

    RoutineSources = app_instance.CreateRoutineSources(),
    RefreshEndpointEnabled = config.GetConfigBool("Enabled", refreshOptionsCfg, false),
    RefreshPath = config.GetConfigStr("Path", refreshOptionsCfg) ?? "/api/npgsqlrest/refresh",
    RefreshMethod = config.GetConfigStr("Method", refreshOptionsCfg) ?? "GET",
    UploadOptions = app_instance.CreateUploadOptions(),
};

app.UseNpgsqlRest(options);
// Create instances with extracted configuration values - allows initialization objects to be GC'd
var externalAuth = new ExternalAuth(builder.ExternalAuthConfig, connectionString, builder.Logger);
var tokenRefreshAuth = new TokenRefreshAuth(builder.BearerTokenConfig);
// Call instance methods - maintains same API behavior
externalAuth.Configure(app, options, logConnectionNoticeEventsMode);
tokenRefreshAuth.Configure(app);
app.Run();
