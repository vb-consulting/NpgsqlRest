﻿using System.Net;
using System.Security.Claims;
using Npgsql;
using NpgsqlRest;
using NpgsqlRest.HttpFiles;
using NpgsqlRest.TsClient;
using Serilog;

using static NpgsqlRestClient.Config;
using static NpgsqlRestClient.Builder;
using Microsoft.Extensions.Primitives;

namespace NpgsqlRestClient;

public static class App
{
    public static void Configure(WebApplication app, Action started)
    {
        app.Lifetime.ApplicationStarted.Register(started);
        if (UseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }
        if (UseHsts)
        {
            app.UseHsts();
        }

        if (LogToConsole is true || LogToFile is true)
        {
            app.UseSerilogRequestLogging();
        }
        
        var cfgCfg = Cfg.GetSection("Config");
        var configEndpoint = GetConfigStr("ExposeAsEndpoint", cfgCfg);
        if (configEndpoint is not null)
        {
            app.Use(async (context, next) =>
            {
                if (
                    string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(context.Request.Path, configEndpoint, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(Serialize());
                    await context.Response.CompleteAsync();
                    return;
                }
                await next(context);
            });
        }
    }

    public static void ConfigureStaticFiles(WebApplication app)
    {
        var staticFilesCfg = Cfg.GetSection("StaticFiles");
        if (staticFilesCfg.Exists() is false || GetConfigBool("Enabled", staticFilesCfg) is false)
        {
            return;
        }

        var redirect = GetConfigStr("LoginRedirectPath", staticFilesCfg) ?? "/login/";
        var anonPaths = GetConfigEnumerable("AnonymousPaths", staticFilesCfg) ?? ["*"];
        HashSet<string>? anonPathsHash = anonPaths is null ?
            null : 
            new(Config.GetConfigEnumerable("AnonymousPaths", staticFilesCfg) ?? ["*"]);

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
                            Logger?.Information("Unauthorized access to {0}", path);
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
        Logger?.Information("Serving static files from {0}", app.Environment.WebRootPath);
    }

    public static string CreateUrl(Routine routine, NpgsqlRestOptions options) =>
        string.Concat(
            string.IsNullOrEmpty(options.UrlPathPrefix) ? "/" : string.Concat("/", options.UrlPathPrefix.Trim('/')),
            routine.Schema == "public" ? "" : routine.Schema.Trim('"').Trim('/'),
            "/",
            routine.Name.Trim('"').Trim('/'),
            "/");

    public static Func<Routine, RoutineEndpoint, RoutineEndpoint?>? CreateEndpointCreatedHandler()
    {
        var loginPath = GetConfigStr("LoginPath", AuthCfg);
        var logoutPath = GetConfigStr("LogoutPath", AuthCfg);
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

    public static Action<ParameterValidationValues>? CreateValidateParametersHandler()
    {
        var userIdParameterName = GetConfigStr("UserIdParameterName", AuthCfg);
        var userNameParameterName = GetConfigStr("UserNameParameterName", AuthCfg);
        var userRolesParameterName = GetConfigStr("UserRolesParameterName", AuthCfg);

        Dictionary<string, StringValues>? customClaims = null;
        foreach (var section in AuthCfg.GetSection("CustomParameterNameToClaimMappings").GetChildren())
        {
            customClaims ??= [];
            customClaims.Add(section.Key, section.Value);
        }

        if (userIdParameterName is null && userNameParameterName is null && userRolesParameterName is null && customClaims is null)
        {
            return null;
        }
        return (ParameterValidationValues p) =>
        {
            if (userIdParameterName is not null && string.Equals(p.Parameter.ActualName, userIdParameterName, StringComparison.OrdinalIgnoreCase))
            {
                p.Parameter.Value = p.Context.User.Claims
                    .FirstOrDefault(c => string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.Ordinal))?
                    .Value as object ?? DBNull.Value;
            }
            else if (userNameParameterName is not null && string.Equals(p.Parameter.ActualName, userNameParameterName, StringComparison.OrdinalIgnoreCase))
            {
                p.Parameter.Value = p.Context.User.Identity?.Name as object ?? DBNull.Value;
            }
            else if (userRolesParameterName is not null && string.Equals(p.Parameter.ActualName, userRolesParameterName, StringComparison.OrdinalIgnoreCase))
            {
                p.Parameter.Value = p.Context.User.Claims
                    .Where(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal))?
                    .Select(r => r.Value).ToArray() as object ?? DBNull.Value;
            } 
            else if (customClaims is not null)
            {
                if (customClaims.TryGetValue(p.Parameter.ActualName, out var claimName))
                {
                    if (p.Parameter.TypeDescriptor.IsArray)
                    {
                        p.Parameter.Value = p.Context.User.Claims
                            .Where(c => string.Equals(c.Type, claimName, StringComparison.Ordinal))?
                            .Select(r => r.Value).ToArray() as object ?? DBNull.Value;
                    }
                    else
                    {
                        p.Parameter.Value = p.Context.User.Claims
                            .FirstOrDefault(c => string.Equals(c.Type, claimName, StringComparison.Ordinal))?
                            .Value as object ?? DBNull.Value;
                    }
                }
            }
        };
    }

    public static Dictionary<string, int> CreatePostgreSqlErrorCodeToHttpStatusCodeMapping()
    {
        if (NpgsqlRestCfg.Exists() is false)
        {
            return new()
            {
                { "57014", 205 }, //query_canceled -> 205 Reset Content
            };
        }
        var config = NpgsqlRestCfg.GetSection("PostgreSqlErrorCodeToHttpStatusCodeMapping");
        if (config.Exists() is false)
        {
            return new()
            {
                { "57014", 205 }, //query_canceled -> 205 Reset Content
            };
        }
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

    public static Action<NpgsqlConnection, Routine, RoutineEndpoint, HttpContext>? BeforeConnectionOpen(string connectionString)
    {
        if (Config.UseConnectionApplicationNameWithUsername is false)
        {
            return null;
        }

        return (NpgsqlConnection connection, Routine routine, RoutineEndpoint endpoint, HttpContext context) =>
        {
            var uid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var executionId = context.Request.Headers["X-Execution-Id"].FirstOrDefault();
            connection.ConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
            {
                ApplicationName = string.Concat("{\"app\":\"", Instance.Environment.ApplicationName,
                        "\",\"uid\":", uid is null ? "null" : string.Concat("\"", uid, "\""),
                        ",\"id\":", executionId is null ? "null" : string.Concat("\"", executionId, "\""),
                        "}")
            }.ConnectionString;
        };
    }

    public static List<IEndpointCreateHandler> CreateCodeGenHandlers(string connectionString)
    {
        List<IEndpointCreateHandler> handlers = new(2);
        var httpFilecfg = NpgsqlRestCfg.GetSection("HttpFileOptions");
        if (httpFilecfg is not null && GetConfigBool("Enabled", httpFilecfg) is true)
        {
            handlers.Add(new HttpFile(new HttpFileOptions
            {
                Name = GetConfigStr("Name", httpFilecfg),
                Option = GetConfigEnum<HttpFileOption?>("Option", httpFilecfg) ?? HttpFileOption.File,
                NamePattern = GetConfigStr("NamePattern", httpFilecfg) ?? "{0}{1}",
                CommentHeader = GetConfigEnum<CommentHeader?>("CommentHeader", httpFilecfg) ?? CommentHeader.Simple,
                CommentHeaderIncludeComments = GetConfigBool("CommentHeaderIncludeComments", httpFilecfg, true),
                FileMode = GetConfigEnum<HttpFileMode?>("FileMode", httpFilecfg) ?? HttpFileMode.Schema,
                FileOverwrite = GetConfigBool("FileOverwrite", httpFilecfg, true),
                ConnectionString = connectionString
            }));
        }
        var tsClientCfg = NpgsqlRestCfg.GetSection("TsClient");
        if (tsClientCfg is not null && GetConfigBool("Enabled", tsClientCfg) is true)
        {
            var ts = new TsClientOptions
            {
                FilePath = GetConfigStr("FilePath", tsClientCfg),
                FileOverwrite = GetConfigBool("FileOverwrite", tsClientCfg, true),
                IncludeHost = GetConfigBool("IncludeHost", tsClientCfg, true),
                CustomHost = GetConfigStr("CustomHost", tsClientCfg),
                CommentHeader = GetConfigEnum<CommentHeader?>("CommentHeader", tsClientCfg) ?? CommentHeader.Simple,
                CommentHeaderIncludeComments = GetConfigBool("CommentHeaderIncludeComments", tsClientCfg, true),
                BySchema = GetConfigBool("BySchema", tsClientCfg, true),
                IncludeStatusCode = GetConfigBool("IncludeStatusCode", tsClientCfg, true),
                CreateSeparateTypeFile = GetConfigBool("CreateSeparateTypeFile", tsClientCfg, true),
                ImportBaseUrlFrom = GetConfigStr("ImportBaseUrlFrom", tsClientCfg),
                ImportParseQueryFrom = GetConfigStr("ImportParseQueryFrom", tsClientCfg),
                IncludeParseUrlParam = GetConfigBool("IncludeParseUrlParam", tsClientCfg),
                IncludeParseRequestParam = GetConfigBool("IncludeParseRequestParam", tsClientCfg),
                UseRoutineNameInsteadOfEndpoint = GetConfigBool("UseRoutineNameInsteadOfEndpoint", tsClientCfg),
                DefaultJsonType = GetConfigStr("DefaultJsonType", tsClientCfg) ?? "string",
                ExportUrls =  GetConfigBool("ExportUrls", tsClientCfg),
            };

            var headerLines = GetConfigEnumerable("HeaderLines", tsClientCfg);
            if (headerLines is not null)
            {
                ts.HeaderLines = headerLines.ToList();
            }

            var skipRoutineNames = GetConfigEnumerable("SkipRoutineNames", tsClientCfg);
            if (skipRoutineNames is not null)
            {
                ts.SkipRoutineNames = skipRoutineNames.ToArray();
            }

            var skipFunctionNames = GetConfigEnumerable("SkipFunctionNames", tsClientCfg);
            if (skipFunctionNames is not null)
            {
                ts.SkipFunctionNames = skipFunctionNames.ToArray();
            }

            var skipPaths = GetConfigEnumerable("SkipPaths", tsClientCfg);
            if (skipPaths is not null)
            {
                ts.SkipPaths = skipPaths.ToArray();
            }

            var skipSchemas = GetConfigEnumerable("SkipSchemas", tsClientCfg);
            if (skipSchemas is not null)
            {
                ts.SkipSchemas = skipSchemas.ToArray();
            }

            handlers.Add(new TsClient(ts));
        }

        return handlers;
    }

    public static List<IRoutineSource> CreateRoutineSources()
    {
        var source = new RoutineSource();
        var routineOptionsCfg = NpgsqlRestCfg.GetSection("RoutineOptions");
        if (routineOptionsCfg.Exists() is false)
        {
            return [source];
        }
        var customTypeParameterSeparator = GetConfigStr("CustomTypeParameterSeparator", routineOptionsCfg);
        if (customTypeParameterSeparator is not null)
        {
            source.CustomTypeParameterSeparator = customTypeParameterSeparator;
        }
        var includeLanguagues = GetConfigEnumerable("IncludeLanguagues", routineOptionsCfg);
        if (includeLanguagues is not null)
        {
            source.IncludeLanguagues = includeLanguagues.ToArray();
        }
        var excludeLanguagues = GetConfigEnumerable("ExcludeLanguagues", routineOptionsCfg);
        if (excludeLanguagues is not null)
        {
            source.ExcludeLanguagues = excludeLanguagues.ToArray();
        }

        return [source];
    }
}
