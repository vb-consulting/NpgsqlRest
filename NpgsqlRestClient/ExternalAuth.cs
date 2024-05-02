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
    public string ClientId { get; init; } = default!;
    public string ClientSecret { get; init; } = default!;
    public string AuthUrl { get; init; } = default!;
    public string TokenUrl { get; init; } = default!;
    public string InfoUrl { get; init; } = default!;
    public string? EmailUrl { get; init; }
    public string SigninUrl { get; init; } = default!;
    public string ExternalType { get; init; } = default!;
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
    public static Dictionary<string, ExternalAuthClientConfig> ClientConfigs { get; private set; } = new();

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
            var signinUrl = string.Format(signinUrlTemplate, section.Key.ToLowerInvariant());
            ClientConfigs.Add(signinUrl, new()
            {
                ClientId = GetConfigStr("ClientId", section) ?? throw new ArgumentException($"ClientId can not be null. Auth config section: {section.Key}"),
                ClientSecret = GetConfigStr("ClientSecret", section) ?? throw new ArgumentException($"ClientSecret can not be null. Auth config section: {section.Key}"),
                AuthUrl = GetConfigStr("AuthUrl", section) ?? throw new ArgumentException($"AuthUrl can not be null. Auth config section: {section.Key}"),
                TokenUrl = GetConfigStr("TokenUrl", section) ?? throw new ArgumentException($"TokenUrl can not be null. Auth config section: {section.Key}"),
                InfoUrl = GetConfigStr("InfoUrl", section) ?? throw new ArgumentException($"InfoUrl can not be null. Auth config section: {section.Key}"),
                EmailUrl = GetConfigStr("EmailUrl", section),
                SigninUrl = signinUrl,
                ExternalType = section.Key
            });
            Logger?.Information("External login available for {0} available on path: {1}", section.Key, signinUrl);
        }

        if (ClientConfigs.Count == 0)
        {
            return;
        }

        BrowserSessionStatusKey = GetConfigStr("BrowserSessionStatusKey", externalCfg) ?? "__external_status";
        BrowserSessionMessageKey = GetConfigStr("BrowserSessionMessageKey", externalCfg) ?? "__external_message";

        SignInHtmlTemplate = GetConfigStr("SignInHtmlTemplate", externalCfg) ?? "<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>Talking To {0}</title></head><body>Loading...{1}</body></html>";
        RedirectUrl = GetConfigStr("RedirectUrl", externalCfg);
        ReturnToPath = GetConfigStr("ReturnToPath", externalCfg) ?? "/";
        ReturnToPathQueryStringKey = GetConfigStr("ReturnToPathQueryStringKey", externalCfg) ?? "return_to";
        LoginCommand = GetConfigStr("LoginCommand", externalCfg) ?? "select * from auth.login($1, $2)";
    }
}

public static class ExternalAuth
{
    public static void Configure(WebApplication app, NpgsqlRestOptions options)
    {
        if (ExternalAuthConfig.ClientConfigs.Count == 0)
        {
            return;
        }

        app.Use(async (context, next) =>
        {
            if (ExternalAuthConfig.ClientConfigs.TryGetValue(context.Request.Path, out var config) is false)
            {
                await next(context);
                return;
            }

            if (string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                PrepareResponse(Text.Html);
                await context.Response.WriteAsync(BuildHtmlTemplate(config, context.Request));
                await context.Response.CompleteAsync();
                return;
            }

            if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                PrepareResponse(Text.Plain);
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

            void PrepareResponse(string contentType)
            {
                context.Response.ContentType = contentType;
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, proxy-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";
            }
        });
    }

    private static readonly HttpClient _httpClient = new();
    private static readonly string _agent = $"{Guid.NewGuid().ToString()[..8]}";

    private const string browserSessionStateKey = "__external_state";
    private const string browserSessionParamsKey = "__external_params";

    private static async Task ProcessAsync(string code, ExternalAuthClientConfig config, HttpContext context, NpgsqlRestOptions options)
    {
        string? email;
        string? name;
        string token;

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
            JsonNode node = JsonNode.Parse(infoContent) ??
                throw new ArgumentException("info json node is null");

            email = node["email"]?.ToString() ??
                node?["elements"]?[0]?["handle~"]?["emailAddress"]?.ToString(); // linkedin format

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            name = node["localizedLastName"] is not null ?
                $"{node["localizedFirstName"]} {node["localizedLastName"]}".Trim() : // linkedin format
                node["name"]?.ToString(); // normal format
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

        using var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = ExternalAuthConfig.LoginCommand;
        command.Parameters.Add(new NpgsqlParameter()
        {
            Value = email,
        });
        command.Parameters.Add(new NpgsqlParameter()
        {
            Value = name is not null ? name : DBNull.Value
        });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync() is false || reader.FieldCount == 0)
        {
            await CompleteWithErrorAsync("Login command did not return any data.", HttpStatusCode.NotFound, LogEventLevel.Warning);
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
                            HttpStatusCode.InternalServerError, LogEventLevel.Error);
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
                    HttpStatusCode.InternalServerError, LogEventLevel.Error);
                return;
            }
            await result.ExecuteAsync(context);
        }

        if (message is not null)
        {
            await context.Response.WriteAsync(message);
        }
        await context.Response.CompleteAsync();

        async Task CompleteWithErrorAsync(string text, HttpStatusCode code, LogEventLevel? logLevel = null)
        {
            if (logLevel.HasValue)
            {
                Logger?.Write(logLevel.Value, text);
            }
            context.Response.StatusCode = (int)code;
            await context.Response.WriteAsync(text);
            await context.Response.CompleteAsync();
        }
    }

    private static string BuildHtmlTemplate(ExternalAuthClientConfig config, HttpRequest request)
    {
        string redirectUrl = GetRedirectUrl(config, request);
        var state = Guid.NewGuid().ToString();
        var redirectTo = "/";
        var authUrl = string.Format(config.AuthUrl, config.ClientId, redirectUrl, state);
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
                config.ExternalType//9
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
            var params = paramsToObject(window.location.search);
            var keys = Object.keys(params);
            if (keys.length==0 || (keys.length==1 && params[returnToPathQueryStringKey])) {{
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