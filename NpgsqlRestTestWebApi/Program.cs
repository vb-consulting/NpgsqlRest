// dotnet publish -r win-x64 -c Release
// dotnet publish -r linux-x64 -c Release
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Net.Http.Headers;
using Serilog;
using Npgsql;
using NpgsqlRest;
using NpgsqlRest.Defaults;
using NpgsqlRest.HttpFiles;
using NpgsqlRest.TsClient;
using NpgsqlRest.CrudSource;

if (args.Any(a => a == "-v" || a == "--version" || a == "-h" || a == "--help"))
{
    Console.WriteLine("Usages");
    Console.WriteLine("1: npgsqlrest-[os]");
    Console.WriteLine("2: npgsqlrest-[os] [path to one or more configuration file(s)]");
    Console.WriteLine("3: npgsqlrest-[os] [-v | --version | -h | --help]");
    Console.WriteLine();

    Console.WriteLine("Where");
    Console.WriteLine("npgsqlrest-[os]  is executable for the specific OS (like npgsqlrest-win64 or npgsqlrest-linux64)");
    Console.WriteLine("1:               run executable with default configuration files: appsettings.json (required) and appsettings.Development.json (optional).");
    Console.WriteLine("2:               run executable with optional configuration files from argument list.");
    Console.WriteLine("3:               show this screen.");

    Console.WriteLine();
    Console.WriteLine("Versions");
    Console.WriteLine("Build                {0}", System.Reflection.Assembly.GetAssembly(typeof(Program))?.GetName()?.Version?.ToString());
    Console.WriteLine("Npgsql               {0}", System.Reflection.Assembly.GetAssembly(typeof(NpgsqlRestOptions))?.GetName()?.Version?.ToString());
    Console.WriteLine("NpgsqlRest.HttpFiles {0}", System.Reflection.Assembly.GetAssembly(typeof(HttpFileOptions))?.GetName()?.Version?.ToString());
    Console.WriteLine("NpgsqlRest.TsClient  {0}", System.Reflection.Assembly.GetAssembly(typeof(TsClientOptions))?.GetName()?.Version?.ToString());
    Console.WriteLine();
    return;
}

Stopwatch sw = new();
sw.Start();

var config = BuildConfiguration(args);
var builder = CreateBuilder();
var logger = BuildLogger(out var logToConsole, out var logToFile);

logger?.Information("----> Starting with configuration(s): {0}", config.Providers);
BuildAuthentication();
BuildCors();

var npgsqlRestCfg = config.GetSection("NpgsqlRest");
var authCfg = npgsqlRestCfg.GetSection("AuthenticationOptions");

var connectionString = GetConnectionString();
var app = builder.Build();

ConfigureApp();
ConfigureStaticFiles();
List<IEndpointCreateHandler> handlers = CreateCodeGenHandlers();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    ConnectionFromServiceProvider = false,

    SchemaSimilarTo = GetConfigStr("SchemaSimilarTo", npgsqlRestCfg),
    SchemaNotSimilarTo = GetConfigStr("SchemaNotSimilarTo", npgsqlRestCfg),
    IncludeSchemas = GetConfigEnumerable("IncludeSchemas", npgsqlRestCfg)?.ToArray(),
    ExcludeSchemas = GetConfigEnumerable("ExcludeSchemas", npgsqlRestCfg)?.ToArray(),
    NameSimilarTo = GetConfigStr("NameSimilarTo", npgsqlRestCfg),
    NameNotSimilarTo = GetConfigStr("NameNotSimilarTo", npgsqlRestCfg),
    IncludeNames = GetConfigEnumerable("IncludeNames", npgsqlRestCfg)?.ToArray(),
    ExcludeNames = GetConfigEnumerable("ExcludeNames", npgsqlRestCfg)?.ToArray(),
    UrlPathPrefix = GetConfigStr("UrlPathPrefix", npgsqlRestCfg),
    UrlPathBuilder = GetConfigBool("KebabCaseUrls", npgsqlRestCfg) ? DefaultUrlBuilder.CreateUrl : CreateUrl,
    NameConverter = GetConfigBool("CamelCaseNames", npgsqlRestCfg) ? DefaultNameConverter.ConvertToCamelCase : n => n?.Trim('"'),
    RequiresAuthorization = GetConfigBool("RequiresAuthorization", npgsqlRestCfg),

    LogEndpointCreatedInfo = GetConfigBool("LogEndpointCreatedInfo", npgsqlRestCfg),
    LogAnnotationSetInfo = GetConfigBool("LogEndpointCreatedInfo", npgsqlRestCfg),
    LogConnectionNoticeEvents = GetConfigBool("LogConnectionNoticeEvents", npgsqlRestCfg),
    LogCommands = GetConfigBool("LogCommands", npgsqlRestCfg),
    LogCommandParameters = GetConfigBool("LogCommandParameters", npgsqlRestCfg),

    CommandTimeout = GetConfigInt("CommandTimeout", npgsqlRestCfg),
    DefaultHttpMethod = GetConfigEnum<Method?>("DefaultHttpMethod", npgsqlRestCfg),
    DefaultRequestParamType = GetConfigEnum<RequestParamType?>("DefaultRequestParamType", npgsqlRestCfg),
    CommentsMode = GetConfigEnum<CommentsMode>("CommentsMode", npgsqlRestCfg),
    RequestHeadersMode = GetConfigEnum<RequestHeadersMode>("RequestHeadersMode", npgsqlRestCfg),
    RequestHeadersParameterName = GetConfigStr("RequestHeadersParameterName", npgsqlRestCfg) ?? "headers",

    EndpointCreated = CreateEndpointCreatedHandler(),
    ValidateParameters = CreateValidateParametersHandler(),
    ReturnNpgsqlExceptionMessage = GetConfigBool("ReturnNpgsqlExceptionMessage", npgsqlRestCfg, true),
    PostgreSqlErrorCodeToHttpStatusCodeMapping = CreatePostgreSqlErrorCodeToHttpStatusCodeMapping(),
    BeforeConnectionOpen = BeforeConnectionOpen(),

    AuthenticationOptions = new()
    {
        DefaultAuthenticationType = GetConfigStr("DefaultAuthenticationType", authCfg)
    },

    EndpointCreateHandlers = handlers,
    SourcesCreated = SourcesCreated
});

app.Run();
return;

static IConfigurationRoot BuildConfiguration(string[] args)
{
    var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
    IConfigurationRoot config;
    if (args.Length > 0)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith('-') is false)
            {
                configBuilder.AddJsonFile(arg, optional: false);
            }
        }
        config = configBuilder.Build();
    }
    else
    {
        config = configBuilder
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
    }

    return config;
}

WebApplicationBuilder CreateBuilder()
{
    var staticFilesCfg = config.GetSection("StaticFiles");
    string? webRootPath = staticFilesCfg is not null && GetConfigBool("Enabled", staticFilesCfg) is true ? GetConfigStr("RootPath", staticFilesCfg) : null;
    var builder = WebApplication.CreateEmptyBuilder(new()
    {
        ApplicationName = GetConfigStr("ApplicationName"),
        WebRootPath = webRootPath,
        EnvironmentName = GetConfigStr("EnvironmentName") ?? "Production",
    });
    builder.WebHost.UseKestrelCore();
    builder.WebHost.UseUrls(GetConfigStr("Urls")?.Split(';') ?? ["http://localhost:5001"]);
    return builder;
}

Serilog.ILogger? BuildLogger(out bool logToConsole, out bool logToFile)
{
    var logCfg = config.GetSection("Log");
    if (logCfg is null)
    {
        logToConsole = false;
        logToFile = false;
        return null;
    }
    Serilog.ILogger? logger = null;
    logToConsole = GetConfigBool("ToConsole", logCfg);
    logToFile = GetConfigBool("ToFile", logCfg);
    var filePath = GetConfigStr("FilePath", logCfg);

    if (logToConsole is true || logToFile is true)
    {
        var loggerConfig = new LoggerConfiguration().MinimumLevel.Verbose();
        foreach (var level in logCfg.GetSection("MinimalLevels").GetChildren())
        {
            var key = level.Key;
            var value = GetEnum<Serilog.Events.LogEventLevel?>(level.Value);
            if (value is not null && key is not null)
            {
                loggerConfig.MinimumLevel.Override(key, value.Value);
            }
        }
        string outputTemplate = GetConfigStr("OutputTemplate", logCfg) ?? "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj} [{SourceContext}]{NewLine}{Exception}";
        if (logToConsole is true)
        {
            loggerConfig = loggerConfig.WriteTo.Console(
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                outputTemplate: outputTemplate,
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code);
        }
        if (logToFile is true)
        {
            loggerConfig = loggerConfig.WriteTo.File(
                path: filePath ?? "logs/log.txt",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: GetConfigInt("FileSizeLimitBytes", logCfg) ?? 30000000,
                retainedFileCountLimit: GetConfigInt("RetainedFileCountLimit", logCfg) ?? 30,
                rollOnFileSizeLimit: GetConfigBool("RollOnFileSizeLimit", logCfg, defaultVal: true),
                outputTemplate: outputTemplate);
        }
        var serilog = loggerConfig.CreateLogger();
        logger = serilog.ForContext<Program>();
        builder.Host.UseSerilog(serilog);
    }

    return logger;
}

void BuildAuthentication()
{
    var authCfg = config.GetSection("Auth");
    if (authCfg is null)
    {
        return;
    }
    var cookieAuth = GetConfigBool("CookieAuth", authCfg);
    var bearerTokenAuth = GetConfigBool("BearerTokenAuth", authCfg);
    if (cookieAuth is true || bearerTokenAuth is true)
    {
        var cookieScheme = GetConfigStr("CookieAuthScheme", authCfg) ?? CookieAuthenticationDefaults.AuthenticationScheme;
        var tokenScheme = GetConfigStr("BearerTokenAuthScheme", authCfg) ?? BearerTokenDefaults.AuthenticationScheme;
        string defaultScheme = (cookieAuth, bearerTokenAuth) switch
        {
            (true, true) => string.Concat(cookieScheme, "_and_", tokenScheme),
            (true, false) => cookieScheme,
            (false, true) => tokenScheme,
            _ => throw new NotImplementedException(),
        };
        var auth = builder.Services.AddAuthentication(defaultScheme);

        if (cookieAuth is true)
        {
            var days = GetConfigInt("CookieExpireDays", authCfg) ?? 14;
            auth.AddCookie(cookieScheme, options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(days);
                var name = GetConfigStr("CookieName", authCfg);
                if (string.IsNullOrEmpty(name) is false)
                {
                    options.Cookie.Name = GetConfigStr("CookieName", authCfg);
                }
                options.Cookie.Path = GetConfigStr("CookiePath", authCfg);
                options.Cookie.Domain = GetConfigStr("CookieDomain", authCfg);
                options.Cookie.MaxAge = GetConfigBool("CookieMultiSessions", authCfg) is true ? TimeSpan.FromDays(days) : null;
                options.Cookie.HttpOnly = GetConfigBool("CookieHttpOnly", authCfg) is true;
            });
            logger?.Information("Using Cookie Authentication with scheme {0}. Cookie expires in {1} days.", cookieScheme, days);
        }
        if (bearerTokenAuth is true)
        {
            var hours = GetConfigInt("BearerTokenExpireHours", authCfg) ?? 1;
            var days = GetConfigInt("BearerRefreshTokenExpireDays", authCfg) ?? 14;
            auth.AddBearerToken(tokenScheme, options =>
            {
                options.BearerTokenExpiration = TimeSpan.FromHours(hours);
                options.RefreshTokenExpiration = TimeSpan.FromDays(days);
            });
            logger?.Information("Using Bearer Token Authentication with scheme {0}. Token expires in {1} hours and refresh token expires in {2} days.", tokenScheme, hours, days);
        }
        if (cookieAuth is true && bearerTokenAuth is true)
        {
            auth.AddPolicyScheme(defaultScheme, defaultScheme, options =>
            {
                // runs on each request
                options.ForwardDefaultSelector = context =>
                {
                    // filter by auth type
                    string? authorization = context.Request.Headers[HeaderNames.Authorization];
                    if (string.IsNullOrEmpty(authorization) is false && authorization.StartsWith("Bearer "))
                    {
                        return tokenScheme;
                    }
                    // otherwise always check for cookie auth
                    return cookieScheme;
                };
            });
        }
    }
}

void BuildCors()
{
    var corsCfg = config.GetSection("Cors");
    if (corsCfg is null || GetConfigBool("Enabled", corsCfg) is false)
    {
        return;
    }

    var allowedOrigins = GetConfigEnumerable("AllowedOrigins", corsCfg)?.ToArray() ?? [];
    var allowedMethods = GetConfigEnumerable("AllowedMethods", corsCfg)?.ToArray() ?? [];
    var allowedHeaders = GetConfigEnumerable("AllowedHeaders", corsCfg)?.ToArray() ?? [];

    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin();
            logger?.Information("Allowed any origins.");
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
            logger?.Information("Allowed origins: {0}", allowedOrigins);
        }

        if (allowedMethods.Contains("*"))
        {
            policy.AllowAnyMethod();
            logger?.Information("Allowed any methods.");
        }
        else
        {
            policy.WithMethods(allowedMethods);
            logger?.Information("Allowed methods: {0}", allowedMethods);
        }

        if (allowedHeaders.Contains("*"))
        {
            policy.AllowAnyHeader();
            logger?.Information("Allowed any headers.");
        }
        else
        {
            policy.WithHeaders(allowedHeaders);
            logger?.Information("Allowed headers: {0}", allowedHeaders);
        }

        policy.AllowCredentials();
    }));
}

void ConfigureApp()
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        sw.Stop();
        logger?.Information("Started in {0}", sw);
        logger?.Information("Listening on {0}", app.Urls);
    });

    if (logToConsole is true || logToFile is true)
    {
        app.UseSerilogRequestLogging();
    }
}

void ConfigureStaticFiles()
{
    var staticFilesCfg = config.GetSection("StaticFiles");
    if (staticFilesCfg is null || GetConfigBool("Enabled", staticFilesCfg) is false)
    {
        return;
    }

    var redirect = GetConfigStr("LoginRedirectPath", staticFilesCfg);
    var anonPaths = GetConfigEnumerable("AnonymousPaths", staticFilesCfg);
    HashSet<string>? anonPathsHash = anonPaths is null ? null : new(GetConfigEnumerable("AnonymousPaths", staticFilesCfg) ?? []);

    app.UseDefaultFiles();
    if (anonPathsHash?.Contains("*") is true)
    {
        app.UseStaticFiles();
    }
    else
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (anonPathsHash is not null && ctx?.Context?.User?.Identity?.IsAuthenticated is false)
                {
                    var path = ctx.Context.Request.Path.Value?[..^ctx.File.Name.Length] ?? "/";
                    if (anonPathsHash.Contains(path) is false)
                    {
                        logger?.Information("Unauthorized access to {0}", path);
                        ctx.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        if (redirect is not null)
                        {
                            ctx.Context.Response.Redirect(redirect);
                        }
                    }
                }
            }
        });
    }
    logger?.Information("Serving static files from {0}", app.Environment.WebRootPath);
}

string? GetConnectionString()
{
    string? connectionString;
    string? connectionName = GetConfigStr("ConnectionName", npgsqlRestCfg);
    if (connectionName is not null)
    {
        connectionString = config?.GetConnectionString(connectionName);
    }
    else
    {
        var section = config.GetSection("ConnectionStrings");
        connectionString = section.GetChildren().FirstOrDefault()?.Value;
    }
    if (connectionString is null)
    {
        logger?.Fatal("Connection string could not be initialized!");
        return null;
    }

    var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    if (GetConfigBool("SetApplicationNameInConnection", npgsqlRestCfg) is true)
    {
        connectionStringBuilder.ApplicationName = builder.Environment.ApplicationName;
    }

    connectionString = connectionStringBuilder.ConnectionString;
    connectionStringBuilder.Remove("password");
    logger?.Information(messageTemplate: "Using connection: {0}", connectionStringBuilder.ConnectionString);

    return connectionString;
}

Action<NpgsqlConnection, Routine, RoutineEndpoint, HttpContext>? BeforeConnectionOpen()
{
    var useConnectionApplicationNameWithUsername = GetConfigBool("UseJsonApplicationName", npgsqlRestCfg);
    if (useConnectionApplicationNameWithUsername is false)
    {
        return null;
    }

    return (NpgsqlConnection connection, Routine routine, RoutineEndpoint endpoint, HttpContext context) =>
    {
        var uid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var executionId = context.Request.Headers["X-Execution-Id"].FirstOrDefault();
        connection.ConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = string.Concat("{\"app\":\"", builder.Environment.ApplicationName,
                    "\",\"uid\":", uid is null ? "null" : string.Concat("\"", uid, "\""),
                    ",\"id\":", executionId is null ? "null" : string.Concat("\"", executionId, "\""),
                    "}")
        }.ConnectionString;
    };
}

Func<Routine, RoutineEndpoint, RoutineEndpoint?>? CreateEndpointCreatedHandler()
{
    var loginPath = GetConfigStr("LoginPath", authCfg);
    var logoutPath = GetConfigStr("LogoutPath", authCfg);
    if (loginPath is null && logoutPath is null)
    {
        return null;
    }
    return (Routine routine, RoutineEndpoint endpoint) =>
    {
        if (loginPath is not null && string.Equals(endpoint.Url, loginPath, StringComparison.OrdinalIgnoreCase))
        {
            return endpoint with { Login = true };
        }
        if (logoutPath is not null && string.Equals(routine.Name, logoutPath, StringComparison.OrdinalIgnoreCase))
        {
            return endpoint with { Logout = true };
        }
        return endpoint;
    };
}

Action<ParameterValidationValues>? CreateValidateParametersHandler()
{
    var userIdParameterName = GetConfigStr("UserIdParameterName", authCfg);
    var userNameParameterName = GetConfigStr("UserNameParameterName", authCfg);
    var userRolesParameterName = GetConfigStr("UserRolesParameterName", authCfg);

    if (userIdParameterName is null && userNameParameterName is null && userRolesParameterName is null)
    {
        return null;
    }
    return (ParameterValidationValues p) =>
    {
        if (userIdParameterName is not null && string.Equals(p.Parameter.ActualName, userIdParameterName, StringComparison.OrdinalIgnoreCase))
        {
            p.Parameter.Value = p.Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value as object ?? DBNull.Value;
        }
        else if (userNameParameterName is not null && string.Equals(p.Parameter.ActualName, userNameParameterName, StringComparison.OrdinalIgnoreCase))
        {
            p.Parameter.Value = p.Context.User.Identity?.Name as object ?? DBNull.Value;
        }
        else if (userRolesParameterName is not null && string.Equals(p.Parameter.ActualName, userRolesParameterName, StringComparison.OrdinalIgnoreCase))
        {
            p.Parameter.Value = p.Context.User.Claims.Where(c => c.Type == ClaimTypes.Role)?.Select(r => r.Value).ToArray() as object ?? DBNull.Value;
        }
    };
}

Dictionary<string, int> CreatePostgreSqlErrorCodeToHttpStatusCodeMapping()
{
    var config = npgsqlRestCfg.GetSection("PostgreSqlErrorCodeToHttpStatusCodeMapping");
    var result = new Dictionary<string, int>();
    foreach (var section in config.GetChildren())
    {
        if (int.TryParse(section.Value, out var value))
        {
            result.TryAdd(section.Key, value);
        }
    }
    return result;
}

List<IEndpointCreateHandler> CreateCodeGenHandlers()
{
    List<IEndpointCreateHandler> handlers = new(2);
    var httpFilecfg = npgsqlRestCfg.GetSection("HttpFileOptions");
    if (httpFilecfg is not null && GetConfigBool("Enabled", httpFilecfg) is true)
    {
        handlers.Add(new HttpFile(new HttpFileOptions
        {
            Name = GetConfigStr("Name", httpFilecfg),
            Option = GetConfigEnum<HttpFileOption>("Option", httpFilecfg),
            NamePattern = GetConfigStr("NamePattern", httpFilecfg) ?? "{0}{1}",
            CommentHeader = GetConfigEnum<CommentHeader>("CommentHeader", httpFilecfg),
            CommentHeaderIncludeComments = GetConfigBool("CommentHeaderIncludeComments", httpFilecfg),
            FileMode = GetConfigEnum<HttpFileMode>("FileMode", httpFilecfg),
            FileOverwrite = GetConfigBool("FileOverwrite", httpFilecfg),
            ConnectionString = connectionString
        }));
    }
    var tsClientCfg = npgsqlRestCfg.GetSection("TsClient");
    if (tsClientCfg is not null && GetConfigBool("Enabled", tsClientCfg) is true)
    {
        handlers.Add(new TsClient(new TsClientOptions
        {
            FilePath = GetConfigStr("FilePath", tsClientCfg),
            FileOverwrite = GetConfigBool("FileOverwrite", tsClientCfg),
            IncludeHost = GetConfigBool("IncludeHost", tsClientCfg),
            CustomHost = GetConfigStr("CustomHost", tsClientCfg),
            CommentHeader = GetConfigEnum<CommentHeader>("CommentHeader", tsClientCfg),
            CommentHeaderIncludeComments = GetConfigBool("CommentHeaderIncludeComments", tsClientCfg),
            BySchema = GetConfigBool("BySchema", tsClientCfg),
            IncludeStatusCode = GetConfigBool("IncludeStatusCode", tsClientCfg),
            CreateSeparateTypeFile = GetConfigBool("CreateSeparateTypeFile", tsClientCfg),
            ImportBaseUrlFrom = GetConfigStr("ImportBaseUrlFrom", tsClientCfg),
            ImportParseQueryFrom = GetConfigStr("ImportParseQueryFrom", tsClientCfg),
            IncludeParseUrlParam = GetConfigBool("IncludeParseUrlParam", tsClientCfg),
            IncludeParseRequestParam = GetConfigBool("IncludeParseRequestParam", tsClientCfg),
        }));
    }

    return handlers;
}

static string CreateUrl(Routine routine, NpgsqlRestOptions options) =>
    string.Concat(
        string.IsNullOrEmpty(options.UrlPathPrefix) ? "/" : string.Concat("/", options.UrlPathPrefix.Trim('/')),
        routine.Schema == "public" ? "" : routine.Schema.Trim('"').Trim('/'),
        "/",
        routine.Name.Trim('"').Trim('/'),
        "/");

void SourcesCreated(List<IRoutineSource> sources)
{
    var routineCfg = npgsqlRestCfg.GetSection("RoutinesSource");
    if (routineCfg is null || GetConfigBool("Enabled", routineCfg) is false)
    {
        sources.Clear();
    }
    else
    {
        sources[0].SchemaSimilarTo = GetConfigStr("SchemaSimilarTo", routineCfg);
        sources[0].SchemaNotSimilarTo = GetConfigStr("SchemaNotSimilarTo", routineCfg);
        sources[0].IncludeSchemas = GetConfigEnumerable("IncludeSchemas", routineCfg)?.ToArray();
        sources[0].ExcludeSchemas = GetConfigEnumerable("ExcludeSchemas", routineCfg)?.ToArray();
        sources[0].NameSimilarTo = GetConfigStr("NameSimilarTo", routineCfg);
        sources[0].NameNotSimilarTo = GetConfigStr("NameNotSimilarTo", routineCfg);
        sources[0].IncludeNames = GetConfigEnumerable("IncludeNames", routineCfg)?.ToArray();
        sources[0].ExcludeNames = GetConfigEnumerable("ExcludeNames", routineCfg)?.ToArray();
        sources[0].Query = GetConfigStr("Query", routineCfg);
        sources[0].CommentsMode = GetConfigEnum<CommentsMode?>("CommentsMode", routineCfg);
    }

    var crudSourceCfg = npgsqlRestCfg.GetSection("CrudSource");
    if (crudSourceCfg is null || GetConfigBool("Enabled", crudSourceCfg) is false)
    {
        return;
    }
    sources.Add(new CrudSource()
    {
        SchemaSimilarTo = GetConfigStr("SchemaSimilarTo", crudSourceCfg),
        SchemaNotSimilarTo = GetConfigStr("SchemaNotSimilarTo", crudSourceCfg),
        IncludeSchemas = GetConfigEnumerable("IncludeSchemas", crudSourceCfg)?.ToArray(),
        ExcludeSchemas = GetConfigEnumerable("ExcludeSchemas", crudSourceCfg)?.ToArray(),
        NameSimilarTo = GetConfigStr("NameSimilarTo", crudSourceCfg),
        NameNotSimilarTo = GetConfigStr("NameNotSimilarTo", crudSourceCfg),
        IncludeNames = GetConfigEnumerable("IncludeNames", crudSourceCfg)?.ToArray(),
        ExcludeNames = GetConfigEnumerable("ExcludeNames", crudSourceCfg)?.ToArray(),
        Query = GetConfigStr("Query", crudSourceCfg),
        CommentsMode = GetConfigEnum<CommentsMode?>("CommentsMode", crudSourceCfg),
        CrudTypes = GetConfigFlag<CrudCommandType>("CrudTypes", crudSourceCfg),
    });
}

bool GetConfigBool(string key, IConfiguration? subsection = null, bool defaultVal = false)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    if (string.IsNullOrEmpty(section?.Value))
    {
        return defaultVal;
    }
    return string.Equals(section?.Value, "true", StringComparison.OrdinalIgnoreCase);
}

string? GetConfigStr(string key, IConfiguration? subsection = null)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    return string.IsNullOrEmpty(section?.Value) ? null : section.Value;
}

int? GetConfigInt(string key, IConfiguration? subsection = null)
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

T? GetConfigEnum<T>(string key, IConfiguration? subsection = null)
{
    var section = subsection?.GetSection(key) ?? config?.GetSection(key);
    if (string.IsNullOrEmpty(section?.Value))
    {
        return default;
    }
    return GetEnum<T>(section?.Value);
}

static T? GetEnum<T>(string? value)
{
    if (value is null)
    {
        return default;
    }
    var type = typeof(T);
    var nullable = Nullable.GetUnderlyingType(type);
    var names = Enum.GetNames(nullable ?? type);
    foreach (var name in names)
    {
        if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
        {
            return (T)Enum.Parse(nullable ?? type, name);
        }
    }
    return default;
}

IEnumerable<string>? GetConfigEnumerable(string key, IConfiguration? subsection = null)
{
    var section = subsection is not null ? subsection?.GetSection(key) : config?.GetSection(key);
    var children = section?.GetChildren().ToArray();
    if (children is null || (children.Length == 0 && section?.Value == ""))
    {
        return null;
    }
    return children.Select(c => c.Value ?? "");
}

T? GetConfigFlag<T>(string key, IConfiguration? subsection = null)
{
    var array = GetConfigEnumerable(key, subsection)?.ToArray();
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