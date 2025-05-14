using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using Npgsql;

namespace NpgsqlRest;

public class NpgsqlRestParameter : NpgsqlParameter
{
    public int Ordinal { get; init; }
    public string ConvertedName { get; init; } = default!;
    public string ActualName { get; init; } = default!;
    public ParamType ParamType { get; set; } = default!;
    public StringValues? QueryStringValues { get; set; } = null;
    public JsonNode? JsonBodyNode { get; set; } = null;
    public TypeDescriptor TypeDescriptor { get; init; } = default!;
    public NpgsqlRestParameter? HashOf { get; set; } = null;
    public bool UploadMetadata { get; set; } = false;

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

    public NpgsqlRestParameter NpgsqlRestParameterMemberwiseClone()
    {
#pragma warning disable CS8603 // Possible null reference return.
        return MemberwiseClone() as NpgsqlRestParameter;
#pragma warning restore CS8603 // Possible null reference return.
    }

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

    public static NpgsqlParameter CreateTextParam(string? value)
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