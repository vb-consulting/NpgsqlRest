namespace NpgsqlRest;

internal class RoutineSourceQuery
{
    public const string Query = """
    with cte as (
        select
            lower(r.routine_type) as type,
            quote_ident(r.specific_schema) as schema,
            quote_ident(r.routine_name) as name,
            proc.oid as oid,
            r.specific_name,
            des.description as comment,
            proc.proisstrict as is_strict,
            proc.provolatile as volatility_option,
            proc.proretset as returns_set,
            (r.type_udt_schema || '.' || r.type_udt_name)::regtype::text as return_type,

            coalesce(
                array_agg(quote_ident(p.parameter_name::text) order by p.ordinal_position) filter(where p.parameter_mode = 'IN' or p.parameter_mode = 'INOUT'),
                '{}'::text[]
            ) as in_params,

            coalesce(
                case when r.data_type = 'USER-DEFINED' then cols.out_params
                else
                    array_agg(quote_ident(p.parameter_name::text) order by p.ordinal_position) filter(where p.parameter_mode = 'INOUT' or p.parameter_mode = 'OUT')
                end,
                '{}'::text[]
            ) as out_params,

            coalesce(
                array_agg(
                    case when p.data_type = 'bit' then 'varbit' else (p.udt_schema || '.' || p.udt_name)::regtype::text end
                    order by p.ordinal_position
                ) filter(where p.parameter_mode = 'IN' or p.parameter_mode = 'INOUT'),
                '{}'::text[]
            ) as in_param_types,

            coalesce(
                case when r.data_type = 'USER-DEFINED' then cols.out_params_types
                else
                    array_agg(
                        case when p.data_type = 'bit' then 'varbit' else (p.udt_schema || '.' || p.udt_name)::regtype::text end
                        order by p.ordinal_position) filter(where p.parameter_mode = 'INOUT' or p.parameter_mode = 'OUT')
                end, '{}'::text[]
            ) as out_param_types,

            coalesce(
                array_agg(p.parameter_default order by p.ordinal_position) filter(where p.parameter_mode = 'IN' or p.parameter_mode = 'INOUT'),
                '{}'::text[]
            ) as in_param_defaults,

            case when proc.provariadic <> 0 then true else false end as has_variadic
        from
            information_schema.routines r
            join pg_catalog.pg_proc proc on r.specific_name = proc.proname || '_' || proc.oid
            left join pg_catalog.pg_description des on proc.oid = des.objoid
            left join information_schema.parameters p on r.specific_name = p.specific_name and r.specific_schema = p.specific_schema
            left join lateral (
                select 
                    col.table_schema,
                    col.table_name,
                    array_agg(quote_ident(col.column_name) order by col.ordinal_position) as out_params,
                    array_agg(
                        case when col.data_type = 'bit' then 'varbit' else (col.udt_schema || '.' || col.udt_name)::regtype::text end
                        order by col.ordinal_position
                    ) as out_params_types
                from information_schema.columns col
                where
                    r.data_type = 'USER-DEFINED' 
                    and col.table_schema = r.type_udt_schema 
                    and col.table_name = r.type_udt_name
                group by
                    col.table_schema,
                    col.table_name
            ) cols on true
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
            and coalesce(r.type_udt_name, '') <> 'trigger'
        group by
            r.routine_type, r.specific_schema, r.routine_name,
            proc.oid, r.specific_name, des.description,
            r.data_type, r.type_udt_schema, r.type_udt_name,
            proc.proisstrict, proc.procost, proc.prorows, proc.proparallel, proc.provolatile,
            proc.proretset, proc.provariadic,
            cols.out_params, cols.out_params_types
    )
    select
        type,
        schema,
        name,
        comment,
        is_strict,
        volatility_option,
        case when returns_set is true or return_type = 'record' then true else false end as returns_record,
        case
            when returns_set is true and return_type <> 'record' then 'record'
            else coalesce(return_type, 'void')
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
        returns_set is true and return_type = 'record' and array_length(out_params, 1) is null as is_unnamed_record,

        array_length(in_params, 1) as param_count,
        in_params as param_names,
        in_param_types as param_types,
        in_param_defaults as param_defaults,
        has_variadic,
        pg_get_functiondef(oid) as definition
    from cte
    """;
}
