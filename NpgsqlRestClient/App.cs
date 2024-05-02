using System.Net;
using System.Security.Claims;
using Npgsql;
using NpgsqlRest;
using NpgsqlRest.CrudSource;
using NpgsqlRest.HttpFiles;
using NpgsqlRest.TsClient;
using Serilog;

using static NpgsqlRestClient.Config;
using static NpgsqlRestClient.Builder;

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

        var redirect = GetConfigStr("LoginRedirectPath", staticFilesCfg);
        var anonPaths = GetConfigEnumerable("AnonymousPaths", staticFilesCfg);
        HashSet<string>? anonPathsHash = anonPaths is null ? null : new(Config.GetConfigEnumerable("AnonymousPaths", staticFilesCfg) ?? []);

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

    public static Dictionary<string, int> CreatePostgreSqlErrorCodeToHttpStatusCodeMapping()
    {
        var config = NpgsqlRestCfg.GetSection("PostgreSqlErrorCodeToHttpStatusCodeMapping");
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
        var useConnectionApplicationNameWithUsername = GetConfigBool("UseJsonApplicationName", NpgsqlRestCfg);
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
                Option = GetConfigEnum<HttpFileOption>("Option", httpFilecfg),
                NamePattern = GetConfigStr("NamePattern", httpFilecfg) ?? "{0}{1}",
                CommentHeader = GetConfigEnum<CommentHeader>("CommentHeader", httpFilecfg),
                CommentHeaderIncludeComments = GetConfigBool("CommentHeaderIncludeComments", httpFilecfg),
                FileMode = GetConfigEnum<HttpFileMode>("FileMode", httpFilecfg),
                FileOverwrite = GetConfigBool("FileOverwrite", httpFilecfg),
                ConnectionString = connectionString
            }));
        }
        var tsClientCfg = NpgsqlRestCfg.GetSection("TsClient");
        if (tsClientCfg is not null && GetConfigBool("Enabled", tsClientCfg) is true)
        {
            var ts = new TsClientOptions
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

            handlers.Add(new TsClient(ts));
        }

        return handlers;
    }

    public static void SourcesCreated(List<IRoutineSource> sources)
    {
        var routineCfg = NpgsqlRestCfg.GetSection("RoutinesSource");
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

        var crudSourceCfg = NpgsqlRestCfg.GetSection("CrudSource");
        if (crudSourceCfg.Exists() is false || GetConfigBool("Enabled", crudSourceCfg) is false)
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
}
