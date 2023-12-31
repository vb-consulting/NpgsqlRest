﻿using Npgsql;
using NpgsqlTypes;

namespace NpgsqlRest;

internal class RoutineQuery()
{
    private const string query = @"with cte as (
    select 
        lower(r.routine_type) as type,
        r.specific_schema as schema,
        r.routine_name as name,
        proc.oid as oid,
        r.specific_name,
        lower(r.external_language) as language,
        des.description as comment,
        r.security_type,
        proc.proisstrict as is_strict,
        proc.procost::numeric as cost_num,
        proc.prorows::numeric as rows_num,
        case when proc.proparallel = 'u' then 'UNSAFE'
            when proc.proparallel = 's' then 'SAFE' 
            when proc.proparallel = 'r' then 'RESTRICTED' 
        else '' end as parallel_option,
        case when proc.provolatile = 'i' then 'IMMUTABLE' 
            when proc.provolatile = 's' then 'STABLE' 
            when proc.provolatile = 'v' then 'VOLATILE' 
        else '' end volatility_option,
        proc.proretset as returns_set,
        (r.type_udt_schema || '.' || r.type_udt_name)::regtype::text as return_type,
        coalesce(
            array_agg(p.parameter_name::text order by p.ordinal_position) filter(where p.parameter_mode = 'IN' or p.parameter_mode = 'INOUT'), 
            '{}'::text[]
        ) as in_params,
        coalesce(
            array_agg(p.parameter_name::text order by p.ordinal_position) filter(where p.parameter_mode = 'INOUT' or p.parameter_mode = 'OUT'), 
            '{}'::text[]
        ) as out_params,
        coalesce(
            array_agg((p.udt_schema || '.' || p.udt_name)::regtype::text order by p.ordinal_position) filter(where p.parameter_mode = 'IN' or p.parameter_mode = 'INOUT'), 
            '{}'::text[]
        ) as in_param_types,
        coalesce(
            array_agg((p.udt_schema || '.' || p.udt_name)::regtype::text order by p.ordinal_position) filter(where p.parameter_mode = 'INOUT' or p.parameter_mode = 'OUT'), 
            '{}'::text[]
        ) as out_param_types
    from 
        information_schema.routines r
        join pg_catalog.pg_proc proc on r.specific_name = proc.proname || '_' || proc.oid
        left join pg_catalog.pg_description des on proc.oid = des.objoid
        left join information_schema.parameters p on r.specific_name = p.specific_name and r.specific_schema = p.specific_schema 
    where
        r.specific_schema = any(
            select
                nspname
            from
                pg_catalog.pg_namespace
            where
                nspname not like 'pg_%' 
                and nspname <> 'information_schema' 
                and ($1 is null or nspname similar to $1)
                and ($2 is null or nspname not similar to $2)
                and ($3 is null or nspname = any($3))
                and ($4 is null or not nspname = any($4))
        )
        and ($5 is null or r.routine_name similar to $5)
        and ($6 is null or r.routine_name not similar to $6)
        and ($7 is null or r.routine_name = any($7))
        and ($8 is null or not r.routine_name = any($8))

        and proc.prokind in ('f', 'p')
        and not lower(r.external_language) = any(array['c', 'internal'])
        and r.type_udt_name <> 'trigger'
    group by
        r.routine_type, r.specific_schema, r.routine_name, 
        proc.oid, r.specific_name, r.external_language, des.description,
        r.security_type, r.type_udt_schema, r.type_udt_name,
        proc.proisstrict, proc.procost, proc.prorows, proc.proparallel, proc.provolatile, proc.proretset
)
select
    type,
    schema,
    name,
    oid::text,
    concat(name, '(', array_to_string(in_param_types, ', '), ')') as signature,
    language,
    comment,
    security_type,
    is_strict,
    cost_num,
    rows_num,
    parallel_option,
    volatility_option,
    case when returns_set is true or return_type = 'record' then true else false end as returns_record,
    case 
        when returns_set is true and return_type <> 'record' then 'record'
        else return_type
    end as return_type,
    case 
        when case when returns_set is true or return_type = 'record' then true else false end then 
            case when array_length(out_params, 1) is null then 1 else array_length(out_params, 1) end
        else 0
    end as return_record_count,
    case 
        when case when returns_set is true or return_type = 'record' then true else false end then 
            case when array_length(out_params, 1) is null then array[name]::text[] else out_params end
        else array[]::text[]
    end as return_record_names,
    case 
        when case when returns_set is true or return_type = 'record' then true else false end then 
            case when array_length(out_params, 1) is null then array[return_type]::text[] else out_param_types end
        else array[]::text[]
    end as return_record_types,
    returns_set is true and return_type <> 'record' and array_length(out_params, 1) is null as returns_unnamed_set,
    array_length(in_params, 1) as param_count,
    in_params as param_names,
    in_param_types as param_types,
    pg_get_functiondef(oid) as definition
from cte";

    internal static IEnumerable<Routine> Run(NpgsqlRestOptions options)
    {
        using var connection = new NpgsqlConnection(options.ConnectionString);
        using var command = connection.CreateCommand();
        command.CommandText = options.CustomRoutineCommand ?? query;
        
        void AddParameter(object? value, bool isArray = false) => command?
            .Parameters
            .Add(new NpgsqlParameter()
            {
                NpgsqlDbType = isArray ? NpgsqlDbType.Text | NpgsqlDbType.Array : NpgsqlDbType.Text,
                Value = value ?? DBNull.Value
            });

        AddParameter(options.SchemaSimilarTo); // $1
        AddParameter(options.SchemaNotSimilarTo); // $2
        AddParameter(options.IncludeSchemas, true); // $3
        AddParameter(options.ExcludeSchemas, true); // $4
        AddParameter(options.NameSimilarTo); // $5
        AddParameter(options.NameNotSimilarTo); // $6
        AddParameter(options.IncludeNames, true); // $7
        AddParameter(options.ExcludeNames, true); // $8
        
        connection.Open();
        using NpgsqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            var type = reader.Get<string>("type");
            var language = reader.Get<string>("language");
            var paramTypes = reader.Get<string[]>("param_types");
            var returnType = reader.Get<string>("return_type");
            var name = reader.Get<string>("name");
            var paramCount = reader.Get<int>("param_count");
            var paramNames = reader.Get<string[]>("param_names");
            var isVoid = string.Equals(returnType, "void", StringComparison.Ordinal);
            var schema = reader.Get<string>("schema");
            var returnsRecord = reader.Get<bool>("returns_record");
            var returnsUnnamedSet = reader.Get<bool>("returns_unnamed_set");
            var expression = string.Concat(
                isVoid || (returnsRecord == false && returnsRecord == false) ? "select " : string.Concat("select ", string.Join(", ", paramNames), " "),
                schema,
                ".",
                name,
                "(",
                string.Join(", ", Enumerable.Range(1, paramCount).Select(i => $"${i}")), 
                ")");
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
            yield return new Routine(
                type: type.GetEnum<RoutineType>(),
                typeInfo: type,
                schema: schema,
                name: name,
                oid: reader.Get<string>("oid"),
                signature: reader.Get<string>("signature"),
                language: language.GetEnum<Language>(),
                languageInfo: language,
                comment: reader.Get<string>("comment"),
                securityType: reader.GetEnum<SecurityType>("security_type"),
                isStrict: reader.Get<bool>("is_strict"),
                cost: reader.Get<decimal>("cost_num"),
                rows: reader.Get<decimal>("rows_num"),
                parallelOption: reader.GetEnum<ParallelOption>("parallel_option"),
                volatilityOption: reader.GetEnum<VolatilityOption>("volatility_option"),
                returnsRecord: returnsRecord,
                returnType: returnType,
                returnRecordCount: reader.Get<int>("return_record_count"),
                returnRecordNames: reader.Get<string[]>("return_record_names"),
                returnRecordTypes: returnRecordTypes,
                returnsUnnamedSet: returnsUnnamedSet,
                paramCount: paramCount,
                paramNames: paramNames,
                paramTypes: paramTypes,
                definition: reader.Get<string>("definition"),
                paramTypeDescriptor: paramTypes.Select(x => new TypeDescriptor(x)).ToArray(),
                isVoid: isVoid,
                expression: expression,
                returnTypeDescriptor: returnTypeDescriptor);
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
    
    internal static T GetEnum<T>(this NpgsqlDataReader reader, string name) where T : struct
    {
        return reader.Get<string?>(name).GetEnum<T>();
    }

    internal static T GetEnum<T>(this string? value) where T : struct
    {
        Enum.TryParse<T>(value, true, out var result);
        // return the first enum (Other) when no match
        return result;
    }
}