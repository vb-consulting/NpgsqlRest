# Performance Tests

This directory contains files required for performance tests with the [Grafana K6](https://k6.io/) REST API load and performance testing tool.

Used API's are from:

- Latest version 2.0.0 [NpgsqlRest AOT build](https://github.com/vb-consulting/NpgsqlRest/tree/master/AotBuildTemplate).
- [PostgREST 12.0.2](https://github.com/PostgREST/postgrest/releases/tag/v12.0.2)

## Files

- `appsettings.json` - configuration for NpgsqlRest used in testing.
- `k6-api-tests.js` - K6 testing script.
- `perf_tests_script.sql` - database script that creates functions that are tested.
- `perf_tests.http` - HTTP file for smoke tests for both systems.
- `postgrest.conf` - PostgREST configuration file used in testing.
- `readme.md` - this file.
- `test-script.sh` - shell script that orchestrates and runs all tests.
- `results` - directory with the raw dump of text files from the testing session.

## Results

Number of successful requests in 50 seconds (higher is better).

| Records | Function   | NpgsqlRest AOT (1) | NpgsqlRest JIT (2) | PostgREST | Ratio (AOT / PostgREST) | Ratio (JIT / PostgREST) |
| ------: | ---------: | ---------: | --------: | --------: | --------: | --------: |
| 10 | `perf_test` | 370,415 | 423,408 | 68,021 | 5.45 | 6.22 |
| 100 | `perf_test` | 352,021 | 400,924 | 59,749 | 5.89 | 6.71 |
| 10 | `perf_test_arrays` | 276,115 | 310,398 | 51,704 | 5.34 | 6.00 |
| 100 | `perf_test_arrays` | 275,891 | 289,838 | 49,760 | 5.54 | 5.82 |
| 10 | `perf_test_record` | 477,001 | 522,165 | 62,392 | 7.65 | 8.37 |
| 100 | `perf_test_record` | 480,127 | 493,580 | 64,619 | 7.43 | 7.64 |
| 10 | `perf_test_record_arrays` | 349,336 | 379,135 | 55,602 | 6.28 | 6.82 |
| 100 | `perf_test_record_arrays` | 356,748 | 362,237 | 51,401 | 6.94 | 7.05 |

1) NpgsqlRest AOT is ahead-of-time (AOT) compiled to native code. AOT has an average **startup time of between 180 to 200 milliseconds.**
2) NpgsqlRest JIT is a Just-In-Time (JIT) compilation of Common Intermediate Language (CIL). JIT has an average **startup time of between 360 to 400 milliseconds.**

### Other Platforms

| Records | Function   | NpgsqlRest AOT | NpgsqlRest JIT | Django REST Framework 4.2.10 on Python 3.8 |
| ------: | ---------: | ---------: | --------: | --------: |
| 10 | `perf_test` | 370,415 | 423,408 | 11,476 |
| 100 | `perf_test` | 352,021 | 400,924 | 11,695 |
| 10 | `perf_test_arrays` | 276,115| 310,398 | 11,784 |
| 100 | `perf_test_arrays` | 275,891| 289,838 | 9,575 |

## Tests Functions

```sql
create or replace function perf_test(
    _records int,
    _text_param text,
    _int_param int,
    _ts_param timestamp,
    _bool_param bool
) 
returns table(
    id1 int, 
    foo1 text, 
    bar1 text, 
    datetime1 timestamp, 
    id2 int, 
    foo2 text, 
    bar2 text, 
    datetime2 timestamp,
    long_foo_bar text, 
    is_foobar bool
)
language sql
as
$$
select
    i + _int_param as id1,
    'foo' || '_' || _text_param || '_' || i::text as foo1,
        'bar' || i::text as bar1,
        (_ts_param::date) + (i::text || ' days')::interval as datetime1,
        i+1 + _int_param as id2,
    'foo' || '_' || _text_param || '_' || (i+1)::text as foo2,
        'bar' || '_' || _text_param || '_' || (i+1)::text as bar2,
        (_ts_param::date) + ((i+1)::text || ' days')::interval as datetime2,
    'long_foo_bar_' || '_' || _text_param || '_' || (i+2)::text as long_foo_bar, 
    (i % 2)::boolean and _bool_param as is_foobar
from
    generate_series(1, _records) as i
$$;
```

```sql
create or replace function perf_test_arrays(
    _records int,
    _text_param text,
    _int_param int,
    _ts_param timestamp,
    _bool_param bool
) 
returns table(
    id1 int[], 
    foo1 text[], 
    bar1 text[], 
    datetime1 timestamp[], 
    id2 int[], 
    foo2 text[], 
    bar2 text[], 
    datetime2 timestamp[],
    long_foo_bar text[], 
    is_foobar bool[]
)
language sql
as
$$
select
    array[i + _int_param, i + _int_param + 1, i + _int_param + 2] as id1, 
    array['foo' || '_' || _text_param || '_' || i::text, 'foo2', 'foo3'] as foo1, 
    array['bar' || i::text, 'bar2', 'bar3'] as bar1, 
    array[(_ts_param::date) + (i::text || ' days')::interval, _ts_param::date + '1 days'::interval, _ts_param::date + '2 days'::interval] as datetime1, 
    array[i+1 + _int_param, i+2 + _int_param, i+3 + _int_param] as id2, 
    array['foo' || '_' || _text_param || '_' || (i+1)::text, 'foo' || '_' || _text_param || '_' || (i+2)::text, 'foo' || '_' || _text_param || '_' || (i+3)::text] as foo2, 
    array['bar' || '_' || _text_param || '_' || (i+1)::text, 'bar' || '_' || _text_param || '_' || (i+2)::text, 'bar' || '_' || _text_param || '_' || (i+3)::text] as bar2, 
    array[(_ts_param::date) + ((i+1)::text || ' days')::interval, _ts_param::date + '1 days'::interval, _ts_param::date + '2 days'::interval] as datetime2,
    array['long_foo_bar_' || '_' || _text_param || '_' || (i+2)::text, 'long_foo_bar_' || '_' || _text_param || '_' || (i+3)::text, 'long_foo_bar_' || '_' || _text_param || '_' || (i+4)::text] as long_foo_bar, 
    array[(i % 2)::boolean and _bool_param, ((i+1) % 2)::boolean and _bool_param, ((i+2) % 2)::boolean and _bool_param] as is_foobar
from
    generate_series(1, _records) as i
$$;
```

```sql
create or replace function perf_test_record(
    _records int,
    _text_param text,
    _int_param int,
    _ts_param timestamp,
    _bool_param bool
) 
returns setof record
language sql
as
$$
select
    i + _int_param as id1,
    'foo' || '_' || _text_param || '_' || i::text as foo1,
        'bar' || i::text as bar1,
        (_ts_param::date) + (i::text || ' days')::interval as datetime1,
        i+1 + _int_param as id2,
    'foo' || '_' || _text_param || '_' || (i+1)::text as foo2,
        'bar' || '_' || _text_param || '_' || (i+1)::text as bar2,
        (_ts_param::date) + ((i+1)::text || ' days')::interval as datetime2,
    'long_foo_bar_' || '_' || _text_param || '_' || (i+2)::text as long_foo_bar, 
    (i % 2)::boolean and _bool_param as is_foobar
from
    generate_series(1, _records) as i
$$;
```

```sql
create or replace function perf_test_record_arrays(
    _records int,
    _text_param text,
    _int_param int,
    _ts_param timestamp,
    _bool_param bool
) 
returns setof record
language sql
as
$$
select
    array[i + _int_param, i + _int_param + 1, i + _int_param + 2] as id1, 
    array['foo' || '_' || _text_param || '_' || i::text, 'foo2', 'foo3'] as foo1, 
    array['bar' || i::text, 'bar2', 'bar3'] as bar1, 
    array[(_ts_param::date) + (i::text || ' days')::interval, _ts_param::date + '1 days'::interval, _ts_param::date + '2 days'::interval] as datetime1, 
    array[i+1 + _int_param, i+2 + _int_param, i+3 + _int_param] as id2, 
    array['foo' || '_' || _text_param || '_' || (i+1)::text, 'foo' || '_' || _text_param || '_' || (i+2)::text, 'foo' || '_' || _text_param || '_' || (i+3)::text] as foo2, 
    array['bar' || '_' || _text_param || '_' || (i+1)::text, 'bar' || '_' || _text_param || '_' || (i+2)::text, 'bar' || '_' || _text_param || '_' || (i+3)::text] as bar2, 
    array[(_ts_param::date) + ((i+1)::text || ' days')::interval, _ts_param::date + '1 days'::interval, _ts_param::date + '2 days'::interval] as datetime2,
    array['long_foo_bar_' || '_' || _text_param || '_' || (i+2)::text, 'long_foo_bar_' || '_' || _text_param || '_' || (i+3)::text, 'long_foo_bar_' || '_' || _text_param || '_' || (i+4)::text] as long_foo_bar, 
    array[(i % 2)::boolean and _bool_param, ((i+1) % 2)::boolean and _bool_param, ((i+2) % 2)::boolean and _bool_param] as is_foobar
from
    generate_series(1, _records) as i
$$;
```