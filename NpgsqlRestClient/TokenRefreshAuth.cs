using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using NpgsqlRest;

namespace NpgsqlRestClient;

public class BearerTokenConfig
{
    public string? Scheme { get; set; }
    public string? RefreshPath { get; set; }
}

public static class TokenRefreshAuth
{
    public static void Configure(WebApplication app)
    {
        if (Builder.BearerTokenConfig is null || 
            string.IsNullOrEmpty(Builder.BearerTokenConfig.RefreshPath) is true || 
            string.IsNullOrEmpty(Builder.BearerTokenConfig.Scheme) is true)
        {
            return;
        }

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals(Builder.BearerTokenConfig.RefreshPath, StringComparison.OrdinalIgnoreCase) is false)
            {
                await next(context);
                return;
            }

            if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) is false)
            {
                await next(context);
                return;
            }

            var bearerTokenOptions = app.Services.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenOptions>>();
            var refreshTokenProtector = bearerTokenOptions.Get(Builder.BearerTokenConfig.Scheme).RefreshTokenProtector;
            var timeProvider = app.Services.GetRequiredService<TimeProvider>();

            string refreshToken;
            IResult result;

            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                var node = JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                refreshToken = node!["refresh"]?.ToString() ?? throw new ArgumentException("refresh token is null");
            }
            catch (Exception ex)
            {
                NpgsqlRestMiddleware.Logger?.LogError(ex, "Failed to read refresh token from request body.");
                result = Results.BadRequest(context.Response);
                await result.ExecuteAsync(context);
                return;
            }

            var refreshTicket = refreshTokenProtector.Unprotect(refreshToken);
            if (
                (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || timeProvider.GetUtcNow() >= expiresUtc) || 
                context.User.Identity?.IsAuthenticated is false)
            {
                result = Results.Challenge();
                await result.ExecuteAsync(context);
                return;
            }

            if (Results.SignIn(principal: context.User, authenticationScheme: Builder.BearerTokenConfig.Scheme) is not SignInHttpResult signInResult)
            {
                NpgsqlRestMiddleware.Logger?.LogError("Failed in constructing user identity for authentication.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }
            await signInResult.ExecuteAsync(context);
        });
    }
}
