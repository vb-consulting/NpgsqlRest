namespace NpgsqlRest;

internal class RoutineSourceQuery
{
    public const string Query = """
    with _types as (

        select
            (n.nspname::text || '.' || t.typname::text)::regtype::text as name,
            a.attnum as att_pos,
            quote_ident(a.attname) as att_name,
            pg_catalog.format_type(a.atttypid, a.atttypmod) as att_type
        from 
            pg_catalog.pg_type t
            join pg_catalog.pg_namespace n on n.oid = t.typnamespace
            join pg_catalog.pg_class c on t.typrelid = c.oid and c.relkind = 'c'
            join pg_catalog.pg_attribute a on t.typrelid = a.attrelid and a.attisdropped is false

    ), s as (

        select
            array_agg(nspname) as s
        from
            pg_catalog.pg_namespace
        where
            nspname not like 'pg_%'
            and nspname <> 'information_schema'
            and ($1 is null or nspname similar to $1)
            and ($2 is null or nspname not similar to $2)
            and ($3 is null or nspname = any($3))
            and ($4 is null or not nspname = any($4))
    ), r as (

        select
            r.routine_type, 
            r.specific_schema, 
            r.specific_name,
            r.routine_name,
            r.data_type, 
            r.type_udt_schema,
            r.type_udt_name,
            (r.type_udt_schema || '.' || r.type_udt_name)::regtype::text as return_type,
            r.data_type = 'USER-DEFINED' as is_user_defined,

            quote_ident(p.parameter_name::text) as param_name,
            p.parameter_mode = 'IN' or p.parameter_mode = 'INOUT' as is_in_param,
            p.parameter_mode = 'INOUT' or p.parameter_mode = 'OUT' as is_out_param,

            row_number() over (partition by r.specific_schema, r.specific_name order by p.ordinal_position, t.att_pos) as param_position,
            case when p.data_type = 'bit' then 'varbit' else (p.udt_schema || '.' || p.udt_name)::regtype::text end as param_type,

            t.att_name as custom_type_name,
            t.att_type as custom_type_type,
            t.att_pos as custom_type_pos

        from information_schema.routines r
        join s on r.specific_schema = any(s.s)
        left join information_schema.parameters p on r.specific_name = p.specific_name and r.specific_schema = p.specific_schema
        left join _types t on p.data_type = 'USER-DEFINED' and (p.udt_schema || '.' || p.udt_name)::regtype::text = t.name

        where
            not lower(r.external_language) = any(array['c', 'internal'])
            and coalesce(r.type_udt_name, '') <> 'trigger'
            and r.routine_type in ('FUNCTION', 'PROCEDURE')
            and ($5 is null or r.routine_name similar to $5)
            and ($6 is null or r.routine_name not similar to $6)
            and ($7 is null or r.routine_name = any($7))
            and ($8 is null or not r.routine_name = any($8))
        order by 
            r.specific_schema, r.specific_name, p.ordinal_position, t.att_pos

    ), cte1 as (

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
            r.return_type,

            coalesce(array_agg(r.param_name order by r.param_position) filter(where r.is_in_param), '{}'::text[]) as in_params,
            case 
                when r.is_user_defined then null
                else coalesce((array_agg(r.param_name order by r.param_position) filter(where r.is_out_param)),'{}'::text[])
            end as out_params,

            coalesce(array_agg(r.param_type order by r.param_position) filter(where r.is_in_param), '{}'::text[]) as in_param_types,

            case 
                when  r.is_user_defined then null
                else coalesce((array_agg(r.param_type order by r.param_position) filter(where r.is_out_param)), '{}'::text[])
            end as out_param_types,

            pg_get_function_arguments(proc.oid) as arguments_def,

            case when proc.provariadic <> 0 then true else false end as has_variadic,

            r.type_udt_schema,
            r.type_udt_name,
            r.data_type,

            coalesce(array_agg(r.custom_type_name order by r.param_position), '{}'::text[]) as custom_type_names,
            coalesce(array_agg(r.custom_type_type order by r.param_position), '{}'::text[]) as custom_type_types,
            coalesce(array_agg(r.custom_type_pos order by r.param_position), '{}'::smallint[]) as custom_type_positions
        from
            r
            join pg_catalog.pg_proc proc on r.specific_name = proc.proname || '_' || proc.oid
            left join pg_catalog.pg_description des on proc.oid = des.objoid
        group by
            r.routine_type, 
            r.specific_schema, 
            r.routine_name,
            r.specific_name, 
            r.data_type, 
            r.type_udt_schema, 
            r.type_udt_name, 
            r.return_type, 
            r.is_user_defined,
            des.description,
            proc.oid,
            proc.proisstrict, 
            proc.procost, 
            proc.prorows, 
            proc.proparallel, 
            proc.provolatile,
            proc.proretset, 
            proc.provariadic

    ), cte2 as (

        select 
            cte1.schema, 
            cte1.specific_name,
            array_agg(quote_ident(col.column_name) order by col.ordinal_position) as out_params,
            array_agg(
                case when col.data_type = 'bit' then 'varbit' else (col.udt_schema || '.' || col.udt_name)::regtype::text end
                order by col.ordinal_position
            ) as out_param_types
        from 
            information_schema.columns col
            join cte1 on 
            cte1.data_type = 'USER-DEFINED' and col.table_schema = cte1.type_udt_schema and col.table_name = cte1.type_udt_name 
        group by
            cte1.schema, cte1.specific_name

    )
    select
        type,
        cte1.schema,
        cte1.name,
        comment,
        is_strict,
        volatility_option,

        returns_set,
        coalesce(return_type, 'void') as return_type,

        coalesce(array_length(coalesce(cte1.out_params, cte2.out_params), 1), 1) as return_record_count,
        case 
            when array_length(coalesce(cte1.out_params, cte2.out_params), 1) is null 
            then array[cte1.name]::text[] 
            else coalesce(cte1.out_params, cte2.out_params) 
        end as return_record_names,

        case 
            when array_length(coalesce(cte1.out_param_types, cte2.out_param_types), 1) is null 
            then array[return_type]::text[] 
            else coalesce(cte1.out_param_types, cte2.out_param_types)
        end as return_record_types,

        coalesce(cte1.out_params, cte2.out_params) = '{}' as is_unnamed_record,

        array_length(in_params, 1) as param_count,
        in_params as param_names,
        in_param_types as param_types,
        arguments_def,
        has_variadic,
        pg_get_functiondef(oid) as definition,
        custom_type_names,
        custom_type_types,
        custom_type_positions
    from cte1
    left join cte2 on cte1.schema = cte2.schema and cte1.specific_name = cte2.specific_name
    
    """;
}
