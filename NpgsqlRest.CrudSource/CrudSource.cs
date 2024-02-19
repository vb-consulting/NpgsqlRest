using Npgsql;
using NpgsqlTypes;

namespace NpgsqlRest.CrudSource;

public class CrudSource(
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
    public string? SchemaSimilarTo { get; init; } = schemaSimilarTo;
    public string? SchemaNotSimilarTo { get; init; } = schemaNotSimilarTo;
    public string[]? IncludeSchemas { get; init; } = includeSchemas;
    public string[]? ExcludeSchemas { get; init; } = excludeSchemas;
    public string? NameSimilarTo { get; init; } = nameSimilarTo;
    public string? NameNotSimilarTo { get; init; } = nameNotSimilarTo;
    public string[]? IncludeNames { get; init; } = includeNames;
    public string[]? ExcludeNames { get; init; } = excludeNames;
    public string Query { get; init; } = query ?? CrudSourceQuery.Query;

    public IRoutineSourceParameterFormatter GetRoutineSourceParameterFormatter()
    {
        return new CrudSourceParameterFormatter();
    }

    public IEnumerable<Routine> Read(NpgsqlRestOptions options)
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
            var type = reader.Get<string>("comment") switch
            {
                "BASE TABLE" => RoutineType.Table,
                "VIEW" => RoutineType.View,
                _ => RoutineType.Other
            };
            var schema = reader.Get<string>("schema");
            var name = reader.Get<string>("name");
            var comment = reader.Get<string>("comment");

            var columnCount = reader.Get<int>("column_count");
            var columnNames = reader.Get<string[]>("column_names");
            var columnTypes = reader.Get<string[]>("column_types");
            var descriptors = columnTypes.Select(x => new TypeDescriptor(x, true)).ToArray();

            Routine CreateRoutine(CrudType crudType, string expression, string fullDefinition, string simpleDefinition) => new(
                type: type,
                schema: schema,
                name: name,
                comment: comment,
                isStrict: false,
                crudType: crudType,

                returnsRecord: true,
                returnType: name,
                returnRecordCount: columnCount,
                returnRecordNames: columnNames,
                returnRecordTypes: columnTypes,
                returnsUnnamedSet: false,
                returnTypeDescriptor: descriptors,
                isVoid: false,

                paramCount: columnCount,
                paramNames: columnNames,
                paramTypeDescriptor: descriptors,

                expression: expression,
                fullDefinition: fullDefinition,
                simpleDefinition: simpleDefinition,
                formattableCommand: false);

            var expression = string.Concat(
                "select ", 
                string.Join(", ", columnNames),
                " from ", 
                schema, ".", name);
            var fullDefinition = string.Concat(
                "select ",
                Environment.NewLine, "    ",
                string.Join(", ", columnNames),
                " from ",
                Environment.NewLine, "    ",
                schema, ".", name);
            var simpleDefinition = string.Concat("select ", schema, ".", name);

            yield return CreateRoutine(CrudType.Select, expression, fullDefinition, simpleDefinition);
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
}