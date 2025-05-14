using System.Net;
using System.Security.Claims;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

using static NpgsqlRest.Auth.ClaimsDictionary;

namespace NpgsqlRest.Auth;

internal static class AuthHandler
{
    public static async Task HandleLoginAsync(
        NpgsqlCommand command,
        HttpContext context,
        Routine routine,
        NpgsqlRestOptions options,
        ILogger? logger)
    {
        var connection = command.Connection;
        string? scheme = null;
        string? message = null;
        string? userId = null;
        string? userName = null;
        var authenticationType = options.AuthenticationOptions.DefaultAuthenticationType;
        var claims = new List<Claim>(10);
        var verificationPerformed = false;
        var verificationFailed = false;


        await using (var reader = await command.ExecuteReaderAsync())
        {
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

            for (int i = 0; i < routine.ColumnCount; i++)
            {
                var name1 = routine.OriginalColumnNames[i];
                var name2 = routine.ColumnNames[i];
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
                            logger?.WrongStatusType(string.Concat(routine.Type, " ", routine.Schema, ".", routine.Name));
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
                            claimType = name1;
                        }
                    }
                }
                else
                {
                    claimType = name1;
                }

                if (reader?.IsDBNull(i) is true)
                {
                    claims.Add(new Claim(claimType, ""));
                    if (string.Equals(claimType, options.AuthenticationOptions.DefaultNameClaimType, StringComparison.Ordinal))
                    {
                        userName = null;
                    }
                    else if (string.Equals(claimType, ClaimTypes.NameIdentifier, StringComparison.Ordinal))
                    {
                        userId = null;
                    }
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
                    if (string.Equals(claimType, options.AuthenticationOptions.DefaultNameClaimType, StringComparison.Ordinal))
                    {
                        userName = value;
                    }
                    else if (string.Equals(claimType, ClaimTypes.NameIdentifier, StringComparison.Ordinal))
                    {
                        userId = value;
                    }
                }
            }

            // hash verification last
            if (options.AuthenticationOptions.HashColumnName is not null &&
                options.AuthenticationOptions.PasswordHasher is not null &&
                options.AuthenticationOptions.PasswordParameterNameContains is not null)
            {
                for (int i = 0; i < routine.ColumnCount; i++)
                {
                    var name1 = routine.OriginalColumnNames[i];
                    var name2 = routine.ColumnNames[i];
                    if (string.Equals(name1, options.AuthenticationOptions.HashColumnName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name2, options.AuthenticationOptions.HashColumnName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader?.IsDBNull(i) is false)
                        {
                            var hash = reader?.GetValue(i).ToString();
                            if (hash is not null)
                            {
                                var endpoint = string.Concat(context.Request.Method.ToString(), " ", context.Request.Path.ToString());
                                // find the password parameter
                                var foundPasswordParameter = false;
                                for (var j = 0; j < command.Parameters.Count; j++)
                                {
                                    var parameter = command.Parameters[j];
                                    var name = (parameter as NpgsqlRestParameter)?.ActualName;
                                    if (name is not null && name.Contains(options.AuthenticationOptions.PasswordParameterNameContains, // found password parameter
                                        StringComparison.OrdinalIgnoreCase))
                                    {
                                        foundPasswordParameter = true;
                                        var pass = parameter?.Value?.ToString();
                                        if (pass is not null && parameter?.Value != DBNull.Value)
                                        {
                                            verificationPerformed = true;
                                            if (options.AuthenticationOptions.PasswordHasher?.VerifyHashedPassword(hash, pass) is false)
                                            {
                                                logger?.VerifyPasswordFailed(endpoint, userId, userName);
                                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                                await context.Response.CompleteAsync();
                                                verificationFailed = true;
                                            }
                                        }
                                        break;
                                    }
                                }
                                if (foundPasswordParameter is false)
                                {
                                    logger?.CantFindPasswordParameter(endpoint,
                                        command.Parameters.Select(p => (p as NpgsqlRestParameter)?.ActualName)?.ToArray(),
                                        options.AuthenticationOptions.PasswordParameterNameContains);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (verificationPerformed is true)
        {
            if (verificationFailed is true)
            {
                if (string.IsNullOrEmpty(options.AuthenticationOptions.PasswordVerificationFailedCommand) is false)
                {
                    using var failedCommand = connection?.CreateCommand();
                    if (failedCommand is not null)
                    {
                        failedCommand.CommandText = options.AuthenticationOptions.PasswordVerificationFailedCommand;
                        var paramCount = failedCommand.CommandText.PgCountParams();
                        if (paramCount >= 1)
                        {
                            failedCommand.Parameters.Add(NpgsqlRestParameter.CreateTextParam(scheme));
                        }
                        if (paramCount >= 2)
                        {
                            failedCommand.Parameters.Add(NpgsqlRestParameter.CreateTextParam(userId));
                        }
                        if (paramCount >= 3)
                        {
                            failedCommand.Parameters.Add(NpgsqlRestParameter.CreateTextParam(userName));
                        }
                        await failedCommand.ExecuteNonQueryAsync();
                    }
                }
                return;
            }
            else
            {
                if (string.IsNullOrEmpty(options.AuthenticationOptions.PasswordVerificationSucceededCommand) is false)
                {
                    using var succeededCommand = connection?.CreateCommand();
                    if (succeededCommand is not null)
                    {
                        succeededCommand.CommandText = options.AuthenticationOptions.PasswordVerificationSucceededCommand;
                        var paramCount = succeededCommand.CommandText.PgCountParams();

                        if (paramCount >= 1)
                        {
                            succeededCommand.Parameters.Add(NpgsqlRestParameter.CreateTextParam(scheme));
                        }
                        if (paramCount >= 2)
                        {
                            succeededCommand.Parameters.Add(NpgsqlRestParameter.CreateTextParam(userId));
                        }
                        if (paramCount >= 3)
                        {
                            succeededCommand.Parameters.Add(NpgsqlRestParameter.CreateTextParam(userName));
                        }
                        await succeededCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        if (context.Response.StatusCode == (int)HttpStatusCode.OK)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                claims, 
                scheme ?? authenticationType,
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