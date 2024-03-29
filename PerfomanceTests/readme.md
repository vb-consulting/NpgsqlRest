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

The number of successful requests executed by the K6 (see the `k6-api-tests.js` file in this dir):

- Duration of 60 seconds.
- 100 simultaneous virtual users.
- Retrieval of 10 and 100 records.

Numbers represent an overall number of successful requests (higher is better).

| Records | Function   | AOT [1](#1-aot) | JIT [2](#2-jit) | PostgREST | Ratio (AOT / PostgREST) | Ratio (JIT / PostgREST) |
| ------: | ---------: | ---------: | --------: | --------: | --------: | --------: |
| 10 | `perf_test` | 781,803 | 562,304 | 72,305 | 10.81 | 7.78 |
| 100 | `perf_test` | 307,427 | 303,692 | 40,456 | 7.60 | 7.51 |
| 10 | `perf_test_arrays` | 495,190 | 384,090 | 55,331 | 8.95 | 6.94 |
| 100 | `perf_test_arrays` | 124,912 | 127,707 | 32,418 | 3.85 | 3.94 |
| 10 | `perf_test_record` | 642,772 | 561,855 | 61,825 | 10.40 | 9.09 |
| 100 | `perf_test_record` | 216,775 | 227,297 | 36,642 | 5.92 | 6.20 |
| 10 | `perf_test_record_arrays` | 448,273 | 403,900 | 50,579 | 8.86 | 7.99 |
| 100 | `perf_test_record_arrays` | 100,485 | 112,989 | 32,619 | 3.08 | 3.46 |

### Other Platforms

| Records | Function | AOT [1](#1-aot) | JIT [2](#2-jit) | EF [3](#3-ef) | ADO [4](#4-ado) | Django [5](#5-django) | Express [6](#6-express) | GO [7](#7-go) | FastAPI [8](#8-fastapi) |
| ------: | ---------: | ---------: | --------: | --------: | --------: | --------: | --------: | --------: | --------: |
| 10 | `perf_test` | 781,803 | 562,304 | 337,612 | 440,896 | 21,193 | 160,241 | 78,530 | 13,650 |
| 100 | `perf_test` | 307,427 | 303,692 | 235,331 | 314,198 | 18,345 | 58,130 | 55,119 | 9,666 |
| 10 | `perf_test_arrays` | 495,190 | 384,090 | 254,787 | 309,059 | 19,011 | 91,987 | N/A | 11,881 |
| 100 | `perf_test_arrays` | 124,912 | 127,707 | 113,663 | 130,471 | 11,452 | 17,896 | N/A | 6,192 |

### Consolidated Results

| Platform | 10 Records | 100 Records |
| -------- | ---------: | ----------: |
| **AOT** is an ahead-of-time native compilation of `NpgsqlRest`. `NpgsqlRest` compiled to the native binary. | **781,803** | **307,427** |
| **JIT** is a just-in-time compilation of `NpgsqlRest` to NET8 CLR (Common Language Runtime) on NET8 runtime. | **562,304**  | **303,692** |
| **ADO** is NET8 Raw ADO Data Reader approach. [Source](#4-ado) | 440,896 | 314,198 |
| **EF** is Entity Framework Core 8 on NET8 runtime. [Source](#3-ef) | 337,612 | 235,331 |
| **PostgREST** version 12.0.2 | 72,305 | 40,456 |
| **Django** REST Framework 4.2.10 on Python 3.8 [Source Link](5-django) | 21,193 | 18,345  |
| **Express** on NodeJS v20.11.1, express v4.18.3, pg 8.11.3 [Source Link](#6-express) | 160,241 | 55,119 |
| **GO** version go1.13.8 [Source Link](#7-go) | 78,530 | 55,119 |
| **FastAPI** version 0.110.0 on Python 3.8 [Source Link](#8-fastapi)| 13,650 | 9,666 |

#### 1) AOT

NpgsqlRest .NET8 AOT build is ahead-of-time (AOT) compiled to native code. AOT has an average **startup time of between 180 to 200 milliseconds.**

#### 2) JIT

NpgsqlRest JIT build is a Just-In-Time (JIT) compilation of Common Intermediate Language (CIL). JIT has an average **startup time of between 360 to 400 milliseconds.**

#### 3) EF

.NET8 Npgsql.EntityFrameworkCore.PostgreSQL 8.0.2

```csharp
var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<DbContext>(options => 
    options.UseNpgsql(connectionString));

var app = builder.Build();


app.MapPost("/api/perf_test", (DbContext dbContext, [FromBody]Params p) => dbContext.Database.SqlQuery<Table>(
        $"select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test(_records => {p._records}, _text_param => {p._text_param}, _int_param => {p._int_param}, _ts_param => {p._ts_param}, _bool_param => {p._bool_param})"));

app.MapPost("/api/perf_test_arrays", (DbContext dbContext, [FromBody]Params p) => dbContext.Database.SqlQuery<TableWithArrays>(
        $"select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test_arrays(_records => {p._records}, _text_param => {p._text_param}, _int_param => {p._int_param}, _ts_param => {p._ts_param}, _bool_param => {p._bool_param})"));

app.Run();
```

#### 4) ADO

.NET8 Raw Data ADO Reader:

```csharp
var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var app = builder.Build();


app.MapPost("/api/perf_test", (DbContext dbContext, [FromBody]Params p) => Data.GetTableData(p));
app.MapPost("/api/perf_test_arrays", (DbContext dbContext, [FromBody]Params p) => Data.GetTableArrayData(p));

app.Run();

static class Data
{
    public static async IAsyncEnumerable<Table> GetTableData(Params p)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test(@_records, @_text_param, @_int_param, @_ts_param, @_bool_param)";

        command.Parameters.AddWithValue("_records", p._records);
        command.Parameters.AddWithValue("_text_param", p._text_param);
        command.Parameters.AddWithValue("_int_param", p._int_param);
        command.Parameters.AddWithValue("_ts_param", p._ts_param);
        command.Parameters.AddWithValue("_bool_param", p._bool_param);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            yield return new Table
            {
                id1 = reader.GetInt32(0),
                foo1 = reader.IsDBNull(1) ? null : reader.GetString(1),
                bar1 = reader.IsDBNull(2) ? null : reader.GetString(2),
                datetime1 = reader.GetDateTime(3),
                id2 = reader.GetInt32(4),
                foo2 = reader.IsDBNull(5) ? null : reader.GetString(5),
                bar2 = reader.IsDBNull(6) ? null : reader.GetString(6),
                datetime2 = reader.GetDateTime(7),
                long_foo_bar = reader.IsDBNull(8) ? null : reader.GetString(8),
                is_foobar = reader.GetBoolean(9)
            };
        }
    }

    public static async IAsyncEnumerable<TableWithArrays> GetTableArrayData(Params p)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test_arrays(@_records, @_text_param, @_int_param, @_ts_param, @_bool_param)";

        command.Parameters.AddWithValue("_records", p._records);
        command.Parameters.AddWithValue("_text_param", p._text_param);
        command.Parameters.AddWithValue("_int_param", p._int_param);
        command.Parameters.AddWithValue("_ts_param", p._ts_param);
        command.Parameters.AddWithValue("_bool_param", p._bool_param);

        using var reader = await command.ExecuteReaderAsync();
        var result = new List<TableWithArrays>();

        while (await reader.ReadAsync())
        {
            yield return new TableWithArrays
            {
                id1 = reader.IsDBNull(0) ? null : (int[])reader.GetValue(0),
                foo1 = reader.IsDBNull(1) ? null : (string[])reader.GetValue(1),
                bar1 = reader.IsDBNull(2) ? null : (string[])reader.GetValue(2),
                datetime1 = reader.IsDBNull(3) ? null : (DateTime[])reader.GetValue(3),
                id2 = reader.IsDBNull(4) ? null : (int[])reader.GetValue(4),
                foo2 = reader.IsDBNull(5) ? null : (string[])reader.GetValue(5),
                bar2 = reader.IsDBNull(6) ? null : (string[])reader.GetValue(6),
                datetime2 = reader.IsDBNull(7) ? null : (DateTime[])reader.GetValue(7),
                long_foo_bar = reader.IsDBNull(8) ? null : (string[])reader.GetValue(8),
                is_foobar = reader.IsDBNull(9) ? null : (bool[])reader.GetValue(9)
            };
        }
    }
}
```

#### 5) Django

Django REST Framework 4.2.10 on Python 3.8

```python
from rest_framework.views import APIView
from rest_framework.response import Response
from django.db import connection

class PerfTestView(APIView):
    def post(self, request):
        records = request.data.get('_records', 10)
        text_param = request.data.get('_text_param', 'abcxyz')
        int_param = request.data.get('_int_param', 999)
        ts_param = request.data.get('_ts_param', '2024-01-01')
        bool_param = request.data.get('_bool_param', True)

        with connection.cursor() as cursor:
            cursor.execute(
                "select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test(_records => %s, _text_param => %s, _int_param => %s, _ts_param => %s, _bool_param => %s)",
                [records, text_param, int_param, ts_param, bool_param])
            data = cursor.fetchall()

        columns = [col[0] for col in cursor.description]
        return Response([dict(zip(columns, row)) for row in data])

class PerfTestArrays(APIView):
    def post(self, request):
        records = request.data.get('_records', 10)
        text_param = request.data.get('_text_param', 'abcxyz')
        int_param = request.data.get('_int_param', 999)
        ts_param = request.data.get('_ts_param', '2024-01-01')
        bool_param = request.data.get('_bool_param', True)

        with connection.cursor() as cursor:
            cursor.execute(
                "select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test_arrays(_records => %s, _text_param => %s, _int_param => %s, _ts_param => %s, _bool_param => %s)",
                [records, text_param, int_param, ts_param, bool_param])
            data = cursor.fetchall()

        columns = [col[0] for col in cursor.description]
        return Response([dict(zip(columns, row)) for row in data])
```

#### 6) Express

NodeJS v20.11.1, express v4.18.3, pg 8.11.3

```js
app.post('/api/perf_test', async (req, res) => {
  try {
    const { _records, _text_param, _int_param, _ts_param, _bool_param } = req.body;
    const queryResult = await pool.query(
      'select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test($1, $2, $3, $4, $5)', 
      [_records, _text_param, _int_param, _ts_param, _bool_param]);
    res.json(queryResult.rows);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

app.post('/api/perf_test_arrays', async (req, res) => {
  try {
    const { _records, _text_param, _int_param, _ts_param, _bool_param } = req.body;
    const queryResult = await pool.query(
      'select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test_arrays($1, $2, $3, $4, $5)', 
      [_records, _text_param, _int_param, _ts_param, _bool_param]);
    res.json(queryResult.rows);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});
```

#### 7) GO

go version go1.13.8

Note: array function endpoint tests are skipped.

```go
package main

import (
    "database/sql"
    "encoding/json"
    "log"
    "net/http"

    "github.com/gorilla/mux"
    _ "github.com/lib/pq"
)

const (
    host     = "127.0.0.1"
    port     = "5432"
    user     = "postgres"
    password = "postgres"
    dbname   = "perf_tests"
)

type PerfTestResult struct {
    ID1           int     `json:"id1"`
    Foo1          string  `json:"foo1"`
    Bar1          string  `json:"bar1"`
    Datetime1     string  `json:"datetime1"`
    ID2           int     `json:"id2"`
    Foo2          string  `json:"foo2"`
    Bar2          string  `json:"bar2"`
    Datetime2     string  `json:"datetime2"`
    LongFooBar    string  `json:"long_foo_bar"`
    IsFooBar      bool    `json:"is_foobar"`
}

func main() {
    // Initialize a new router
    router := mux.NewRouter()

    // Define your endpoint
    router.HandleFunc("/api/perf_test", PerfTestFunction).Methods("POST")

    // Start the server
    log.Fatal(http.ListenAndServe(":8080", router))
}

func PerfTestFunction(w http.ResponseWriter, r *http.Request) {
    // Parse JSON parameters from request body
    var params map[string]interface{}
    if err := json.NewDecoder(r.Body).Decode(&params); err != nil {
        http.Error(w, err.Error(), http.StatusBadRequest)
        return
    }

    // Connect to PostgreSQL database
    connStr := "host=" + host + " port=" + port + " user=" + user + " password=" + password + " dbname=" + dbname + " sslmode=disable"
    db, err := sql.Open("postgres", connStr)
    if err != nil {
        http.Error(w, err.Error(), http.StatusInternalServerError)
        return
    }
    defer db.Close()

    // Call PostgreSQL function
    rows, err := db.Query("SELECT id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test($1, $2, $3, $4, $5)", 
        params["_records"], params["_text_param"], params["_int_param"], params["_ts_param"], params["_bool_param"])
    if err != nil {
        http.Error(w, err.Error(), http.StatusInternalServerError)
        return
    }
    defer rows.Close()

    // Prepare the result slice
    var results []PerfTestResult

    // Iterate over the rows returned by the query
    for rows.Next() {
        var result PerfTestResult
        if err := rows.Scan(
            &result.ID1, &result.Foo1, &result.Bar1, &result.Datetime1,
            &result.ID2, &result.Foo2, &result.Bar2, &result.Datetime2,
            &result.LongFooBar, &result.IsFooBar,
        ); err != nil {
            http.Error(w, err.Error(), http.StatusInternalServerError)
            return
        }
        results = append(results, result)
    }

    // Check for errors during row iteration
    if err := rows.Err(); err != nil {
        http.Error(w, err.Error(), http.StatusInternalServerError)
        return
    }

    // Convert the result slice to JSON
    jsonResponse, err := json.Marshal(results)
    if err != nil {
        http.Error(w, err.Error(), http.StatusInternalServerError)
        return
    }

    // Set Content-Type header and write response
    w.Header().Set("Content-Type", "application/json")
    w.Write(jsonResponse)
}
```

#### 8) FastAPI

FastAPI 0.110.0 on Python 3.8

```python
@app.post("/api/perf_test")
async def perf_test(request: Request):
    conn = get_db_connection()
    cursor = conn.cursor()
    json = await request.json()
    with conn.cursor() as cursor:
        cursor.execute(
            "select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test(_records => %s, _text_param => %s, _int_param => %s, _ts_param => %s, _bool_param => %s)",
            [json["_records"], json["_text_param"], json["_int_param"], json["_ts_param"], json["_bool_param"]])
        return cursor.fetchall()

@app.post("/api/perf_test_arrays")
async def perf_test(request: Request):
    conn = get_db_connection()
    cursor = conn.cursor()
    json = await request.json()
    with conn.cursor() as cursor:
        cursor.execute(
            "select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from perf_test_arrays(_records => %s, _text_param => %s, _int_param => %s, _ts_param => %s, _bool_param => %s)",
            [json["_records"], json["_text_param"], json["_int_param"], json["_ts_param"], json["_bool_param"]])
        return cursor.fetchall()
```

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