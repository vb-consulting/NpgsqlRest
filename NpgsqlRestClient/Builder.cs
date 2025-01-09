﻿using System.Data.Common;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
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
    public static BearerTokenConfig? BearerTokenConfig { get; private set; } = null;

    public static void BuildInstance()
    {
        var staticFilesCfg = Cfg.GetSection("StaticFiles");
        string? webRootPath = staticFilesCfg is not null && GetConfigBool("Enabled", staticFilesCfg) is true ? GetConfigStr("RootPath", staticFilesCfg) ?? "wwwroot" : null;

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
        else
        {
           Instance.WebHost.UseUrls("http://localhost:5000", "http://localhost:5001");
        }

        var ssqlCfg = Cfg.GetSection("Ssl");
        if (ssqlCfg.Exists() is true)
        {
            if (GetConfigBool("Enabled", ssqlCfg) is true)
            {
                Instance.WebHost.UseKestrelHttpsConfiguration();
                UseHttpsRedirection = GetConfigBool("UseHttpsRedirection", ssqlCfg, true);
                UseHsts = GetConfigBool("UseHsts", ssqlCfg, true);
            }
        }
        else
        {
            UseHttpsRedirection = false;
            UseHsts = false;
        }
    }

    public static WebApplication Build() => Instance.Build();

    public static void BuildLogger()
    {
        var logCfg = Cfg.GetSection("Log");
        Logger = null;
        LogToConsole = GetConfigBool("ToConsole", logCfg, true);
        LogToFile = GetConfigBool("ToFile", logCfg);
        var filePath = GetConfigStr("FilePath", logCfg) ?? "logs/log.txt";

        if (LogToConsole is true || LogToFile is true)
        {
            var loggerConfig = new LoggerConfiguration().MinimumLevel.Verbose();
            bool systemAdded = false;
            bool microsoftAdded = false;
            foreach (var level in logCfg?.GetSection("MinimalLevels")?.GetChildren() ?? [])
            {
                var key = level.Key;
                var value = GetEnum<Serilog.Events.LogEventLevel?>(level.Value);
                if (value is not null && key is not null)
                {
                    loggerConfig.MinimumLevel.Override(key, value.Value);
                    if (string.Equals(key, "System", StringComparison.OrdinalIgnoreCase))
                    {
                        systemAdded = true;
                    }
                    if (string.Equals(key, "Microsoft", StringComparison.OrdinalIgnoreCase))
                    {
                        microsoftAdded = true;
                    }
                }
            }
            if (systemAdded is false)
            {
                loggerConfig.MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning);
            }
            if (microsoftAdded is false)
            {
                loggerConfig.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning);
            }

            string outputTemplate = GetConfigStr("OutputTemplate", logCfg) ?? 
                "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj} [{SourceContext}]{NewLine}{Exception}";
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
        bool cookieAuth = false;
        bool bearerTokenAuth = false;
        if (authCfg.Exists() is true)
        {
            cookieAuth = GetConfigBool("CookieAuth", authCfg);
            bearerTokenAuth = GetConfigBool("BearerTokenAuth", authCfg);

        }

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
                    options.Cookie.MaxAge = GetConfigBool("CookieMultiSessions", authCfg, true) is true ? TimeSpan.FromDays(days) : null;
                    options.Cookie.HttpOnly = GetConfigBool("CookieHttpOnly", authCfg, true) is true;
                });
                Logger?.Information("Using Cookie Authentication with scheme {0}. Cookie expires in {1} days.", cookieScheme, days);
            }
            if (bearerTokenAuth is true)
            {
                var hours = GetConfigInt("BearerTokenExpireHours", authCfg) ?? 1;
                BearerTokenConfig = new()
                {
                    Scheme = tokenScheme,
                    RefreshPath = GetConfigStr("BearerTokenRefreshPath", authCfg)
                };
                auth.AddBearerToken(tokenScheme, options =>
                {
                    options.BearerTokenExpiration = TimeSpan.FromHours(hours);
                    options.RefreshTokenExpiration = TimeSpan.FromHours(hours);
                    options.Validate();
                });
                Logger?.Information(
                    "Using Bearer Token Authentication with scheme {0}. Token expires in {1} hours. Refresh path is {2}", 
                    tokenScheme, 
                    hours, 
                    BearerTokenConfig.RefreshPath);
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

    public static bool BuildCors()
    {
        var corsCfg = Cfg.GetSection("Cors");
        if (corsCfg.Exists() is false || GetConfigBool("Enabled", corsCfg) is false)
        {
            return false;
        }

        var allowedOrigins = GetConfigEnumerable("AllowedOrigins", corsCfg)?.ToArray() ?? ["*"];
        var allowedMethods = GetConfigEnumerable("AllowedMethods", corsCfg)?.ToArray() ?? ["*"];
        var allowedHeaders = GetConfigEnumerable("AllowedHeaders", corsCfg)?.ToArray() ?? ["*"];

        Instance.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder builder = policy;
            if (allowedOrigins.Contains("*"))
            {
                builder = builder.AllowAnyOrigin();
                Logger?.Information("Allowed any origins.");
            }
            else
            {
                builder = builder.WithOrigins(allowedOrigins);
                Logger?.Information("Allowed origins: {0}", allowedOrigins);
            }

            if (allowedMethods.Contains("*"))
            {
                builder = builder.AllowAnyMethod();
                Logger?.Information("Allowed any methods.");
            }
            else
            {
                builder = builder.WithMethods(allowedMethods);
                Logger?.Information("Allowed methods: {0}", allowedMethods);
            }

            if (allowedHeaders.Contains("*"))
            {
                builder = builder.AllowAnyHeader();
                Logger?.Information("Allowed any headers.");
            }
            else
            {
                builder = builder.WithHeaders(allowedHeaders);
                Logger?.Information("Allowed headers: {0}", allowedHeaders);
            }

            builder.AllowCredentials();
        }));
        return true;
    }

    public static bool ConfigureResponseCompression()
    {
        var responseCompressionCfg = Cfg.GetSection("ResponseCompression");
        if (responseCompressionCfg.Exists() is false || GetConfigBool("Enabled", responseCompressionCfg) is false)
        {
            return false;
        }

        var useBrotli = GetConfigBool("UseBrotli", responseCompressionCfg, true);
        var useGzipFallback = GetConfigBool("UseGzipFallback", responseCompressionCfg, true);

        if (useBrotli is false && useGzipFallback is false)
        {
            return false;
        }

        Instance.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = GetConfigBool("EnableForHttps", responseCompressionCfg, false); 
            if (useBrotli is true)
            {
                options.Providers.Add<BrotliCompressionProvider>();
            }
            if (useGzipFallback is true)
            {
                options.Providers.Add<GzipCompressionProvider>();
            }
            options.MimeTypes = GetConfigEnumerable("IncludeMimeTypes", responseCompressionCfg)?.ToArray() ?? [
                "text/plain",
                "text/css",
                "application/javascript",
                "text/html",
                "application/xml",
                "text/xml",
                "application/json",
                "text/json",
                "image/svg+xml",
                "font/woff",
                "font/woff2",
                "application/font-woff",
                "application/font-woff2"];
            options.ExcludedMimeTypes = GetConfigEnumerable("ExcludeMimeTypes", responseCompressionCfg)?.ToArray() ?? [];
        });

        var level = GetConfigEnum<CompressionLevel>("CompressionLevel", responseCompressionCfg);
        if (useBrotli is true)
        {
            Instance.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = level;
            });
        }
        if (useGzipFallback is true)
        {
            Instance.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = level;
            });
        }

        return true;
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
        if (GetConfigBool("SetApplicationNameInConnection", ConnectionSettingsCfg, true) is true)
        {
            connectionStringBuilder.ApplicationName = Instance.Environment.ApplicationName;
        }

        if (GetConfigBool("UseEnvVars", ConnectionSettingsCfg) is true)
        {
            var envOverride = GetConfigBool("EnvVarsOverride", ConnectionSettingsCfg, false);

            var hostEnvVar = GetConfigStr("HostEnvVar", ConnectionSettingsCfg) ?? "PGHOST";
            if (string.IsNullOrEmpty(hostEnvVar) is false && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(hostEnvVar)) is false)
            {
                if (envOverride is true)
                {
                    connectionStringBuilder.Host = Environment.GetEnvironmentVariable(hostEnvVar);
                }
                else if (envOverride is false && string.IsNullOrEmpty(connectionStringBuilder.Host))
                {
                    connectionStringBuilder.Host = Environment.GetEnvironmentVariable(hostEnvVar);
                }
            }

            var portEnvVar = GetConfigStr("PortEnvVar", ConnectionSettingsCfg) ?? "PGPORT";
            if (string.IsNullOrEmpty(portEnvVar) is false && int.TryParse(Environment.GetEnvironmentVariable(portEnvVar), out int port) is true)
            {
                if (envOverride is true)
                {
                    connectionStringBuilder.Port = port;
                }
                else if (envOverride is false && connectionStringBuilder.Port != port)
                {
                    connectionStringBuilder.Port = port;
                }
            }
            var dbEnvVar = GetConfigStr("DatabaseEnvVar", ConnectionSettingsCfg) ?? "PGDATABASE";
            if (string.IsNullOrEmpty(dbEnvVar) is false && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(dbEnvVar)) is false)
            {
                if (envOverride is true)
                {
                    connectionStringBuilder.Database = Environment.GetEnvironmentVariable(dbEnvVar);
                }
                else if (envOverride is false && string.IsNullOrEmpty(connectionStringBuilder.Database))
                {
                    connectionStringBuilder.Database = Environment.GetEnvironmentVariable(dbEnvVar);
                }
            }
            var userEnvVar = GetConfigStr("UserEnvVar", ConnectionSettingsCfg) ?? "PGUSER";
            if (string.IsNullOrEmpty(userEnvVar) is false && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(userEnvVar)) is false)
            {
                if (envOverride is true)
                {
                    connectionStringBuilder.Username = Environment.GetEnvironmentVariable(userEnvVar);
                }
                else if (envOverride is false && string.IsNullOrEmpty(connectionStringBuilder.Username))
                {
                    connectionStringBuilder.Username = Environment.GetEnvironmentVariable(userEnvVar);
                }
            }
            var passEnvVar = GetConfigStr("PasswordEnvVar", ConnectionSettingsCfg) ?? "PGPASSWORD";
            if (string.IsNullOrEmpty(passEnvVar) is false && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(passEnvVar)) is false)
            {
                if (envOverride is true)
                {
                    connectionStringBuilder.Password = Environment.GetEnvironmentVariable(passEnvVar);
                }
                else if (envOverride is false && string.IsNullOrEmpty(connectionStringBuilder.Password))
                {
                    connectionStringBuilder.Password = Environment.GetEnvironmentVariable(passEnvVar);
                }
            }
        }

        // Connection doesn't participate in ambient TransactionScope
        connectionStringBuilder.Enlist = false;
        connectionStringBuilder.NoResetOnClose = true;

        connectionString = connectionStringBuilder.ConnectionString;
        connectionStringBuilder.Remove("password");
        Logger?.Information(messageTemplate: "Using connection: {0}", connectionStringBuilder.ConnectionString);

        // disable SQL rewriting to ensure that NpgsqlRest works with this option on.
        AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);

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
