using System.Text;
using Npgsql;
using NpgsqlTypes;
using NpgsqlRest.Extensions;

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
        string? query = null,
        CommentsMode? commentsMode = null) : IRoutineSource
{
    private readonly IRoutineSourceParameterFormatter _formatter = new RoutineSourceParameterFormatter();
    public string? SchemaSimilarTo { get; set; } = schemaSimilarTo;
    public string? SchemaNotSimilarTo { get; set; } = schemaNotSimilarTo;
    public string[]? IncludeSchemas { get; set; } = includeSchemas;
    public string[]? ExcludeSchemas { get; set; } = excludeSchemas;
    public string? NameSimilarTo { get; set; } = nameSimilarTo;
    public string? NameNotSimilarTo { get; set; } = nameNotSimilarTo;
    public string[]? IncludeNames { get; set; } = includeNames;
    public string[]? ExcludeNames { get; set; } = excludeNames;
    public string Query { get; set; } = query ?? RoutineSourceQuery.Query;
    public CommentsMode? CommentsMode { get; } = commentsMode;

    public IEnumerable<(Routine, IRoutineSourceParameterFormatter)> Read(NpgsqlRestOptions options)
    {
        using var connection = new NpgsqlConnection(options.ConnectionString);
        using var command = connection.CreateCommand();
        if (Query.Contains(' ') is false)
        {
            command.CommandText = string.Concat("select * from ", Query, "($1,$2,$3,$4,$5,$6,$7,$8)");
        }
        else
        {
            command.CommandText = Query;
        }
        
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
            var type = reader.Get<string>(0);//"type");
            var paramTypes = reader.Get<string[]>(14);// "param_types");
            var returnType = reader.Get<string>(7);// "return_type");
            var name = reader.Get<string>(2);// "name");

            var volatility = reader.Get<char>(5);//"volatility_option");

            var hasGet =
                name.Contains("_get_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("_get", StringComparison.OrdinalIgnoreCase);
            var crudType = hasGet ? CrudType.Select : (volatility == 'v' ? CrudType.Update : CrudType.Select);

            var paramNames = reader.Get<string[]>(13);//"param_names");
            var isVoid = string.Equals(returnType, "void", StringComparison.Ordinal);
            var schema = reader.Get<string>(1);//"schema");
            var returnsSet = reader.Get<bool>(6);//"returns_set");

            var returnRecordTypes = reader.Get<string[]>(10);//"return_record_types");
            TypeDescriptor[] returnTypeDescriptor;
            if (isVoid)
            {
                returnTypeDescriptor = [];
            }
            else
            {
                returnTypeDescriptor = returnRecordTypes.Select(x => new TypeDescriptor(x)).ToArray();
            }
            var returnRecordNames = reader.Get<string[]>(9);//"return_record_names");
            var paramDefaults = reader.Get<string?[]>(15);//"param_defaults");
            bool isUnnamedRecord = reader.Get<bool>(11);// "is_unnamed_record");
            var routineType = type.GetEnum<RoutineType>();
            var callIdent = routineType == RoutineType.Procedure ? "call " : "select ";
            var paramCount = reader.Get<int>(12);// "param_count");
            var returnRecordCount = reader.Get<int>(8);// "return_record_count");
            var variadic = reader.Get<bool>(16);// "has_variadic");
            var expression = string.Concat(
                (isVoid || returnRecordCount == 1)
                    ? callIdent
                    : string.Concat(callIdent, string.Join(",", returnRecordNames), " from "),
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
            if (!returnsSet)
            {
                simpleDefinition.AppendLine(string.Concat("returns ", returnType));
            }
            else
            {
                if (isUnnamedRecord)
                {
                    simpleDefinition.AppendLine(string.Concat($"returns setof {returnType}"));
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
                    comment: reader.Get<string>(3),//"comment"),
                    isStrict: reader.Get<bool>(4),//"is_strict"),
                    crudType: crudType,

                    returnsRecordType: string.Equals(returnType, "record", StringComparison.OrdinalIgnoreCase),
                    returnsSet: returnsSet,
                    columnCount: returnRecordCount,
                    columnNames: returnRecordNames,
                    returnsUnnamedSet: isUnnamedRecord,
                    columnsTypeDescriptor: returnTypeDescriptor,
                    isVoid: isVoid,

                    paramCount: paramCount,
                    paramNames: paramNames,
                    paramTypeDescriptor: paramTypes
                        .Select((x, i) => new TypeDescriptor(x, hasDefault: paramDefaults[i] is not null))
                        .ToArray(),

                    expression: expression,
                    fullDefinition: reader.Get<string>(17),//"definition"),
                    simpleDefinition: simpleDefinition.ToString(),
                    
                    tags: [routineType.ToString().ToLowerInvariant(), volatility switch
                    {
                        'v' => "volatile",
                        's' => "stable",
                        'i' => "immutable",
                        _ => "other"
                    }]), 
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
