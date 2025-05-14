using System.Security.Claims;
using System.Text;
using Npgsql;

namespace NpgsqlRest.Extensions;

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

    public static string? GetUserId(this ClaimsPrincipal user)
    {
        foreach (var claim in user.Claims)
        {
            if (claim.IsTypeOf(ClaimTypes.NameIdentifier))
            {
                return claim.Value;
            }
        }
        return null;
    }

    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.Identity?.Name;
    }

    public static string? GetUserRoles(this ClaimsPrincipal user, NpgsqlRestOptions options)
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
                sb.Append(claim.Value);
                i++;
            }
        }
        sb.Append('}');
        return sb.ToString();
    }
}