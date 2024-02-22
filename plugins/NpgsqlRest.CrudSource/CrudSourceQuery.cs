namespace NpgsqlRest.CrudSource;

internal class CrudSourceQuery
{
    public const string Query = """
    with _cte1 as (
        select
            nspname as schema
        from
            pg_catalog.pg_namespace
        where
            nspname not like 'pg_%'
            and nspname <> 'information_schema'
            and ($1 is null or nspname similar to $1)
            and ($2 is null or nspname not similar to $2)
            and ($3 is null or nspname = any($3))
            and ($4 is null or not nspname = any($4))
    ), _cte2 as (
        select 
            tc.table_name,
            tc.table_schema,
            coalesce(array_agg(kcu.column_name), '{}'::text[]) as primary_keys
        from information_schema.table_constraints as tc
        join information_schema.key_column_usage as kcu on tc.constraint_name = kcu.constraint_name and tc.table_schema = kcu.table_schema
        join _cte1 on tc.table_schema = _cte1.schema
        where 
            tc.constraint_type = 'PRIMARY KEY'
            and ($5 is null or tc.table_name similar to $5)
            and ($6 is null or tc.table_name not similar to $6)
            and ($7 is null or tc.table_name = any($7))
            and ($8 is null or not tc.table_name = any($8))
        group by
            tc.table_name,
            tc.table_schema
    )
    select
        t.table_type as type,
        quote_ident(t.table_schema) as schema,
        quote_ident(t.table_name) as name,
        t.is_insertable_into = 'YES' as is_insertable,

        count(*)::int as column_count,
        coalesce(
            array_agg(c.column_name order by c.ordinal_position),
            '{}'::text[]
        ) as column_names,

        coalesce(
            array_agg(
                case when c.data_type = 'bit' then 'varbit' else (c.udt_schema || '.' || c.udt_name)::regtype::text end
                order by c.ordinal_position
            ),
            '{}'::text[]
        ) as column_types,

        coalesce(
            array_agg(c.is_updatable = 'YES' order by c.ordinal_position),
            '{}'::boolean[]
        ) as updatable_columns,

        coalesce(
            array_agg(coalesce(c.identity_generation, '') = 'ALWAYS' order by c.ordinal_position),
            '{}'::boolean[]
        ) as identity_columns,

        coalesce(_cte2.primary_keys, '{}'::text[]) as primary_keys,
        pgdesc.description as comment
    from
        information_schema.columns c
        join _cte1 on c.table_schema = _cte1.schema
        join information_schema.tables t on c.table_schema = t.table_schema and c.table_name = t.table_name
        left join _cte2 on c.table_schema = _cte2.table_schema and c.table_name = _cte2.table_name
        left join pg_catalog.pg_stat_all_tables pgtbl
            on t.table_name = pgtbl.relname and t.table_schema = pgtbl.schemaname
        left join pg_catalog.pg_description pgdesc
            on pgtbl.relid = pgdesc.objoid and pgdesc.objsubid = 0
    where
        (t.table_type = 'VIEW' or t.table_type = 'BASE TABLE')
        and ($5 is null or t.table_name similar to $5)
        and ($6 is null or t.table_name not similar to $6)
        and ($7 is null or t.table_name = any($7))
        and ($8 is null or not t.table_name = any($8))
    group by 
        t.table_type,
        t.table_schema,
        t.table_name,
        t.is_insertable_into,
        _cte2.primary_keys,
        pgdesc.description
    """;
}
