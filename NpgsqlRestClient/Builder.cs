using System.Data.Common;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Npgsql;
using Serilog;

using static NpgsqlRestClient.Config;

namespace NpgsqlRestClient;

public static class Builder
{
    public static WebApplicationBuilder Instance { get; private set; }  = default!;

    public static bool LogToConsole { get; private set; } = false;
    public static bool LogToFile { get; private set; } = false;
    public static Serilog.ILogger? Logger { get; private set; } = null;
    public static bool UseHttpsRedirection { get; private set; } = false;
    public static bool UseHsts { get; private set; } = false;

    public static void BuildInstance()
    {
        var staticFilesCfg = Cfg.GetSection("StaticFiles");
        string? webRootPath = staticFilesCfg is not null && GetConfigBool("Enabled", staticFilesCfg) is true ? GetConfigStr("RootPath", staticFilesCfg) : null;

        var options = new WebApplicationOptions()
        {
            ApplicationName = GetConfigStr("ApplicationName") ?? Path.GetFileName(Environment.CurrentDirectory),
            WebRootPath = webRootPath,
            EnvironmentName = GetConfigStr("EnvironmentName") ?? "Production",
        };
        Instance = WebApplication.CreateEmptyBuilder(options);
        Instance.WebHost.UseKestrelCore();

        var kestrelConfig = Cfg.GetSection("Kestrel");
        Instance.WebHost.ConfigureKestrel((context, options) =>
        {
            options.Configure(Cfg.GetSection("Kestrel"));
        });

        var urls = GetConfigStr("Urls");
        if (urls is not null)
        {
            Instance.WebHost.UseUrls(urls.Split(';'));
        }

        var ssqlCfg = Cfg.GetSection("Ssl");
        if (ssqlCfg.Exists() is true && GetConfigBool("Enabled", ssqlCfg) is true)
        {
            Instance.WebHost.UseKestrelHttpsConfiguration();
            UseHttpsRedirection = GetConfigBool("UseHttpsRedirection", ssqlCfg);
            UseHsts = GetConfigBool("UseHsts", ssqlCfg);
        }
    }

    public static WebApplication Build() => Instance.Build();

    public static void BuildLogger()
    {
        var logCfg = Cfg.GetSection("Log");
        if (logCfg.Exists() is false)
        {
            LogToConsole = false;
            LogToFile = false;
            return;
        }

        Logger = null;
        LogToConsole = GetConfigBool("ToConsole", logCfg);
        LogToFile = GetConfigBool("ToFile", logCfg);
        var filePath = GetConfigStr("FilePath", logCfg);

        if (LogToConsole is true || LogToFile is true)
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
            if (LogToConsole is true)
            {
                loggerConfig = loggerConfig.WriteTo.Console(
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                    outputTemplate: outputTemplate,
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code);
            }
            if (LogToFile is true)
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
            Logger = serilog.ForContext<Program>();
            Instance.Host.UseSerilog(serilog);
            Logger?.Information("----> Starting with configuration(s): {0}", Cfg.Providers);
        }
    }

    public static void BuildAuthentication()
    {
        var authCfg = Cfg.GetSection("Auth");
        if (authCfg.Exists() is false)
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
            var auth = Instance.Services.AddAuthentication(defaultScheme);

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
                Logger?.Information("Using Cookie Authentication with scheme {0}. Cookie expires in {1} days.", cookieScheme, days);
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
                Logger?.Information("Using Bearer Token Authentication with scheme {0}. Token expires in {1} hours and refresh token expires in {2} days.", tokenScheme, hours, days);
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

            if (cookieAuth || bearerTokenAuth)
            {
                ExternalAuthConfig.Build(authCfg);
            }
        }
    }

    public static void BuildCors()
    {
        var corsCfg = Cfg.GetSection("Cors");
        if (corsCfg.Exists() is false || GetConfigBool("Enabled", corsCfg) is false)
        {
            return;
        }

        var allowedOrigins = GetConfigEnumerable("AllowedOrigins", corsCfg)?.ToArray() ?? [];
        var allowedMethods = GetConfigEnumerable("AllowedMethods", corsCfg)?.ToArray() ?? [];
        var allowedHeaders = GetConfigEnumerable("AllowedHeaders", corsCfg)?.ToArray() ?? [];

        Instance.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Contains("*"))
            {
                policy.AllowAnyOrigin();
                Logger?.Information("Allowed any origins.");
            }
            else
            {
                policy.WithOrigins(allowedOrigins);
                Logger?.Information("Allowed origins: {0}", allowedOrigins);
            }

            if (allowedMethods.Contains("*"))
            {
                policy.AllowAnyMethod();
                Logger?.Information("Allowed any methods.");
            }
            else
            {
                policy.WithMethods(allowedMethods);
                Logger?.Information("Allowed methods: {0}", allowedMethods);
            }

            if (allowedHeaders.Contains("*"))
            {
                policy.AllowAnyHeader();
                Logger?.Information("Allowed any headers.");
            }
            else
            {
                policy.WithHeaders(allowedHeaders);
                Logger?.Information("Allowed headers: {0}", allowedHeaders);
            }

            policy.AllowCredentials();
        }));
    }

    public static string? BuildConnectionString()
    {
        string? connectionString;
        string? connectionName = GetConfigStr("ConnectionName", NpgsqlRestCfg);
        if (connectionName is not null)
        {
            connectionString = Cfg.GetConnectionString(connectionName);
        }
        else
        {
            var section = Cfg.GetSection("ConnectionStrings");
            connectionString = section.GetChildren().FirstOrDefault()?.Value;
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        if (GetConfigBool("SetApplicationNameInConnection", NpgsqlRestCfg) is true)
        {
            connectionStringBuilder.ApplicationName = Instance.Environment.ApplicationName;
        }

        if (GetConfigBool("UseEnvironmentConnection", NpgsqlRestCfg) is true)
        {
            connectionStringBuilder.Host ??= Environment.GetEnvironmentVariable("PGHOST") ?? Environment.GetEnvironmentVariable("PGHOSTADDR");
            if (int.TryParse(Environment.GetEnvironmentVariable("PGPORT"), out int port) is true)
            {
                connectionStringBuilder.Port = port;
            }
            connectionStringBuilder.Database ??= Environment.GetEnvironmentVariable("PGDATABASE");
        }

        connectionString = connectionStringBuilder.ConnectionString;
        connectionStringBuilder.Remove("password");
        Logger?.Information(messageTemplate: "Using connection: {0}", connectionStringBuilder.ConnectionString);

        return connectionString;
    }

    private static string? _instanceId = null;

    public static string InstanceId
    {
        get
        {
            _instanceId ??= Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return _instanceId;
        }
    }

    public static Dictionary<string, StringValues> GetCustomHeaders()
    {
        var result = new Dictionary<string, StringValues>();
        foreach(var section in NpgsqlRestCfg.GetSection("CustomRequestHeaders").GetChildren())
        {
            result.Add(section.Key, section.Value);
        }

        var instIdName = GetConfigStr("InstanceIdRequestHeaderName", NpgsqlRestCfg);
        if (string.IsNullOrEmpty(instIdName) is false)
        {
            result.Add(instIdName, InstanceId);
        }
        return result;
    }
}
