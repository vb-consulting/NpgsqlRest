using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using Npgsql;

namespace NpgsqlRest;

public readonly struct ParameterValidationValues(
    HttpContext context, 
    Routine routine,
    NpgsqlRestParameter parameter,
    string paramName,
    TypeDescriptor typeDescriptor,
    RequestParamType requestParamType,
    StringValues? queryStringValues,
    JsonNode? jsonBodyNode)
{
    /// <summary>
    /// Current HttpContext.
    /// </summary>
    public readonly HttpContext Context = context;
    /// <summary>
    /// Current Routine.
    /// </summary>
    public readonly Routine Routine = routine;
    /// <summary>
    /// Parameter to be validated. Note: if parameter is using default value and value not provided, parameter.Value is null.
    /// </summary>
    public readonly NpgsqlParameter Parameter = parameter;
    /// <summary>
    /// Current parameter name (converted).
    /// </summary>
    public readonly string ParamName = paramName;
    /// <summary>
    /// Current parameter type descriptor (additional type information)
    /// </summary>
    public readonly TypeDescriptor TypeDescriptor = typeDescriptor;
    /// <summary>
    /// Current parameter position Query String or JSON Body.
    /// </summary>
    public readonly RequestParamType RequestParamType = requestParamType;
    /// <summary>
    /// Current parameter values from Query String.
    /// NULL if parameter is not from Query String or Query String is not provided and parameter has default.
    /// </summary>
    public readonly StringValues? QueryStringValues = queryStringValues;
    /// <summary>
    /// Current parameter values from JSON Body.
    /// NULL if parameter is not from JSON Body or JSON Body is not provided and parameter has default.
    /// </summary>
    public readonly JsonNode? JsonBodyNode = jsonBodyNode;
}
