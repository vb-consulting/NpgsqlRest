using System.Text.Json.Nodes;
using System.Text;
using System.Net.Http.Headers;
using Npgsql;
using System.Net;
using NpgsqlRest;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using Serilog.Events;

using static System.Net.Mime.MediaTypeNames;
using static NpgsqlRestClient.Config;
using static NpgsqlRestClient.Builder;
using static NpgsqlRest.Auth.ClaimsDictionary;

namespace NpgsqlRestClient;

public class ExternalAuthClientConfig
{
    public bool Enabled { get; set; } = false;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string AuthUrl { get; set; } = default!;
    public string TokenUrl { get; set; } = default!;
    public string InfoUrl { get; set; } = default!;
    public string? EmailUrl { get; set; } = null;
    public string SigninUrl { get; set; } = default!;
    public string ExternalType { get; set; } = default!;
}

public static class ExternalAuthConfig
{
    public static string BrowserSessionStatusKey { get; private set; } = default!;
    public static string BrowserSessionMessageKey { get; private set; } = default!;
    public static string SignInHtmlTemplate { get; private set; } = default!;
    public static string? RedirectUrl { get; private set; } = null;
    public static string ReturnToPath { get; private set; } = default!;
    public static string ReturnToPathQueryStringKey { get; private set; } = default!;
    public static string LoginCommand { get; private set; } = default!;
    public static string ClientAnaliticsData { get; private set; } = default!;
    public static string ClientAnaliticsIpKey { get; private set; } = default!;

    private static readonly Dictionary<string, ExternalAuthClientConfig> DefaultClientConfigs = 
        new()
        {
        { "google", new ExternalAuthClientConfig
            {
                AuthUrl = "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={0}&redirect_uri={1}&scope=openid profile email&state={2}",
                TokenUrl = "https://oauth2.googleapis.com/token",
                InfoUrl = "https://www.googleapis.com/oauth2/v3/userinfo",
            }
        },
        { "linkedin", new ExternalAuthClientConfig
            {
                AuthUrl = "https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={0}&redirect_uri={1}&state={2}&scope=r_liteprofile%20r_emailaddress",
                TokenUrl = "https://www.linkedin.com/oauth/v2/accessToken",
                InfoUrl = "https://api.linkedin.com/v2/me",
                EmailUrl = "https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements//(handle~))"
            }
        },
        { "github", new ExternalAuthClientConfig
            {
                AuthUrl = "https://github.com/login/oauth/authorize?client_id={0}&redirect_uri={1}&state={2}&allow_signup=false",
                TokenUrl = "https://github.com/login/oauth/access_token",
                InfoUrl = "https://api.github.com/user",
            }
        },
        { "microsoft", new ExternalAuthClientConfig
            {
                AuthUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?response_type=code&client_id={0}&redirect_uri={1}&scope=openid%20profile%20email&state={2}",
                TokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                InfoUrl = "https://graph.microsoft.com/oidc/userinfo",
            }
        },
        { "facebook", new ExternalAuthClientConfig
            {
                AuthUrl = "https://www.facebook.com/v20.0/dialog/oauth?response_type=code&client_id={0}&redirect_uri={1}&scope=public_profile%20email&state={2}",
                TokenUrl = "https://graph.facebook.com/v20.0/oauth/access_token",
                InfoUrl = "https://graph.facebook.com/me?fields=id,name,email",
            }
        }
    };

    public static Dictionary<string, ExternalAuthClientConfig> ClientConfigs { get; private set; } = [];

    public static void Build(IConfigurationSection authConfig)
    {
        var externalCfg = authConfig.GetSection("External");
        if (externalCfg.Exists() is false || GetConfigBool("Enabled", externalCfg) is false)
        {
            return;
        }

        foreach (var section in externalCfg.GetChildren())
        {
            if (section.GetChildren().Any() is false)
            {
                continue;
            }
            var signinUrlTemplate = GetConfigStr("SigninUrl", externalCfg) ?? "/signin-{0}";

            if (GetConfigBool("Enabled", section) is false)
            {
                continue;
            }
            var key = section.Key.ToLowerInvariant();
            var signinUrl = string.Format(signinUrlTemplate, key);

            if (DefaultClientConfigs.TryGetValue(key, out var config) is false)
            {
                config = new ExternalAuthClientConfig();
            }

            config.Enabled = GetConfigBool("Enabled", section, config.Enabled);
            config.ClientId = GetConfigStr("ClientId", section) ?? config.ClientId;
            config.ClientSecret = GetConfigStr("ClientSecret", section) ?? config.ClientSecret;
            config.AuthUrl = GetConfigStr("AuthUrl", section) ?? config.AuthUrl;
            config.TokenUrl = GetConfigStr("TokenUrl", section) ?? config.TokenUrl;
            config.InfoUrl = GetConfigStr("InfoUrl", section) ?? config.InfoUrl;
            config.EmailUrl = GetConfigStr("EmailUrl", section) ?? config.EmailUrl;
            config.SigninUrl = signinUrl;
            config.ExternalType = section.Key;

            ClientConfigs.Add(config.SigninUrl, config);
            Logger?.Information("External login available for {0} available on path: {1}", section.Key, signinUrl);
        }

        if (ClientConfigs.Where(c => c.Value.Enabled).Any() is false)
        {
            return;
        }

        BrowserSessionStatusKey = GetConfigStr("BrowserSessionStatusKey", externalCfg) ?? "__external_status";
        BrowserSessionMessageKey = GetConfigStr("BrowserSessionMessageKey", externalCfg) ?? "__external_message";

        SignInHtmlTemplate = GetConfigStr("SignInHtmlTemplate", externalCfg) ?? 
            "<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>Talking To {0}</title></head><body>Loading...{1}</body></html>";
        RedirectUrl = GetConfigStr("RedirectUrl", externalCfg);
        ReturnToPath = GetConfigStr("ReturnToPath", externalCfg) ?? "/";
        ReturnToPathQueryStringKey = GetConfigStr("ReturnToPathQueryStringKey", externalCfg) ?? "return_to";
        LoginCommand = GetConfigStr("LoginCommand", externalCfg) ?? "select * from auth.login($1,$2,$3,$4)";

        ClientAnaliticsData = GetConfigStr("ClientAnaliticsData", externalCfg) ?? "{timestamp:new Date().toISOString(),timezone:Intl.DateTimeFormat().resolvedOptions().timeZone,screen:{width:window.screen.width,height:window.screen.height,colorDepth:window.screen.colorDepth,pixelRatio:window.devicePixelRatio,orientation:screen.orientation.type},browser:{userAgent:navigator.userAgent,language:navigator.language,languages:navigator.languages,cookiesEnabled:navigator.cookieEnabled,doNotTrack:navigator.doNotTrack,onLine:navigator.onLine,platform:navigator.platform,vendor:navigator.vendor},memory:{deviceMemory:navigator.deviceMemory,hardwareConcurrency:navigator.hardwareConcurrency},window:{innerWidth:window.innerWidth,innerHeight:window.innerHeight,outerWidth:window.outerWidth,outerHeight:window.outerHeight},location:{href:window.location.href,hostname:window.location.hostname,pathname:window.location.pathname,protocol:window.location.protocol,referrer:document.referrer},performance:{navigation:{type:performance.navigation?.type,redirectCount:performance.navigation?.redirectCount},timing:performance.timing?{loadEventEnd:performance.timing.loadEventEnd,loadEventStart:performance.timing.loadEventStart,domComplete:performance.timing.domComplete,domInteractive:performance.timing.domInteractive,domContentLoadedEventEnd:performance.timing.domContentLoadedEventEnd}:null}}";
        ClientAnaliticsIpKey = GetConfigStr("ClientAnaliticsIpKey", externalCfg) ?? "ip";
    }
}

public static class ExternalAuth
{
    private static PostgresConnectionNoticeLoggingMode _loggingMode;

    public static void Configure(WebApplication app, NpgsqlRestOptions options, PostgresConnectionNoticeLoggingMode loggingMode)
    {
        if (ExternalAuthConfig.ClientConfigs.Where(c => c.Value.Enabled).Any() is false)
        {
            return;
        }
        _loggingMode = loggingMode;
        app.Use(async (context, next) =>
        {
            if (ExternalAuthConfig.ClientConfigs.TryGetValue(context.Request.Path, out var config) is false)
            {
                await next(context);
                return;
            }

            if (config.Enabled is false)
            {
                await next(context);
                return;
            }

            if (string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                PrepareResponse(Text.Html, context);
                await context.Response.WriteAsync(BuildHtmlTemplate(config, context.Request));
                await context.Response.CompleteAsync();
                return;
            }

            if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                PrepareResponse(Text.Plain, context);
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                try
                {
                    var body = await reader.ReadToEndAsync();
                    JsonNode node = JsonNode.Parse(body) ??
                        throw new ArgumentException("json node is null");
                    string code = (node["code"]?.ToString()) ??
                        throw new ArgumentException("code retrieved from the external provider is null");

                    await ProcessAsync(code, config, context, options);
                    return;
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync(string.Format("Error talking to {0}", config.ExternalType));
                    await context.Response.CompleteAsync();
                    Logger?.Error(e, "Failed to parse external provider response: {0}", config.ExternalType);
                    return;
                }
            }

            await next(context);
        });
    }

    private static void PrepareResponse(string contentType, HttpContext context)
    {
        context.Response.ContentType = contentType;
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, proxy-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    private static HttpClient? _httpClient = null;
    private static readonly string _agent = $"{Guid.NewGuid().ToString()[..8]}";

    private const string browserSessionStateKey = "__external_state";
    private const string browserSessionParamsKey = "__external_params";

    private static async Task ProcessAsync(
        string code,
        ExternalAuthClientConfig config, 
        HttpContext context, 
        NpgsqlRestOptions options)
    {
        string? email;
        string? name;
        string token;

        _httpClient ??= new();

        using var requestTokenMessage = new HttpRequestMessage(HttpMethod.Post, config.TokenUrl);
        requestTokenMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Application.Json));
        requestTokenMessage.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["redirect_uri"] = GetRedirectUrl(config, context.Request),
            ["code"] = code,
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["grant_type"] = "authorization_code"
        });
        requestTokenMessage.Headers.UserAgent.ParseAdd(_agent);

        using var tokenResponse = await _httpClient.SendAsync(requestTokenMessage);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Token endpoint {config.TokenUrl} returned {tokenResponse.StatusCode} with following content: {tokenContent}");
        }
        if (string.IsNullOrEmpty(tokenContent))
        {
            throw new HttpRequestException($"Token endpoint {config.TokenUrl} returned {tokenResponse.StatusCode} with empty content.");
        }

        try
        {
            JsonNode node = JsonNode.Parse(tokenContent) ??
                throw new ArgumentException("token json node is null");
            token = (node["access_token"]?.ToString()) ??
                throw new ArgumentException("access_token retrieved from the external provider is null");
        }
        catch (Exception e)
        {
            throw new ArgumentException(string.Format("Failed to parse the token response: {0}", e.Message), e);
        }

        using var infoRequest = new HttpRequestMessage(HttpMethod.Get, config.InfoUrl);
        infoRequest.Headers.UserAgent.ParseAdd(_agent);
        infoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var infoResponse = await _httpClient.SendAsync(infoRequest);
        var infoContent = await infoResponse.Content.ReadAsStringAsync();
        JsonNode infoNode;
        if (!infoResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Info endpoint {config.InfoUrl} returned {infoResponse.StatusCode} with following content: {infoContent}");
        }
        if (string.IsNullOrEmpty(infoContent))
        {
            throw new HttpRequestException($"Info endpoint {config.InfoUrl} returned {infoResponse.StatusCode} with empty content.");
        }

        try
        {
            infoNode = JsonNode.Parse(infoContent) ??
                throw new ArgumentException("info json node is null");

            // Email parsing
            email = infoNode["email"]?.ToString() ?? // Google, GitHub, Facebook, Microsoft
                    infoNode["userPrincipalName"]?.ToString() ?? // Microsoft (organizational accounts)
                    infoNode?["elements"]?[0]?["handle~"]?["emailAddress"]?.ToString(); // LinkedIn

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            name = infoNode["localizedLastName"] is not null ?
                $"{infoNode["localizedFirstName"]} {infoNode["localizedLastName"]}".Trim() : // linkedin format
                infoNode["name"]?.ToString(); // normal format
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
        catch (Exception e)
        {
            throw new ArgumentException(string.Format("Failed to parse the info response: {0}", e.Message), e);
        }

        if (email is null && config.EmailUrl is not null)
        {
            using var emailRequest = new HttpRequestMessage(HttpMethod.Get, config.EmailUrl);
            infoRequest.Headers.UserAgent.ParseAdd(_agent);
            infoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var emailResponse = await _httpClient.SendAsync(emailRequest);
            var emailContent = await emailResponse.Content.ReadAsStringAsync();
            if (!emailResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Email endpoint {config.EmailUrl} returned {emailResponse.StatusCode} with following content: {emailResponse}");
            }
            if (string.IsNullOrEmpty(emailContent))
            {
                throw new HttpRequestException($"Email endpoint {config.EmailUrl} returned {emailResponse.StatusCode} with empty content.");
            }

            try
            {
                JsonNode node = JsonNode.Parse(emailContent) ??
                    throw new ArgumentException("email json node is null");
                infoNode["emailRequest"] = node;

                email = node["email"]?.ToString() ??
                    node?["elements"]?[0]?["handle~"]?["emailAddress"]?.ToString(); // linkedin format
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Failed to parse the email response: {0}", e.Message), e);
            }
        }

        if (email is null)
        {
            throw new ArgumentException("email retrieved from the external provider is null");
        }

        var logger = NpgsqlRestMiddleware.Logger;
        using var connection = new NpgsqlConnection(Builder.ConnectionString);
        if (options.LogConnectionNoticeEvents && logger != null)
        {
            connection.Notice += (sender, args) =>
            {
                NpgsqlRestLogger.LogConnectionNotice(logger, args, _loggingMode);
            };
        }

        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = ExternalAuthConfig.LoginCommand;

        var paramCount = command.CommandText.PgCountParams();
        if (paramCount >= 1) command.Parameters.Add(new NpgsqlParameter()
        {
            Value = config.ExternalType,
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text
        });
        if (paramCount >= 2) command.Parameters.Add(new NpgsqlParameter()
        {
            Value = email,
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text
        });
        if (paramCount >= 3) command.Parameters.Add(new NpgsqlParameter()
        {
            Value = name is not null ? name : DBNull.Value,
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text
        });
        //emailContent
        
        if (paramCount >= 4) command.Parameters.Add(new NpgsqlParameter()
        {
            Value = infoNode is not null ? infoContent.ToString() : DBNull.Value,
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Json
        });
        if (paramCount >= 5)
        {
            var query = context.Request.Query["analyticsData"].ToString();
            if (string.IsNullOrEmpty(query) is false)
            {
                var analyticsData = JsonNode.Parse(query);
                if (analyticsData is not null)
                {
                    if (string.IsNullOrEmpty(ExternalAuthConfig.ClientAnaliticsIpKey) is false)
                    {
                        analyticsData[ExternalAuthConfig.ClientAnaliticsIpKey] = App.GetClientIpAddress(context.Request);
                    }
                    command.Parameters.Add(new NpgsqlParameter()
                    {
                        Value = analyticsData.ToJsonString(),
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Json
                    });
                }
            }
        }

        if (logger?.IsEnabled(LogLevel.Information) is true && options.LogCommands)
        {
            var commandToLog = command.CommandText;
            if (options.LogCommandParameters)
            {
                StringBuilder paramsToLog = new();
                for (var i = 0; i < command.Parameters.Count; i++)
                {
                    var p = command.Parameters[i];
                    paramsToLog!.AppendLine(string.Concat(
                        "-- $",
                        (i + 1).ToString(),
                        " ", p.DataTypeName,
                        " = ",
                        p.Value));
                }
                logger?.Log(LogLevel.Information, "{paramsToLog}{commandToLog}", paramsToLog, commandToLog);
            }
            else
            {
                logger?.Log(LogLevel.Information, "{commandToLog}", commandToLog);
            }
        }

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync() is false || reader.FieldCount == 0)
        {
            await CompleteWithErrorAsync("Login command did not return any data.", HttpStatusCode.NotFound, LogEventLevel.Warning, context);
            return;
        }

        var authenticationType = options.AuthenticationOptions.DefaultAuthenticationType;
        string? scheme = null;
        string? message = null;
        var claims = new List<Claim>(10);
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        for (int i = 0; i < reader?.FieldCount; i++)
        {
            string name1 = reader.GetName(i);
            string name2 = options.NameConverter(name1) ?? name1;
            var descriptor = new TypeDescriptor(reader.GetDataTypeName(i));

            if (options.AuthenticationOptions.StatusColumnName is not null)
            {
                if (string.Equals(name1, options.AuthenticationOptions.StatusColumnName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name2, options.AuthenticationOptions.StatusColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    if (descriptor.IsBoolean)
                    {
                        var ok = reader?.GetBoolean(i);
                        if (ok is false)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                    }
                    else if (descriptor.IsNumeric)
                    {
                        var status = reader?.GetInt32(i) ?? 200;
                        if (status != (int)HttpStatusCode.OK)
                        {
                            context.Response.StatusCode = status;
                        }
                    }
                    else
                    {
                        await CompleteWithErrorAsync("External login command returns a status field that is not either boolean or numeric.",
                            HttpStatusCode.InternalServerError, LogEventLevel.Error, context);
                        return;
                    }
                    continue;
                }
            }

            if (options.AuthenticationOptions.SchemeColumnName is not null)
            {
                if (string.Equals(name1, options.AuthenticationOptions.SchemeColumnName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name2, options.AuthenticationOptions.SchemeColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    scheme = reader?.GetValue(i).ToString();
                    continue;
                }
            }

            if (options.AuthenticationOptions.MessageColumnName is not null)
            {
                if (string.Equals(name1, options.AuthenticationOptions.MessageColumnName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name2, options.AuthenticationOptions.MessageColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    message = reader?.GetValue(i).ToString();
                    continue;
                }
            }

            string? claimType;
            if (options.AuthenticationOptions.UseActiveDirectoryFederationServicesClaimTypes)
            {
                if (ClaimTypesDictionary.TryGetValue(name1.ToLowerInvariant(), out claimType) is false)
                {
                    if (ClaimTypesDictionary.TryGetValue(name2.ToLowerInvariant(), out claimType) is false)
                    {
                        claimType = name2;
                    }
                }
            }
            else
            {
                claimType = name2;
            }

            if (reader?.IsDBNull(i) is true)
            {
                claims.Add(new Claim(claimType, ""));
            }
            else if (descriptor.IsArray)
            {
                object[]? values = reader?.GetValue(i) as object[];
                for (int j = 0; j < values?.Length; j++)
                {
                    claims.Add(new Claim(claimType, values[j]?.ToString() ?? ""));
                }
            }
            else
            {
                string? value = reader?.GetValue(i)?.ToString();
                claims.Add(new Claim(claimType, value ?? ""));
            }
        }

        if (context.Response.StatusCode == (int)HttpStatusCode.OK)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                claims, scheme ?? authenticationType,
                nameType: options.AuthenticationOptions.DefaultNameClaimType,
                roleType: options.AuthenticationOptions.DefaultRoleClaimType));

            if (Results.SignIn(principal: principal, authenticationScheme: scheme) is not SignInHttpResult result)
            {
                await CompleteWithErrorAsync("Failed in constructing user identity for authentication.",
                    HttpStatusCode.InternalServerError, LogEventLevel.Error, context);
                return;
            }
            await result.ExecuteAsync(context);
        }

        if (message is not null)
        {
            await context.Response.WriteAsync(message);
        }
        await context.Response.CompleteAsync();
    }

    private static async Task CompleteWithErrorAsync(
        string text, 
        HttpStatusCode code, 
        LogEventLevel? logLevel, 
        HttpContext context)
    {
        if (logLevel.HasValue)
        {
            Logger?.Write(logLevel.Value, text);
        }
        context.Response.StatusCode = (int)code;
        await context.Response.WriteAsync(text);
        await context.Response.CompleteAsync();
    }

    private static string BuildHtmlTemplate(ExternalAuthClientConfig config, HttpRequest request)
    {
        string redirectUrl = GetRedirectUrl(config, request);
        var state = Guid.NewGuid().ToString();
        var redirectTo = "/";
        var authUrl = string.Format(config.AuthUrl, config.ClientId, redirectUrl, state);
        var analyticsData = ExternalAuthConfig.ClientAnaliticsData is null ? "null" : ExternalAuthConfig.ClientAnaliticsData;
        return string.Format(ExternalAuthConfig.SignInHtmlTemplate,
            config.ExternalType,
            string.Format(_jsTemplate,
                browserSessionStateKey,//0
                state,//1
                browserSessionParamsKey,//2
                ExternalAuthConfig.BrowserSessionStatusKey,//3
                ExternalAuthConfig.BrowserSessionMessageKey,//4
                redirectTo,//5
                authUrl,//6
                config.SigninUrl,//7
                ExternalAuthConfig.ReturnToPathQueryStringKey,//8
                config.ExternalType,//9
                analyticsData//10
            ));
    }

    private static string GetRedirectUrl(ExternalAuthClientConfig clientConfig, HttpRequest request)
    {
        return ExternalAuthConfig.RedirectUrl is null ?
            string.Concat(request.Scheme, "://", request.Host, clientConfig.SigninUrl) :
            string.Concat(ExternalAuthConfig.RedirectUrl, clientConfig.SigninUrl);
    }

    private const string _jsTemplate =
        """
        <script>
        (async function () {{
            function paramsToObject(search) {{
                const hashes = search.slice(search.indexOf('?') + 1).split('&');
                const params = {{}};
                hashes.map(hash => {{
                    const [key, val] = hash.split('=');
                    if (key && val) {{
                        params[key] = decodeURIComponent(val);
                    }}
                }});
                return params;
            }}
            var stateKey = "{0}";
            var stateValue = "{1}";
            var paramsKey = "{2}";
            var statusKey = "{3}";
            var messageKey = "{4}";
            var returnToKey = "_return_to";
            var redirectTo = "{5}";
            var authUrl = "{6}";
            var signinUrl = "{7}";
            var returnToPathQueryStringKey = "{8}";
            var externalType = "{9}";
            var analyticsData = {10};
            if (analyticsData) {{
                signinUrl += "?analyticsData=" + encodeURIComponent(JSON.stringify(analyticsData));
            }}
            var params = paramsToObject(window.location.search);
            var keys = Object.keys(params);
            if (!params.error && !params.state) {{
                if (params[returnToPathQueryStringKey]) {{
                    sessionStorage.setItem(returnToKey, params[returnToPathQueryStringKey]);
                    delete params[returnToPathQueryStringKey];
                }} else {{
                    sessionStorage.removeItem(returnToKey);
                }}
                sessionStorage.setItem(stateKey, stateValue);
                sessionStorage.setItem(paramsKey, JSON.stringify(params));
                document.location = authUrl;
                return;
            }} else {{
                var state = sessionStorage.getItem(stateKey);
                params = Object.assign(params, JSON.parse(sessionStorage.getItem(paramsKey) == null ? {{}} : sessionStorage.getItem(paramsKey)));
                sessionStorage.removeItem(stateKey);
                var returnTo = sessionStorage.getItem(returnToKey) || redirectTo;
                if (!params.error && params.state !== state) {{
                    params.error = "Invalid state code received from external provider " + externalType + ".";
                }}
                if (params.error) {{
                    sessionStorage.setItem(statusKey, 503);
                    sessionStorage.setItem(messageKey, params.error);
                    document.location = returnTo;
                    return;
                }}
                var response = await fetch(signinUrl, {{
                    method: "POST",
                    headers: {{"Accept": "application/json","Content-Type": "application/json"}},
                    body: JSON.stringify(params)
                }});
                sessionStorage.setItem(statusKey, response.status);
                sessionStorage.setItem(messageKey, await response.text());
                document.location = returnTo;
            }}
        }})();
        </script>
        """;
}