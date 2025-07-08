using System.Net;
using System.Security.Claims;
using Npgsql;
using NpgsqlRest;
using NpgsqlRest.HttpFiles;
using NpgsqlRest.TsClient;
using Serilog;

using static NpgsqlRestClient.Config;
using static NpgsqlRestClient.Builder;
using Microsoft.Extensions.Primitives;
using NpgsqlRest.CrudSource;
using Microsoft.AspNetCore.Antiforgery;
using NpgsqlRest.UploadHandlers;
using NpgsqlRest.Auth;

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

        if (LogToConsole is true || LogToFile is true || LogToPostgres is true)
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

        app.UseDefaultFiles();

        string[]? autorizePaths = GetConfigEnumerable("AutorizePaths", staticFilesCfg)?.ToArray();
        string? unauthorizedRedirectPath = GetConfigStr("UnauthorizedRedirectPath", staticFilesCfg);
        string? unauthorizedReturnToQueryParameter = GetConfigStr("UnauthorizedReturnToQueryParameter", staticFilesCfg);

        var parseCfg = staticFilesCfg.GetSection("ParseContentOptions");
        
        bool parse = true;
        if (parseCfg.Exists() is false || GetConfigBool("Enabled", parseCfg) is false)
        {
            parse = false;
        }

        var filePaths = GetConfigEnumerable("FilePaths", parseCfg)?.ToArray();
        var userIdTag = GetConfigStr("UserIdTag", parseCfg);
        var userNameTag = GetConfigStr("UserNameTag", parseCfg);
        var userRolesTag = GetConfigStr("UserRolesTag", parseCfg);
        Dictionary<string, StringValues>? customTags = null;
        foreach (var section in parseCfg.GetSection("CustomTagToClaimMappings").GetChildren())
        {
            customTags ??= [];
            if (section?.Value is null)
            {
                continue;
            }
            customTags.Add(section.Key, section.Value!);
        }
        var antiforgeryFieldNameTag = GetConfigStr("AntiforgeryFieldName", parseCfg);
        var antiforgeryTokenTag = GetConfigStr("AntiforgeryToken", parseCfg);
        var antiforgery = app.Services.GetService<IAntiforgery>();

        AppStaticFileMiddleware.ConfigureStaticFileMiddleware(
            parse,
            filePaths,
            userIdTag,
            userNameTag,
            userRolesTag,
            customTags,
            GetConfigBool("CacheParsedFile", parseCfg, true),
            antiforgeryFieldNameTag,
            antiforgeryTokenTag,
            antiforgery,
            GetConfigEnumerable("Headers", parseCfg)?.ToArray(),
            autorizePaths,
            unauthorizedRedirectPath,
            unauthorizedReturnToQueryParameter,
            Logger?.ForContext<AppStaticFileMiddleware>());

        app.UseMiddleware<AppStaticFileMiddleware>();
        Logger?.Information("Serving static files from {0}. Parsing following file path patterns: {1}", app.Environment.WebRootPath, filePaths);
    }

    public static string CreateUrl(Routine routine, NpgsqlRestOptions options) =>
        string.Concat(
            string.IsNullOrEmpty(options.UrlPathPrefix) ? "/" : string.Concat("/", options.UrlPathPrefix.Trim('/')),
            routine.Schema == "public" ? "" : routine.Schema.Trim(Consts.DoubleQuote).Trim('/'),
            "/",
            routine.Name.Trim(Consts.DoubleQuote).Trim('/'),
            "/");

    public static (NpgsqlRestAuthenticationOptions options, IConfigurationSection authCfg) CreateNpgsqlRestAuthenticationOptions()
    {
        var authCfg = NpgsqlRestCfg.GetSection("AuthenticationOptions");
        if (authCfg.Exists() is false)
        {
            return (new NpgsqlRestAuthenticationOptions(), authCfg);
        }

        return (new()
        {
            DefaultAuthenticationType = GetConfigStr("DefaultAuthenticationType", authCfg),

            StatusColumnName = GetConfigStr("StatusColumnName", authCfg) ?? "status",
            SchemeColumnName = GetConfigStr("SchemeColumnName", authCfg) ?? "scheme",
            MessageColumnName = GetConfigStr("MessageColumnName", authCfg) ?? "message",
            UseActiveDirectoryFederationServicesClaimTypes = GetConfigBool("UseActiveDirectoryFederationServicesClaimTypes", authCfg, false),
            
            DefaultUserIdClaimType = GetConfigStr("DefaultUserIdClaimType", authCfg) ?? "nameidentifier",
            DefaultNameClaimType = GetConfigStr("DefaultNameClaimType", authCfg) ?? "name",
            DefaultRoleClaimType = GetConfigStr("DefaultRoleClaimType", authCfg) ?? "role",

            SerializeAuthEndpointsResponse = GetConfigBool("SerializeAuthEndpointsResponse", authCfg, false),
            ObfuscateAuthParameterLogValues = GetConfigBool("ObfuscateAuthParameterLogValues", authCfg, true),
            HashColumnName = GetConfigStr("HashColumnName", authCfg) ?? "hash",
            PasswordParameterNameContains = GetConfigStr("PasswordParameterNameContains", authCfg) ?? "pass",

            PasswordVerificationFailedCommand = GetConfigStr("PasswordVerificationFailedCommand", authCfg),
            PasswordVerificationSucceededCommand = GetConfigStr("PasswordVerificationSucceededCommand", authCfg),
            UseUserContext = GetConfigBool("UseUserContext", authCfg, false),
            UserIdContextKey = GetConfigStr("UserIdContextKey", authCfg) ?? "request.user_id",
            UserNameContextKey = GetConfigStr("UserNameContextKey", authCfg) ?? "request.user_name",
            UserRolesContextKey = GetConfigStr("UserRolesContextKey", authCfg) ?? "request.user_roles",
            IpAddressContextKey = GetConfigStr("IpAddressContextKey", authCfg) ?? "request.ip_address",
            UserClaimsContextKey = GetConfigStr("UserClaimsContextKey", authCfg) ?? "request.user_claims",

            UseUserParameters = GetConfigBool("UseUserParameters", authCfg, false),
            UserIdParameterName = GetConfigStr("UserIdParameterName", authCfg) ?? "_user_id",
            UserNameParameterName = GetConfigStr("UserNameParameterName", authCfg) ?? "_user_name",
            UserRolesParameterName = GetConfigStr("UserRolesParameterName", authCfg) ?? "_user_roles",
            IpAddressParameterName = GetConfigStr("IpAddressParameterName", authCfg) ?? "_ip_address",
            UserClaimsParameterName = GetConfigStr("UserClaimsParameterName", authCfg) ?? "_user_claims",
        }, authCfg);
    }

    public static IResponseParser? CreateDefaultParser(IConfigurationSection authCfg, NpgsqlRestAuthenticationOptions options)
    {
        if (authCfg.Exists() is false)
        {
            return null;
        }
        if (GetConfigBool("UseUserParameters", authCfg, false) is false)
        {
            return null;
        }


        Dictionary<string, string?>? customParameters = null;
        foreach (var section in NpgsqlRestCfg.GetSection("CustomParameterMappings").GetChildren())
        {
            customParameters ??= [];
            customParameters.Add(section.Key, section.Value);
        }

        return new DefaultResponseParser(
            options.UserIdParameterName,
            options.UserNameParameterName,
            options.UserRolesParameterName,
            options.IpAddressParameterName,
            null,
            null,
            null,
            customParameters);
    }

    public static Action<RoutineEndpoint?>? CreateEndpointCreatedHandler(IConfigurationSection authCfg)
    {
        if (authCfg.Exists() is false)
        {
            return null;
        }
        var loginPath = GetConfigStr("LoginPath", authCfg);
        var logoutPath = GetConfigStr("LogoutPath", authCfg);
        if (loginPath is null && logoutPath is null)
        {
            return null;
        }
        return endpoint =>
        {
            if (endpoint is null)
            {
                return;
            }
            if (loginPath is not null && string.Equals(endpoint.Url, loginPath, StringComparison.OrdinalIgnoreCase))
            {
                endpoint.Login = true;
            }
            if (logoutPath is not null && string.Equals(endpoint.Routine.Name, logoutPath, StringComparison.OrdinalIgnoreCase))
            {
                endpoint.Login = true;
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
                { "P0001", 400 }, // raise_exception -> 400 Bad Request
                { "P0004", 400 }, // assert_failure -> 400 Bad Request
            };
        }
        var config = NpgsqlRestCfg.GetSection("PostgreSqlErrorCodeToHttpStatusCodeMapping");
        if (config.Exists() is false)
        {
            return new()
            {
                { "57014", 205 }, //query_canceled -> 205 Reset Content
                { "P0001", 400 }, // raise_exception -> 400 Bad Request
                { "P0004", 400 }, // assert_failure -> 400 Bad Request
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

    public static Action<NpgsqlConnection, RoutineEndpoint, HttpContext>? BeforeConnectionOpen(string connectionString)
    {
        if (UseConnectionApplicationNameWithUsername is false)
        {
            return null;
        }

        return (connection, endpoint, context) =>
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

        var tsClientCfg = NpgsqlRestCfg.GetSection("ClientCodeGen");
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
                ExportUrls = GetConfigBool("ExportUrls", tsClientCfg),
                SkipTypes = GetConfigBool("SkipTypes", tsClientCfg),
                UniqueModels = GetConfigBool("UniqueModels", tsClientCfg),
                XsrfTokenHeaderName = GetConfigStr("XsrfTokenHeaderName", tsClientCfg),
                ExportEventSources = GetConfigBool("ExportEventSources", tsClientCfg, true),
                CustomImports = GetConfigEnumerable("CustomImports", tsClientCfg)?.ToArray() ?? [],
            };

            Dictionary<string, string> customHeaders = [];
            foreach (var section in tsClientCfg.GetSection("CustomHeaders").GetChildren())
            {
                if (section?.Value is null)
                {
                    continue;
                }
                customHeaders.Add(section.Key, section.Value!);
            }
            ts.CustomHeaders = customHeaders;

            var headerLines = GetConfigEnumerable("HeaderLines", tsClientCfg);
            if (headerLines is not null)
            {
                ts.HeaderLines = [.. headerLines];
            }

            var skipRoutineNames = GetConfigEnumerable("SkipRoutineNames", tsClientCfg);
            if (skipRoutineNames is not null)
            {
                ts.SkipRoutineNames = [.. skipRoutineNames];
            }

            var skipFunctionNames = GetConfigEnumerable("SkipFunctionNames", tsClientCfg);
            if (skipFunctionNames is not null)
            {
                ts.SkipFunctionNames = [.. skipFunctionNames];
            }

            var skipPaths = GetConfigEnumerable("SkipPaths", tsClientCfg);
            if (skipPaths is not null)
            {
                ts.SkipPaths = [.. skipPaths];
            }

            var skipSchemas = GetConfigEnumerable("SkipSchemas", tsClientCfg);
            if (skipSchemas is not null)
            {
                ts.SkipSchemas = [.. skipSchemas];
            }

            handlers.Add(new TsClient(ts));
        }

        return handlers;
    }

    public static List<IRoutineSource> CreateRoutineSources()
    {
        var sources = new List<IRoutineSource>(2);

        var source = new RoutineSource();
        var routineOptionsCfg = NpgsqlRestCfg.GetSection("RoutineOptions");
        if (routineOptionsCfg.Exists() is true)
        {
            var customTypeParameterSeparator = GetConfigStr("CustomTypeParameterSeparator", routineOptionsCfg);
            if (customTypeParameterSeparator is not null)
            {
                source.CustomTypeParameterSeparator = customTypeParameterSeparator;
            }
            var includeLanguagues = GetConfigEnumerable("IncludeLanguagues", routineOptionsCfg);
            if (includeLanguagues is not null)
            {
                source.IncludeLanguagues = [.. includeLanguagues];
            }
            var excludeLanguagues = GetConfigEnumerable("ExcludeLanguagues", routineOptionsCfg);
            if (excludeLanguagues is not null)
            {
                source.ExcludeLanguagues = [.. excludeLanguagues];
            }
        }
        sources.Add(source);
        Logger?.Information("Using {name} PostrgeSQL Source", nameof(RoutineSource));

        var crudSourceCfg = NpgsqlRestCfg.GetSection("CrudSource");
        if (crudSourceCfg.Exists() is false || GetConfigBool("Enabled", crudSourceCfg) is false)
        {
            return sources;
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
            CommentsMode = GetConfigEnum<CommentsMode?>("CommentsMode", crudSourceCfg),
            CrudTypes = GetConfigFlag<CrudCommandType>("CrudTypes", crudSourceCfg),

            ReturningUrlPattern = GetConfigStr("ReturningUrlPattern", crudSourceCfg) ?? "{0}/returning",
            OnConflictDoNothingUrlPattern = GetConfigStr("OnConflictDoNothingUrlPattern", crudSourceCfg) ?? "{0}/on-conflict-do-nothing",
            OnConflictDoNothingReturningUrlPattern = GetConfigStr("OnConflictDoNothingReturningUrlPattern", crudSourceCfg) ?? "{0}/on-conflict-do-nothing/returning",
            OnConflictDoUpdateUrlPattern = GetConfigStr("OnConflictDoUpdateUrlPattern", crudSourceCfg) ?? "{0}/on-conflict-do-update",
            OnConflictDoUpdateReturningUrlPattern = GetConfigStr("OnConflictDoUpdateReturningUrlPattern", crudSourceCfg) ?? "{0}/on-conflict-do-update/returning",
        });
        Logger?.Information("Using {name} PostrgeSQL Source", nameof(CrudSource));
        return sources;
    }

    public static NpgsqlRestUploadOptions CreateUploadOptions()
    {
        var uploadCfg = NpgsqlRestCfg.GetSection("UploadOptions");
        if (uploadCfg.Exists() is false)
        {
            return new NpgsqlRestUploadOptions();
        }

        var result = new NpgsqlRestUploadOptions
        {
            Enabled = GetConfigBool("Enabled", uploadCfg, true),
            LogUploadEvent = GetConfigBool("LogUploadEvent", uploadCfg, true),
            LogUploadParameters = GetConfigBool("LogUploadParameters", uploadCfg, false),
            DefaultUploadHandler = GetConfigStr("DefaultUploadHandler", uploadCfg) ?? "large_object",
            UseDefaultUploadMetadataParameter = GetConfigBool("UseDefaultUploadMetadataParameter", uploadCfg, false),
            DefaultUploadMetadataParameterName = GetConfigStr("DefaultUploadMetadataParameterName", uploadCfg) ?? "_upload_metadata",
            UseDefaultUploadMetadataContextKey = GetConfigBool("UseDefaultUploadMetadataContextKey", uploadCfg, false),
            DefaultUploadMetadataContextKey = GetConfigStr("DefaultUploadMetadataContextKey", uploadCfg) ?? "request.upload_metadata",
        };

        var uploadHandlersCfg = uploadCfg.GetSection("UploadHandlers");
        UploadHandlerOptions uploadHandlerOptions;
        if (uploadHandlersCfg.Exists() is false)
        {
            uploadHandlerOptions = new();
        }
        else
        {
            uploadHandlerOptions = new()
            {
                StopAfterFirstSuccess = GetConfigBool("StopAfterFirstSuccess", uploadHandlersCfg, false),
                IncludedMimeTypePatterns = GetConfigStr("IncludedMimeTypePatterns", uploadHandlersCfg).SplitParameter(),
                ExcludedMimeTypePatterns = GetConfigStr("ExcludedMimeTypePatterns", uploadHandlersCfg).SplitParameter(),
                BufferSize = GetConfigInt("BufferSize", uploadHandlersCfg) ?? 8192,
                TextTestBufferSize = GetConfigInt("TextTestBufferSize", uploadHandlersCfg) ?? 4096,
                TextNonPrintableThreshold = GetConfigInt("TextNonPrintableThreshold", uploadHandlersCfg) ?? 5,

                LargeObjectEnabled = GetConfigBool("LargeObjectEnabled", uploadHandlersCfg, true),
                LargeObjectKey = GetConfigStr("LargeObjectKey", uploadHandlersCfg) ?? "large_object",
                LargeObjectCheckText = GetConfigBool("LargeObjectCheckText", uploadHandlersCfg, false),
                LargeObjectCheckImage = GetConfigBool("LargeObjectCheckImage", uploadHandlersCfg, false),

                FileSystemEnabled = GetConfigBool("FileSystemEnabled", uploadHandlersCfg, true),
                FileSystemKey = GetConfigStr("FileSystemKey", uploadHandlersCfg) ?? "file_system",
                FileSystemPath = GetConfigStr("FileSystemPath", uploadHandlersCfg) ?? "/tmp/uploads",
                FileSystemUseUniqueFileName = GetConfigBool("FileSystemUseUniqueFileName", uploadHandlersCfg, true),
                FileSystemCreatePathIfNotExists = GetConfigBool("FileSystemCreatePathIfNotExists", uploadHandlersCfg, true),
                FileSystemCheckText = GetConfigBool("FileSystemCheckText", uploadHandlersCfg, false),
                FileSystemCheckImage = GetConfigBool("FileSystemCheckImage", uploadHandlersCfg, false),

                CsvUploadEnabled = GetConfigBool("CsvUploadEnabled", uploadHandlersCfg, true),
                CsvUploadCheckFileStatus = GetConfigBool("CsvUploadCheckFileStatus", uploadHandlersCfg, true),
                CsvUploadDelimiterChars = GetConfigStr("CsvUploadDelimiterChars", uploadHandlersCfg) ?? ",",
                CsvUploadHasFieldsEnclosedInQuotes = GetConfigBool("CsvUploadHasFieldsEnclosedInQuotes", uploadHandlersCfg, true),
                CsvUploadSetWhiteSpaceToNull = GetConfigBool("CsvUploadSetWhiteSpaceToNull", uploadHandlersCfg, true),
                CsvUploadRowCommand = GetConfigStr("CsvUploadRowCommand", uploadHandlersCfg) ?? "call process_csv_row($1,$2,$3,$4)",
            };
            var imageTypes = GetConfigStr("AllowedImageTypes", uploadHandlersCfg)?.ParseImageTypes(null);
            if (imageTypes is not null)
            {
                uploadHandlerOptions.AllowedImageTypes = imageTypes.Value;
            }
        }
        result.DefaultUploadHandlerOptions = uploadHandlerOptions;

        result.UploadHandlers = result.CreateUploadHandlers();

        if (GetConfigBool("ExcelUploadEnabled", uploadHandlersCfg, true))
        {
            // Initialize ExcelDataReader encoding provider
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            ExcelUploadOptions.Instance.ExcelSheetName = GetConfigStr("ExcelSheetName", uploadHandlersCfg) ?? null;
            ExcelUploadOptions.Instance.ExcelAllSheets = GetConfigBool("ExcelAllSheets", uploadHandlersCfg, false);
            ExcelUploadOptions.Instance.ExcelTimeFormat = GetConfigStr("ExcelTimeFormat", uploadHandlersCfg) ?? "HH:mm:ss";
            ExcelUploadOptions.Instance.ExcelDateFormat = GetConfigStr("ExcelDateFormat", uploadHandlersCfg) ?? "yyyy-MM-dd";
            ExcelUploadOptions.Instance.ExcelDateTimeFormat = GetConfigStr("ExcelDateTimeFormat", uploadHandlersCfg) ?? "yyyy-MM-dd HH:mm:ss";
            ExcelUploadOptions.Instance.ExcelRowDataAsJson = GetConfigBool("ExcelRowDataAsJson", uploadHandlersCfg, false);
            ExcelUploadOptions.Instance.ExcelUploadRowCommand = GetConfigStr("ExcelUploadRowCommand", uploadHandlersCfg) ?? "call process_excel_row($1,$2,$3,$4)";

            result?.UploadHandlers?.Add(GetConfigStr("ExcelKey", uploadHandlersCfg) ?? "excel", logger => new ExcelUploadHandler(result, logger));
        }

        if (result?.UploadHandlers is not null && result.UploadHandlers.Count > 1)
        {
            Logger?.Information("Using {0} upload handlers where {1} is default.", result.UploadHandlers.Keys, result.DefaultUploadHandler);
            foreach (var uploadHandler in result.UploadHandlers)
            {
                Logger?.Information("Upload handler {0} has following parameters: {1}", uploadHandler.Key, uploadHandler.Value(null!).SetType(uploadHandler.Key).Parameters);
            }
        }
        return result!;
    }

    public static void ConfigureThreadPool()
    {
        var threadPoolCfg = Cfg.GetSection("ThreadPool");
        if (threadPoolCfg.Exists() is false)
        {
            return;
        }

        var minWorkerThreads = GetConfigInt("MinWorkerThreads", threadPoolCfg);
        var minCompletionPortThreads = GetConfigInt("MinCompletionPortThreads", threadPoolCfg);
        if (minWorkerThreads is not null || minCompletionPortThreads is not null)
        {
            if (minWorkerThreads is null || minCompletionPortThreads is null)
            {
                ThreadPool.GetMinThreads(out var minWorkerThreadsTmp, out var minCompletionPortThreadsTmp);
                minWorkerThreads ??= minWorkerThreadsTmp;
                minCompletionPortThreads ??= minCompletionPortThreadsTmp;
            }
            ThreadPool.SetMinThreads(workerThreads: minWorkerThreads.Value, completionPortThreads: minCompletionPortThreads.Value);
        }

        var maxWorkerThreads = GetConfigInt("MaxWorkerThreads", threadPoolCfg);
        var maxCompletionPortThreads = GetConfigInt("MaxCompletionPortThreads", threadPoolCfg);
        if (maxWorkerThreads is not null || maxCompletionPortThreads is not null)
        {
            if (maxWorkerThreads is null || maxCompletionPortThreads is null)
            {
                ThreadPool.GetMaxThreads(out var maxWorkerThreadsTmp, out var maxCompletionPortThreadsTmp);
                maxWorkerThreads ??= maxWorkerThreadsTmp;
                maxCompletionPortThreads ??= maxCompletionPortThreadsTmp;
            }
            ThreadPool.SetMaxThreads(workerThreads: maxWorkerThreads.Value, completionPortThreads: maxCompletionPortThreads.Value);
        }
    }
}