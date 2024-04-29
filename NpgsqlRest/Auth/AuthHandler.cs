using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

using static NpgsqlRest.Auth.ClaimsDictionary;

namespace NpgsqlRest.Auth;

internal static class AuthHandler
{
    public static async Task HandleLoginAsync(
        NpgsqlCommand command,
        HttpContext context,
        RoutineEndpoint endpoint,
        Routine routine,
        NpgsqlRestOptions options,
        ILogger? logger)
    {
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync() is false)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        if (reader.FieldCount == 0)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        var authenticationType = options.AuthenticationOptions.DefaultAuthenticationType;
        string? scheme = null;
        string? message = null;
        var claims = new List<Claim>(10);

        for (int i = 0; i < routine.ColumnCount; i++)
        {
            var name1 = routine.ColumnNames[i];
            var name2 = endpoint.ColumnNames[i];
            var descriptor = routine.ColumnsTypeDescriptor[i];

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
                        logger?.WrongStatusType(routine.Type, routine.Schema, routine.Name);
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.CompleteAsync();
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
                logger?.LogError("Failed in constructing user identity for authentication.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }
            await result.ExecuteAsync(context);
        }
        if (message is not null)
        {
            await context.Response.WriteAsync(message);
        }
    }

    public static async Task HandleLogoutAsync(NpgsqlCommand command, Routine routine, HttpContext context)
    {
        if (routine.IsVoid)
        {
            await command.ExecuteNonQueryAsync();
            await Results.SignOut().ExecuteAsync(context);
            await context.Response.CompleteAsync();
            return;
        }

        List<string> schemes = new(5);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        while (await reader.ReadAsync())
        {
            for (int i = 0; i < reader?.FieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i) is true)
                {
                    continue;
                }

                var descriptor = routine.ColumnsTypeDescriptor[i];
                if (descriptor.IsArray)
                {
                    object[]? values = reader?.GetValue(i) as object[];
                    for (int j = 0; j < values?.Length; j++)
                    {
                        var value = values[j]?.ToString();
                        if (value is not null)
                        {
                            schemes.Add(value);
                        }
                    }
                }
                else
                {
                    string? value = reader?.GetValue(i)?.ToString();
                    if (value is not null)
                    {
                        schemes.Add(value);
                    }
                }
            }
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        await Results.SignOut(authenticationSchemes: schemes.Count == 0 ? null : schemes).ExecuteAsync(context);
        await context.Response.CompleteAsync();
    }
}