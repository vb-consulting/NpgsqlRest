using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using Npgsql;

namespace NpgsqlRest;

public class NpgsqlRestParameter : NpgsqlParameter
{
    public int Ordinal { get; private set; }
    public string ConvertedName { get; private set; }
    public string ActualName { get; private set; }
    public TypeDescriptor TypeDescriptor { get; init; }

    public ParamType ParamType { get; set; } = default!;
    public StringValues? QueryStringValues { get; set; } = null;
    public JsonNode? JsonBodyNode { get; set; } = null;
    public NpgsqlRestParameter? HashOf { get; set; } = null;

    public bool IsUploadMetadata { get; set; } = false;
    public bool IsUserId { get; set; } = false;
    public bool IsUserName { get; set; } = false;
    public bool IsUserRoles { get; set; } = false;
    public bool IsIpAddress { get; private set; } = false;
    public bool IsUserClaims { get; private set; } = false;

    public NpgsqlRestParameter(
        NpgsqlRestOptions options, 
        int ordinal, 
        string convertedName, 
        string actualName, 
        TypeDescriptor typeDescriptor)
    {
        Ordinal = ordinal;
        ConvertedName = convertedName;
        ActualName = actualName;
        TypeDescriptor = typeDescriptor;
        NpgsqlDbType = typeDescriptor.ActualDbType;

        if (string.Equals(options.AuthenticationOptions.UserIdParameterName, actualName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.AuthenticationOptions.UserIdParameterName, convertedName, StringComparison.OrdinalIgnoreCase))
        {
            IsUserId = true;
        }
        if (string.Equals(options.AuthenticationOptions.UserNameParameterName, actualName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.AuthenticationOptions.UserNameParameterName, convertedName, StringComparison.OrdinalIgnoreCase))
        {
            IsUserName = true;
        }
        if (string.Equals(options.AuthenticationOptions.UserRolesParameterName, actualName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.AuthenticationOptions.UserRolesParameterName, convertedName, StringComparison.OrdinalIgnoreCase))
        {
            IsUserRoles = true;
        }
        if (string.Equals(options.AuthenticationOptions.IpAddressParameterName, actualName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.AuthenticationOptions.IpAddressParameterName, convertedName, StringComparison.OrdinalIgnoreCase))
        {
            IsIpAddress = true;
        }
        if (string.Equals(options.AuthenticationOptions.UserClaimsParameterName, actualName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.AuthenticationOptions.UserClaimsParameterName, convertedName, StringComparison.OrdinalIgnoreCase))
        {
            IsUserClaims = true;
        }

        if (options.UploadOptions.UseDefaultUploadMetadataParameter is true)
        {
            if (string.Equals(options.UploadOptions.DefaultUploadMetadataParameterName, actualName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(options.UploadOptions.DefaultUploadMetadataParameterName, convertedName, StringComparison.OrdinalIgnoreCase))
            {
                IsUploadMetadata = true;
            }
        }
    }

    internal string GetCacheStringValue()
    {
        if (Value == DBNull.Value)
        {
            return string.Empty;
        }
        if (TypeDescriptor.IsArray)
        {
            return string.Join(",", Value as object[] ?? []);
        }
        return Value?.ToString() ?? string.Empty;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
    public NpgsqlRestParameter NpgsqlRestParameterMemberwiseClone() => MemberwiseClone() as NpgsqlRestParameter;
    private NpgsqlRestParameter() { }
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private static readonly NpgsqlRestParameter _textParam = new()
    {
        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
        Value = DBNull.Value,
    };

    public static NpgsqlParameter CreateParamWithType(NpgsqlTypes.NpgsqlDbType type)
    {
        var result = _textParam.NpgsqlRestParameterMemberwiseClone();
        result.NpgsqlDbType = type;
        return result;
    }

    public static NpgsqlParameter CreateTextParam(object? value)
    {
        var result = _textParam.NpgsqlRestParameterMemberwiseClone();
        if (value is null)
        {
            return result;
        }
        result.Value = value;
        return result;
    }
}