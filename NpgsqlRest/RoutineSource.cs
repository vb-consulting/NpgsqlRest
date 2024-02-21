using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace NpgsqlRest;

public class RoutineSource(
        string? schemaSimilarTo = null,
        string? schemaNotSimilarTo = null,
        string[]? includeSchemas = null,
        string[]? excludeSchemas = null,
        string? nameSimilarTo = null,
        string? nameNotSimilarTo = null,
        string[]? includeNames = null,
        string[]? excludeNames = null,
        string? query = null) : IRoutineSource
{
    private readonly IRoutineSourceParameterFormatter _formatter = new RoutineSourceParameterFormatter();
    public string? SchemaSimilarTo { get; init; } = schemaSimilarTo;
    public string? SchemaNotSimilarTo { get; init; } = schemaNotSimilarTo;
    public string[]? IncludeSchemas { get; init; } = includeSchemas;
    public string[]? ExcludeSchemas { get; init; } = excludeSchemas;
    public string? NameSimilarTo { get; init; } = nameSimilarTo;
    public string? NameNotSimilarTo { get; init; } = nameNotSimilarTo;
    public string[]? IncludeNames { get; init; } = includeNames;
    public string[]? ExcludeNames { get; init; } = excludeNames;
    public string Query { get; init; } = query ?? RoutineSourceQuery.Query;

    public IEnumerable<(Routine, IRoutineSourceParameterFormatter)> Read(NpgsqlRestOptions options)
    {
        using var connection = new NpgsqlConnection(options.ConnectionString);
        using var command = connection.CreateCommand();
        command.CommandText = Query;
        AddParameter(SchemaSimilarTo ?? options.SchemaSimilarTo); // $1
        AddParameter(SchemaNotSimilarTo ?? options.SchemaNotSimilarTo); // $2
        AddParameter(IncludeSchemas ?? options.IncludeSchemas, true); // $3
        AddParameter(ExcludeSchemas ?? options.ExcludeSchemas, true); // $4
        AddParameter(NameSimilarTo ?? options.NameSimilarTo); // $5
        AddParameter(NameNotSimilarTo ?? options.NameNotSimilarTo); // $6
        AddParameter(IncludeNames ?? options.IncludeNames, true); // $7
        AddParameter(ExcludeNames ?? options.ExcludeNames, true); // $8

        connection.Open();
        using NpgsqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            var type = reader.Get<string>("type");
            var paramTypes = reader.Get<string[]>("param_types");
            var returnType = reader.Get<string>("return_type");
            var name = reader.Get<string>("name");

            var volatility = reader.Get<char>("volatility_option");
            var hasGet =
                name.Contains("_get_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("_get", StringComparison.OrdinalIgnoreCase);
            var crudType = hasGet ? CrudType.Select : (volatility == 'v' ? CrudType.Update : CrudType.Select);

            var paramNames = reader.Get<string[]>("param_names");
            var isVoid = string.Equals(returnType, "void", StringComparison.Ordinal);
            var schema = reader.Get<string>("schema");
            var returnsRecord = reader.Get<bool>("returns_record");
            var returnsUnnamedSet = reader.Get<bool>("returns_unnamed_set");

            var returnRecordTypes = reader.Get<string[]>("return_record_types");
            TypeDescriptor[] returnTypeDescriptor;
            if (isVoid)
            {
                returnTypeDescriptor = [];
            }
            else
            {
                if (returnsRecord == false)
                {
                    returnTypeDescriptor = [new TypeDescriptor(returnType)];
                }
                else
                {
                    returnTypeDescriptor = returnRecordTypes.Select(x => new TypeDescriptor(x)).ToArray();
                }
            }
            var returnRecordNames = reader.Get<string[]>("return_record_names");
            var paramDefaults = reader.Get<string?[]>("param_defaults");
            bool isUnnamedRecord = reader.Get<bool>("is_unnamed_record");
            var routineType = type.GetEnum<RoutineType>();
            var callIdent = routineType == RoutineType.Procedure ? "call " : "select ";
            var paramCount = reader.Get<int>("param_count");
            var returnRecordCount = reader.Get<int>("return_record_count");
            var variadic = reader.Get<bool>("has_variadic");
            var expression = string.Concat(
                (isVoid || !returnsRecord || (returnsRecord && returnsUnnamedSet) || isUnnamedRecord)
                    ? callIdent
                    : string.Concat(callIdent, string.Join(", ", returnRecordNames), " from "),
                schema,
                ".",
                name,
                "(",
                variadic && paramCount > 0 ? "variadic " : "");

            var simpleDefinition = new StringBuilder();
            simpleDefinition.AppendLine(string.Concat(
                routineType.ToString().ToLower(), " ",
                schema, ".",
                name, "(",
                paramCount == 0 ? ")" : ""));
            if (paramCount > 0)
            {
                for (var i = 0; i < paramCount; i++)
                {
                    var paramName = paramNames[i];
                    var defaultValue = paramDefaults[i];
                    var paramType = paramTypes[i];
                    var fullParamType = defaultValue == null ? paramType : $"{paramType} DEFAULT {defaultValue}";
                    simpleDefinition
                        .AppendLine(string.Concat("    ", paramName, " ", fullParamType, i == paramCount - 1 ? "" : ","));
                }
                simpleDefinition.AppendLine(")");
            }
            if (!returnsRecord)
            {
                simpleDefinition.AppendLine(string.Concat("returns ", returnType));
            }
            else
            {
                if (returnsUnnamedSet || isUnnamedRecord)
                {
                    simpleDefinition.AppendLine(string.Concat("returns setof ", returnRecordTypes[0]));
                }
                else
                {
                    simpleDefinition.AppendLine("returns table(");

                    for (var i = 0; i < returnRecordCount; i++)
                    {
                        var returnParamName = returnRecordNames[i];
                        var returnParamType = returnRecordTypes[i];
                        simpleDefinition
                            .AppendLine(string.Concat("    ", returnParamName, " ", returnParamType, i == returnRecordCount - 1 ? "" : ","));
                    }
                    simpleDefinition.AppendLine(")");
                }
            }

            yield return (
                new Routine(
                    type: routineType,
                    schema: schema,
                    name: name,
                    comment: reader.Get<string>("comment"),
                    isStrict: reader.Get<bool>("is_strict"),
                    crudType: crudType,
                
                    returnsRecord: returnsRecord,
                    returnType: returnType,
                    returnRecordCount: returnRecordCount,
                    returnRecordNames: returnRecordNames,
                    returnRecordTypes: returnRecordTypes,
                    returnsUnnamedSet: returnsUnnamedSet || isUnnamedRecord,
                    returnTypeDescriptor: returnTypeDescriptor,
                    isVoid: isVoid,

                    paramCount: paramCount,
                    paramNames: paramNames,
                    paramTypeDescriptor: paramTypes
                        .Select((x, i) => new TypeDescriptor(x, hasDefault: paramDefaults[i] is not null))
                        .ToArray(),

                    expression: expression,
                    fullDefinition: reader.Get<string>("definition"),
                    simpleDefinition: simpleDefinition.ToString()), 
                _formatter);
        }

        yield break;

        void AddParameter(object? value, bool isArray = false)
        {
            if (value is null)
            {
                value = DBNull.Value;
            }
            else if (isArray && value is string[] array)
            {
                if (array.Length == 0)
                {
                    value = DBNull.Value;
                }
            }
            else if (!isArray && value is string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                {
                    value = DBNull.Value;
                }
            }
            command.Parameters.Add(new NpgsqlParameter
            {
                NpgsqlDbType = isArray ? NpgsqlDbType.Text | NpgsqlDbType.Array : NpgsqlDbType.Text,
                Value = value
            });
        }
    }
}

internal static class Extensions
{
    internal static T Get<T>(this NpgsqlDataReader reader, string name)
    {
        var value = reader[name];
        if (value == DBNull.Value)
        {
            return default!;
        }
        return (T)value;
    }
    
    internal static T GetEnum<T>(this string? value) where T : struct
    {
        Enum.TryParse<T>(value, true, out var result);
        // return the first enum (Other) when no match
        return result;
    }
}