﻿using System;
using System.Linq;
using Npgsql;
using NpgsqlTypes;

namespace NpgsqlRest.CrudSource;

[Flags]
public enum CrudCommandType
{
    Select = 0,
    Update = 1,
    UpdateReturning = 2,
    Insert = 4,
    InsertReturning = 8,
    InsertOnConflictDoNothing = 16,
    InsertOnConflictDoUpdate = 32,
    InsertOnConflictDoNothingReturning = 64,
    InsertOnConflictDoUpdateReturning = 128,
    Delete = 256,
    DeleteReturning = 512,
    All = Select | 
        Update | 
        UpdateReturning | 
        Insert | 
        InsertReturning | 
        InsertOnConflictDoNothing | 
        InsertOnConflictDoUpdate | 
        InsertOnConflictDoNothingReturning | 
        InsertOnConflictDoUpdateReturning | 
        Delete | 
        DeleteReturning
}

public class CrudSource(
    string? schemaSimilarTo = null,
    string? schemaNotSimilarTo = null,
    string[]? includeSchemas = null,
    string[]? excludeSchemas = null,
    string? nameSimilarTo = null,
    string? nameNotSimilarTo = null,
    string[]? includeNames = null,
    string[]? excludeNames = null,
    string? query = null,
    CrudCommandType crudTypes = CrudCommandType.All) : IRoutineSource
{
    private readonly IRoutineSourceParameterFormatter _selectParameterFormatter = new SelectParameterFormatter();
    private readonly IRoutineSourceParameterFormatter _updateParameterFormatter = new UpdateParameterFormatter();
    private readonly IRoutineSourceParameterFormatter _insertParameterFormatter = new InsertParameterFormatter();
    private readonly string NL = Environment.NewLine;

    public string? SchemaSimilarTo { get; init; } = schemaSimilarTo;
    public string? SchemaNotSimilarTo { get; init; } = schemaNotSimilarTo;
    public string[]? IncludeSchemas { get; init; } = includeSchemas;
    public string[]? ExcludeSchemas { get; init; } = excludeSchemas;
    public string? NameSimilarTo { get; init; } = nameSimilarTo;
    public string? NameNotSimilarTo { get; init; } = nameNotSimilarTo;
    public string[]? IncludeNames { get; init; } = includeNames;
    public string[]? ExcludeNames { get; init; } = excludeNames;
    public string Query { get; init; } = query ?? CrudSourceQuery.Query;
    public bool Select { get; init; } = (crudTypes & CrudCommandType.Select) == CrudCommandType.Select;
    public bool Update { get; init; } = (crudTypes & CrudCommandType.Update) == CrudCommandType.Update;
    public bool UpdateReturning { get; init; } = (crudTypes & CrudCommandType.UpdateReturning) == CrudCommandType.UpdateReturning;
    public bool Insert { get; init; } = (crudTypes & CrudCommandType.Insert) == CrudCommandType.Insert;
    public bool InsertReturning { get; init; } = (crudTypes & CrudCommandType.InsertReturning) == CrudCommandType.InsertReturning;
    public bool InsertOnConflictDoNothing { get; init; } = (crudTypes & CrudCommandType.InsertOnConflictDoNothing) == CrudCommandType.InsertOnConflictDoNothing;
    public bool InsertOnConflictDoUpdate { get; init; } = (crudTypes & CrudCommandType.InsertOnConflictDoUpdate) == CrudCommandType.InsertOnConflictDoUpdate;
    public bool InsertOnConflictDoNothingReturning { get; init; } = (crudTypes & CrudCommandType.InsertOnConflictDoNothingReturning) == CrudCommandType.InsertOnConflictDoNothingReturning;
    public bool InsertOnConflictDoUpdateReturning { get; init; } = (crudTypes & CrudCommandType.InsertOnConflictDoUpdateReturning) == CrudCommandType.InsertOnConflictDoUpdateReturning;
    public bool Delete { get; init; } = (crudTypes & CrudCommandType.Delete) == CrudCommandType.Delete;
    public bool DeleteReturning { get; init; } = (crudTypes & CrudCommandType.DeleteReturning) == CrudCommandType.DeleteReturning;

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

            var primaryKeys = new HashSet<string>(reader.Get<string[]>("primary_keys"));
            var identityColumns = reader.Get<bool[]>("identity_columns");
            
            var notPrimaryKeys = columnNames.Where(x => !primaryKeys.Contains(x)).ToArray();
            var descriptors = columnTypes
                .Select((x, i) => 
                    new TypeDescriptor(x, 
                        hasDefault: true, 
                        isPk: primaryKeys.Contains(columnNames[i]),
                        isIdentity: identityColumns[i]))
                .ToArray();
            
            var updatableColumns = reader.Get<bool[]>("updatable_columns");
            bool hasPks = primaryKeys.Count > 0;
            var isInsertable = reader.Get<bool>("is_insertable");
            bool isUpdatable = updatableColumns.Any(x => x) && hasPks;
            bool doesUpdates = (Update || UpdateReturning) && isUpdatable;
            bool doesInserts = (Insert ||
                InsertOnConflictDoNothing ||
                InsertOnConflictDoNothingReturning ||
                InsertOnConflictDoUpdate ||
                InsertOnConflictDoUpdateReturning ||
                InsertReturning) && isInsertable;
            bool doesReturnings = (UpdateReturning && doesUpdates) || 
                ((InsertOnConflictDoNothingReturning ||
                InsertOnConflictDoUpdateReturning ||
                InsertReturning) && doesInserts);

            Routine CreateRoutine(
                CrudType crudType, 
                string expression, 
                string fullDefinition, 
                string simpleDefinition,
                bool isVoid,
                string? formatUrlPattern = null) => new(
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
                    isVoid: isVoid,
                    paramCount: columnCount,
                    paramNames: columnNames,
                    paramTypeDescriptor: descriptors,
                    expression: expression,
                    fullDefinition: fullDefinition,
                    simpleDefinition: simpleDefinition,
                    formatUrlPattern: formatUrlPattern);

            if (Select)
            {
                var expression = string.Concat(
                    "select ",
                    NL, "    ",
                    string.Join(", ", columnNames),
                    NL,
                    " from ",
                    NL, "    ",
                    schema, ".", name,
                    NL);
                var fullDefinition = string.Concat(expression, 
                    "where", 
                    NL, "    ", 
                    columnNames.Select(n => $"{n} = ?"));
                var simpleDefinition = string.Concat("select ", schema, ".", name);

                yield return (
                    CreateRoutine(
                        CrudType.Select, 
                        expression, 
                        fullDefinition, 
                        simpleDefinition,
                        isVoid: false),
                    _selectParameterFormatter);
            }

            string updateExp = default!, updateDef = default!, updateSimple = default!;
            if (doesUpdates)
            {
                updateExp = string.Concat(
                    "update ", schema, ".", name,
                    NL,
                    "{0}",
                    NL,
                    "{1}");
                updateDef = string.Format(updateExp,
                    string.Concat("set", NL, "    ", string.Join(", ", notPrimaryKeys.Select(n => $"{n} = ?"))),
                    string.Concat("where", NL, "    ", string.Join(" and ", notPrimaryKeys.Select(n => $"{n} = ?"))));
                updateSimple = string.Concat("update ", schema, ".", name);
            }

            if (Update && doesUpdates)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Update,
                        expression: updateExp,
                        fullDefinition: updateDef,
                        simpleDefinition: updateSimple,
                        isVoid: true), 
                    _updateParameterFormatter);
            }

            string returningExp = default!;
            if (doesReturnings)
            {
                returningExp = string.Concat(
                    NL,
                    "returning",
                    NL, "    ",
                    string.Join(", ", columnNames));
            }

            if (UpdateReturning && doesUpdates)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Update,
                        expression: string.Concat(updateExp, returningExp),
                        fullDefinition: string.Concat(updateDef, returningExp),
                        simpleDefinition: string.Concat(updateSimple, returningExp),
                        isVoid: false,
                        formatUrlPattern: "{0}/returning"),
                    _updateParameterFormatter);
            }

            string insertExp = default!, insertDef = default!, insertSimple = default!;
            if (doesInserts)
            {
                insertExp = string.Concat("insert into ", schema, ".", name, 
                    NL, "({0})",
                    "{1}",
                    NL, "values",
                    NL, "({2})");
                insertDef = string.Format(insertExp,
                    string.Join(", ", columnNames),
                    "",
                    string.Join(", ", columnNames.Select(c => "?")));
                insertSimple = string.Concat("insert into ", schema, ".", name);
            }

            if (Insert && doesInserts)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Insert,
                        expression: insertExp,
                        fullDefinition: insertDef,
                        simpleDefinition: insertSimple,
                        isVoid: true),
                    _insertParameterFormatter);
            }

            if (InsertReturning && doesInserts)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Insert,
                        expression: string.Concat(insertExp, returningExp),
                        fullDefinition: string.Concat(insertDef, returningExp),
                        simpleDefinition: string.Concat(insertSimple, returningExp),
                        isVoid: false,
                        formatUrlPattern: "{0}/returning"),
                    _insertParameterFormatter);
            }
            
            // untested

            string onConflict = default!;
            if (doesInserts && hasPks && (InsertOnConflictDoNothing || 
                InsertOnConflictDoNothingReturning ||
                InsertOnConflictDoUpdate ||
                InsertOnConflictDoUpdateReturning))
            {
                onConflict = string.Concat(NL, "on conflict (", string.Join(", ", primaryKeys), ") ");
            }

            if (InsertOnConflictDoNothing && doesInserts && hasPks)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Insert,
                        expression: string.Concat(insertExp, onConflict , "do nothing"),
                        fullDefinition: string.Concat(insertDef, onConflict, "do nothing"),
                        simpleDefinition: string.Concat(insertSimple, onConflict, "do nothing"),
                        isVoid: true,
                        formatUrlPattern: "{0}/on-conflict-do-nothing"),
                    _insertParameterFormatter);
            }

            if (InsertOnConflictDoNothingReturning && doesInserts && hasPks)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Insert,
                        expression: string.Concat(insertExp, onConflict,  "do nothing", returningExp),
                        fullDefinition: string.Concat(insertDef, onConflict, "do nothing", returningExp),
                        simpleDefinition: string.Concat(insertSimple, onConflict, "do nothing", returningExp),
                        isVoid: false,
                        formatUrlPattern: "{0}/on-conflict-do-nothing/returning"),
                    _insertParameterFormatter);
            }

            string doUpdate = default!;
            if (doesInserts && hasPks && (InsertOnConflictDoUpdate || InsertOnConflictDoUpdateReturning))
            {
                doUpdate = string.Concat(NL, "do update set ", string.Join(", ", notPrimaryKeys.Select(n => $"{NL}    {n} = excluded.{n}")));
            }

            if (InsertOnConflictDoUpdate && doesInserts && hasPks)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Insert,
                        expression: string.Concat(insertExp, onConflict, doUpdate),
                        fullDefinition: string.Concat(insertDef, onConflict, doUpdate),
                        simpleDefinition: string.Concat(insertSimple, onConflict, doUpdate),
                        isVoid: true,
                        formatUrlPattern: "{0}/on-conflict-do-update"),
                    _insertParameterFormatter);
            }

            if (InsertOnConflictDoUpdateReturning && doesInserts && hasPks)
            {
                yield return (
                    CreateRoutine(
                        CrudType.Insert,
                        expression: string.Concat(insertExp, onConflict, doUpdate, returningExp),
                        fullDefinition: string.Concat(insertDef, onConflict, doUpdate, returningExp),
                        simpleDefinition: string.Concat(insertSimple, onConflict, doUpdate, returningExp),
                        isVoid: false,
                        formatUrlPattern: "{0}/on-conflict-do-update/returning"),
                    _insertParameterFormatter);
            }
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
