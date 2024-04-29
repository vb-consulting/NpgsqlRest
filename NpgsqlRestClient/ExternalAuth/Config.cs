using static NpgsqlRestClient.Config;
using static NpgsqlRestClient.Builder;

namespace NpgsqlRestClient.ExternalAuth;

public class ClientConfig
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

public static class Config
{
    public static string BrowserSessionStatusKey { get; private set; } = default!;
    public static string BrowserSessionMessageKey { get; private set; } = default!;
    public static string SignInHtmlTemplate { get; private set; } = default!;
    public static string? RedirectUrl { get; private set; } = null;
    public static string ReturnToPath { get; private set; } = default!;
    public static string ReturnToPathQueryStringKey { get; private set; } = default!;
    public static string LoginCommand { get; private set; } = default!;
    public static Dictionary<string, ClientConfig> ClientConfigs { get; private set; } = new();

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
