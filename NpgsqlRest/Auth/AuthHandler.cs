using System;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace NpgsqlRest.Auth;

public static class AuthHandler
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
        var opts = options.AuthenticationOptions;
        List<Claim> claims = new(10);
        var verificationPerformed = false;
        var verificationFailed = false;

        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
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
                var colName = routine.OriginalColumnNames[i];
                
                TypeDescriptor descriptor = routine.ColumnsTypeDescriptor[i];

                if (opts.StatusColumnName is not null)
                {
                    if (string.Equals(colName, opts.StatusColumnName, StringComparison.OrdinalIgnoreCase))
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

                if (opts.SchemeColumnName is not null)
                {
                    if (string.Equals(colName, opts.SchemeColumnName, StringComparison.OrdinalIgnoreCase))
                    {
                        scheme = reader?.GetValue(i).ToString();
                        continue;
                    }
                }

                if (opts.MessageColumnName is not null)
                {
                    if (string.Equals(colName, opts.MessageColumnName, StringComparison.OrdinalIgnoreCase))
                    {
                        message = reader?.GetValue(i).ToString();
                        continue;
                    }
                }
                var (userNameCurrent, userIdCurrent) = AddClaimFromReader(opts, i, descriptor, reader!, claims, colName);
                if (userNameCurrent is not null)
                {
                    userName = userNameCurrent;
                }
                if (userIdCurrent is not null)
                {
                    userId = userIdCurrent;
                }
            }

            // hash verification last
            if (opts.HashColumnName is not null &&
                opts.PasswordHasher is not null &&
                opts.PasswordParameterNameContains is not null)
            {
                for (int i = 0; i < routine.ColumnCount; i++)
                {
                    var name1 = routine.OriginalColumnNames[i];
                    var name2 = routine.ColumnNames[i];
                    if (string.Equals(name1, opts.HashColumnName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name2, opts.HashColumnName, StringComparison.OrdinalIgnoreCase))
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
                                    if (name is not null && name.Contains(opts.PasswordParameterNameContains, // found password parameter
                                        StringComparison.OrdinalIgnoreCase))
                                    {
                                        foundPasswordParameter = true;
                                        var pass = parameter?.Value?.ToString();
                                        if (pass is not null && parameter?.Value != DBNull.Value)
                                        {
                                            verificationPerformed = true;
                                            if (opts.PasswordHasher?.VerifyHashedPassword(hash, pass) is false)
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
                                        opts.PasswordParameterNameContains);
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
                if (string.IsNullOrEmpty(opts.PasswordVerificationFailedCommand) is false)
                {
                    using var failedCommand = connection?.CreateCommand();
                    if (failedCommand is not null)
                    {
                        failedCommand.CommandText = opts.PasswordVerificationFailedCommand;
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
                if (string.IsNullOrEmpty(opts.PasswordVerificationSucceededCommand) is false)
                {
                    using var succeededCommand = connection?.CreateCommand();
                    if (succeededCommand is not null)
                    {
                        succeededCommand.CommandText = opts.PasswordVerificationSucceededCommand;
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
                scheme ?? opts.DefaultAuthenticationType,
                nameType: opts.DefaultNameClaimType,
                roleType: opts.DefaultRoleClaimType));

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

    public static (string? userName, string? userId) AddClaimFromReader(
        NpgsqlRestAuthenticationOptions options,
        int i,
        TypeDescriptor descriptor,
        NpgsqlDataReader reader,
        List<Claim> claims, 
        string colName)
    {
        if (string.Equals(colName, options.HashColumnName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(colName, options.MessageColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        string? claimType;
        string? userName = null;
        string? userId = null;

        claimType = colName;

        if (reader?.IsDBNull(i) is true)
        {
            claims.Add(new Claim(claimType, ""));
            if (string.Equals(claimType, options.DefaultNameClaimType, StringComparison.Ordinal))
            {
                userName = null;
            }
            else if (string.Equals(claimType, options.DefaultUserIdClaimType, StringComparison.Ordinal))
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
            if (string.Equals(claimType, options.DefaultNameClaimType, StringComparison.Ordinal))
            {
                userName = value;
            }
            else if (string.Equals(claimType, options.DefaultUserIdClaimType, StringComparison.Ordinal))
            {
                userId = value;
            }
        }

        return (userName, userId);
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