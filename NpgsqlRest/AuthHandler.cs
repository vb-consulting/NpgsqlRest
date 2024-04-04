using System.Collections.Frozen;
using System.Net;
using System.Security.AccessControl;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace NpgsqlRest;

internal static class AuthHandler
{
    //var names = string.Join($",{Environment.NewLine}", typeof(ClaimTypes)
    //  .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
    //  .Select(f => f.Name)
    //  .Order()
    //  .Select(n => $"{{ nameof(System.Security.Claims.ClaimTypes.{n}).ToLowerInvariant(), System.Security.Claims.ClaimTypes.{n} }}"));
    private static readonly FrozenDictionary<string, string> ClaimTypes = new Dictionary<string, string>()
    {
        { nameof(System.Security.Claims.ClaimTypes.Actor).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Actor },
        { nameof(System.Security.Claims.ClaimTypes.Anonymous).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Anonymous },
        { nameof(System.Security.Claims.ClaimTypes.Authentication).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Authentication },
        { nameof(System.Security.Claims.ClaimTypes.AuthenticationInstant).ToLowerInvariant(), System.Security.Claims.ClaimTypes.AuthenticationInstant },
        { nameof(System.Security.Claims.ClaimTypes.AuthenticationMethod).ToLowerInvariant(), System.Security.Claims.ClaimTypes.AuthenticationMethod },
        { nameof(System.Security.Claims.ClaimTypes.AuthorizationDecision).ToLowerInvariant(), System.Security.Claims.ClaimTypes.AuthorizationDecision },
        { nameof(System.Security.Claims.ClaimTypes.CookiePath).ToLowerInvariant(), System.Security.Claims.ClaimTypes.CookiePath },
        { nameof(System.Security.Claims.ClaimTypes.Country).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Country },
        { nameof(System.Security.Claims.ClaimTypes.DateOfBirth).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DateOfBirth },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlyPrimaryGroupSid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlyPrimaryGroupSid },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlyPrimarySid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlyPrimarySid },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlySid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlySid },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlyWindowsDeviceGroup).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlyWindowsDeviceGroup },
        { nameof(System.Security.Claims.ClaimTypes.Dns).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Dns },
        { nameof(System.Security.Claims.ClaimTypes.Dsa).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Dsa },
        { nameof(System.Security.Claims.ClaimTypes.Email).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Email },
        { nameof(System.Security.Claims.ClaimTypes.Expiration).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Expiration },
        { nameof(System.Security.Claims.ClaimTypes.Expired).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Expired },
        { nameof(System.Security.Claims.ClaimTypes.Gender).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Gender },
        { nameof(System.Security.Claims.ClaimTypes.GivenName).ToLowerInvariant(), System.Security.Claims.ClaimTypes.GivenName },
        { nameof(System.Security.Claims.ClaimTypes.GroupSid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.GroupSid },
        { nameof(System.Security.Claims.ClaimTypes.Hash).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Hash },
        { nameof(System.Security.Claims.ClaimTypes.HomePhone).ToLowerInvariant(), System.Security.Claims.ClaimTypes.HomePhone },
        { nameof(System.Security.Claims.ClaimTypes.IsPersistent).ToLowerInvariant(), System.Security.Claims.ClaimTypes.IsPersistent },
        { nameof(System.Security.Claims.ClaimTypes.Locality).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Locality },
        { nameof(System.Security.Claims.ClaimTypes.MobilePhone).ToLowerInvariant(), System.Security.Claims.ClaimTypes.MobilePhone },
        { nameof(System.Security.Claims.ClaimTypes.Name).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Name },
        { nameof(System.Security.Claims.ClaimTypes.NameIdentifier).ToLowerInvariant(), System.Security.Claims.ClaimTypes.NameIdentifier },
        { nameof(System.Security.Claims.ClaimTypes.OtherPhone).ToLowerInvariant(), System.Security.Claims.ClaimTypes.OtherPhone },
        { nameof(System.Security.Claims.ClaimTypes.PostalCode).ToLowerInvariant(), System.Security.Claims.ClaimTypes.PostalCode },
        { nameof(System.Security.Claims.ClaimTypes.PrimaryGroupSid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.PrimaryGroupSid },
        { nameof(System.Security.Claims.ClaimTypes.PrimarySid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.PrimarySid },
        { nameof(System.Security.Claims.ClaimTypes.Role).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Role },
        { nameof(System.Security.Claims.ClaimTypes.Rsa).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Rsa },
        { nameof(System.Security.Claims.ClaimTypes.SerialNumber).ToLowerInvariant(), System.Security.Claims.ClaimTypes.SerialNumber },
        { nameof(System.Security.Claims.ClaimTypes.Sid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Sid },
        { nameof(System.Security.Claims.ClaimTypes.Spn).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Spn },
        { nameof(System.Security.Claims.ClaimTypes.StateOrProvince).ToLowerInvariant(), System.Security.Claims.ClaimTypes.StateOrProvince },
        { nameof(System.Security.Claims.ClaimTypes.StreetAddress).ToLowerInvariant(), System.Security.Claims.ClaimTypes.StreetAddress },
        { nameof(System.Security.Claims.ClaimTypes.Surname).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Surname },
        { nameof(System.Security.Claims.ClaimTypes.System).ToLowerInvariant(), System.Security.Claims.ClaimTypes.System },
        { nameof(System.Security.Claims.ClaimTypes.Thumbprint).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Thumbprint },
        { nameof(System.Security.Claims.ClaimTypes.Upn).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Upn },
        { nameof(System.Security.Claims.ClaimTypes.Uri).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Uri },
        { nameof(System.Security.Claims.ClaimTypes.UserData).ToLowerInvariant(), System.Security.Claims.ClaimTypes.UserData },
        { nameof(System.Security.Claims.ClaimTypes.Version).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Version },
        { nameof(System.Security.Claims.ClaimTypes.Webpage).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Webpage },
        { nameof(System.Security.Claims.ClaimTypes.WindowsAccountName).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsAccountName },
        { nameof(System.Security.Claims.ClaimTypes.WindowsDeviceClaim).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsDeviceClaim },
        { nameof(System.Security.Claims.ClaimTypes.WindowsDeviceGroup).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsDeviceGroup },
        { nameof(System.Security.Claims.ClaimTypes.WindowsFqbnVersion).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsFqbnVersion },
        { nameof(System.Security.Claims.ClaimTypes.WindowsSubAuthority).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsSubAuthority },
        { nameof(System.Security.Claims.ClaimTypes.WindowsUserClaim).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsUserClaim },
        { nameof(System.Security.Claims.ClaimTypes.X500DistinguishedName).ToLowerInvariant(), System.Security.Claims.ClaimTypes.X500DistinguishedName }
    }.ToFrozenDictionary();

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
            await context.Response.CompleteAsync();
            return;
        }

        if (reader.FieldCount == 0)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.CompleteAsync();
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
                            await context.Response.CompleteAsync();
                            return;
                        }
                    }
                    else if (descriptor.IsNumeric)
                    {
                        var status = reader?.GetInt32(i) ?? 200;
                        if (status != (int)HttpStatusCode.OK)
                        {
                            context.Response.StatusCode = status;
                            await context.Response.CompleteAsync();
                            return;
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

            if (options.AuthenticationOptions.SchemaColumnName is not null)
            {
                if (string.Equals(name1, options.AuthenticationOptions.SchemaColumnName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name2, options.AuthenticationOptions.SchemaColumnName, StringComparison.OrdinalIgnoreCase))
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
                if (ClaimTypes.TryGetValue(name1.ToLowerInvariant(), out claimType) is false)
                {
                    if (ClaimTypes.TryGetValue(name2.ToLowerInvariant(), out claimType) is false)
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

        var result = Results.SignIn(
            principal:
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        claims,
                        scheme ?? authenticationType,
                        nameType: options.AuthenticationOptions.DefaultNameClaimType,
                        roleType: options.AuthenticationOptions.DefaultRoleClaimType)),
            authenticationScheme: scheme
        ) as SignInHttpResult;

        if (result is null) 
        {
            logger?.LogError("Failed in constructing user identity for authentication.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.CompleteAsync();
            return;
        }
        await result.ExecuteAsync(context);
        if (message is not null)
        {
            await context.Response.WriteAsync(message);
        }
        await context.Response.CompleteAsync();
    }

    public static async Task HandleLogoutAsync(NpgsqlCommand command, Routine routine, HttpContext context)
    {
        if (routine.IsVoid)
        {
            await command.ExecuteNonQueryAsync();
            await Results.SignOut().ExecuteAsync(context);
            await context.Response.CompleteAsync();
        }

        List<string> schemes = new(5);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        while (await reader.ReadAsync())
        {
            for(int i = 0;  i < reader?.FieldCount; i++) 
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