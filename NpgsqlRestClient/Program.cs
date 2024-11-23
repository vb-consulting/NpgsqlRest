// dotnet publish -r win-x64 -c Release
// dotnet publish -r linux-x64 -c Release
using System.Diagnostics;
using NpgsqlRest;
using NpgsqlRest.Defaults;
using NpgsqlRestClient;

using static NpgsqlRestClient.Config;
using static NpgsqlRestClient.Builder;
using static NpgsqlRestClient.App;

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
BuildAuthentication();
BuildCors();
var compressionEnabled = ConfigureResponseCompression();

WebApplication app = Build();

Configure(app, () =>
{
    sw.Stop();
    Logger?.Information("Started in {0}", sw);
    Logger?.Information("Listening on {0}", app.Urls);
});

if (compressionEnabled)
{
    app.UseResponseCompression();
}
ConfigureStaticFiles(app);

NpgsqlRestOptions options = new()
{
    ConnectionString = connectionString,
    ConnectionFromServiceProvider = false,

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

    LogEndpointCreatedInfo = GetConfigBool("LogEndpointCreatedInfo", NpgsqlRestCfg, true),
    LogAnnotationSetInfo = GetConfigBool("LogEndpointCreatedInfo", NpgsqlRestCfg, true),
    LogConnectionNoticeEvents = GetConfigBool("LogConnectionNoticeEvents", NpgsqlRestCfg, true),
    LogCommands = GetConfigBool("LogCommands", NpgsqlRestCfg),
    LogCommandParameters = GetConfigBool("LogCommandParameters", NpgsqlRestCfg),

    CommandTimeout = GetConfigInt("CommandTimeout", NpgsqlRestCfg),
    DefaultHttpMethod = GetConfigEnum<Method?>("DefaultHttpMethod", NpgsqlRestCfg),
    DefaultRequestParamType = GetConfigEnum<RequestParamType?>("DefaultRequestParamType", NpgsqlRestCfg),
    CommentsMode = GetConfigEnum<CommentsMode?>("CommentsMode", NpgsqlRestCfg) ?? CommentsMode.OnlyWithHttpTag,
    RequestHeadersMode = GetConfigEnum<RequestHeadersMode?>("RequestHeadersMode", NpgsqlRestCfg) ?? RequestHeadersMode.Ignore,
    RequestHeadersParameterName = GetConfigStr("RequestHeadersParameterName", NpgsqlRestCfg) ?? "_headers",

    EndpointCreated = CreateEndpointCreatedHandler(),
    ValidateParameters = CreateValidateParametersHandler(),
    ReturnNpgsqlExceptionMessage = GetConfigBool("ReturnNpgsqlExceptionMessage", NpgsqlRestCfg, true),
    PostgreSqlErrorCodeToHttpStatusCodeMapping = CreatePostgreSqlErrorCodeToHttpStatusCodeMapping(),
    BeforeConnectionOpen = BeforeConnectionOpen(connectionString),

    AuthenticationOptions = new()
    {
        DefaultAuthenticationType = GetConfigStr("DefaultAuthenticationType", AuthCfg)
    },

    EndpointCreateHandlers = CreateCodeGenHandlers(connectionString),
    CustomRequestHeaders = GetCustomHeaders(),

    RoutineSources = CreateRoutineSources()
};

app.UseNpgsqlRest(options);
ExternalAuth.Configure(app, options);
app.Run();
