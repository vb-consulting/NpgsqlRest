﻿using System.Security.Claims;
using System.Text;
using Npgsql;

namespace NpgsqlRest;

public static class Ext
{
    public static T Get<T>(this NpgsqlDataReader reader, int ordinal)
    {
        object? value;
        if (typeof(T) == typeof(short?[]))
        {
            value = reader.GetFieldValue<short?[]>(ordinal);
        } 
        else
        {
            value = reader[ordinal];
        }

        if (value == DBNull.Value)
        {
            return default!;
        }

        // strange bug single char representing as string on older pg versions when using functions
        if (typeof(T) == typeof(char) && value.GetType() == typeof(string))
        {
            if (value is null)
            {
                return default!;
            }
            object c = ((string)value)[0];
            return (T)c;
        }
        return (T)value;
    }

    public static T GetEnum<T>(this string? value) where T : struct
    {
        Enum.TryParse<T>(value, true, out var result);
        // return the first enum (Other) when no match
        return result;
    }

    public static bool IsTypeOf(this Claim claim, string type)
    {
        return string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase);
    }

    public static object GetUserIdDbParam(this ClaimsPrincipal user, NpgsqlRestOptions options)
    {
        foreach (var claim in user.Claims)
        {
            if (claim.IsTypeOf(options.AuthenticationOptions.DefaultUserIdClaimType))
            {
                return claim.Value;
            }
        }
        return DBNull.Value;
    }

    public static object GetUserNameDbParam(this ClaimsPrincipal user, NpgsqlRestOptions options)
    {
        if (user.Identity is null)
        {
            return DBNull.Value;
        }
        if (user.Identity.Name is null)
        {
            return DBNull.Value;
        }
        if (options.AuthenticationOptions.UsingDefaultNameClaimType)
        {
            return user.Identity.Name;
        }
        foreach (var claim in user.Claims)
        {
            if (claim.IsTypeOf(options.AuthenticationOptions.DefaultNameClaimType))
            {
                return claim.Value;
            }
        }
        return DBNull.Value;
    }

    public static object GetUserRolesTextDbParam(this ClaimsPrincipal user, NpgsqlRestOptions options)
    {
        StringBuilder sb = new(100);
        sb.Append('{');
        int i = 0;
        foreach (var claim in user.Claims)
        {
            if (claim.IsTypeOf(options.AuthenticationOptions.DefaultRoleClaimType))
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(PgConverters.QuoteText(claim.Value.AsSpan()));
                i++;
            }
        }
        sb.Append('}');
        return sb.ToString();
    }

    public static object GetUserRolesDbParam(this ClaimsPrincipal user, NpgsqlRestOptions options)
    {
        List<string> roles = new(10);
        foreach (var claim in user.Claims)
        {
            if (claim.IsTypeOf(options.AuthenticationOptions.DefaultRoleClaimType))
            {
                roles.Add(claim.Value);
            }
        }
        return roles.ToArray();
    }

    public static object GetUserClaimsDbParam(this ClaimsPrincipal user)
    {
        var claims = user.Claims;
        if (!claims.Any())
        {
            return "{}";
        }
        Dictionary<string, object> claimValues = [];
        foreach (var claim in claims)
        {
            if (claimValues.TryGetValue(claim.Type, out var existing))
            {
                if (existing is List<string> list)
                {
                    list.Add(claim.Value);
                }
                else
                {
                    var newList = new List<string>(4) { (string)existing, claim.Value };
                    claimValues[claim.Type] = newList;
                }
            }
            else
            {
                claimValues[claim.Type] = claim.Value;
            }
        }
        int estimatedCapacity = 2 + (claimValues.Count * 10);
        foreach (var entry in claimValues)
        {
            estimatedCapacity += entry.Key.Length * 2;

            if (entry.Value is List<string> list)
            {
                estimatedCapacity += 2;
                foreach (var value in list)
                {
                    estimatedCapacity += value.Length * 2 + 3; 
                }
            }
            else
            {
                estimatedCapacity += ((string)entry.Value).Length * 2 + 2;
            }
        }
        StringBuilder sb = new(estimatedCapacity);
        sb.Append('{');
        int i = 0;
        foreach (var entry in claimValues)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(PgConverters.SerializeString(entry.Key));
            sb.Append(':');
            if (entry.Value is List<string> values)
            {
                sb.Append('[');
                for (int j = 0; j < values.Count; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(PgConverters.SerializeString(values[j]));
                }
                sb.Append(']');
            }
            else
            {
                sb.Append(PgConverters.SerializeString((string)entry.Value));
            }
            i++;
        }
        sb.Append('}');
        return sb.ToString();
    }

    public static string? GetClientIpAddress(this HttpRequest request)
    {
        // Check X-Forwarded-For header
        var forwardedIp = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedIp))
        {
            int commaIndex = forwardedIp.IndexOf(',');
            return commaIndex > 0 ? forwardedIp[..commaIndex].Trim() : forwardedIp.Trim();
        }

        // Check other headers with null-coalescing operator
        var ip = request.Headers["X-Real-IP"].FirstOrDefault()
              ?? request.Headers["HTTP_X_FORWARDED_FOR"].FirstOrDefault()
              ?? request.Headers["REMOTE_ADDR"].FirstOrDefault();

        return !string.IsNullOrEmpty(ip) ? ip : request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    public static object GetClientIpAddressDbParam(this HttpRequest request)
    {
        return request.GetClientIpAddress() as object ?? DBNull.Value;
    }
}