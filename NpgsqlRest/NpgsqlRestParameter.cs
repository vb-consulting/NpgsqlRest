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

    public NpgsqlRestParameter NpgsqlResMemberwiseClone()
    {
#pragma warning disable CS8603 // Possible null reference return.
        return MemberwiseClone() as NpgsqlRestParameter;
#pragma warning restore CS8603 // Possible null reference return.
    }
}