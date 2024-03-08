# Changelog

## Version [2.0.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.0.0) (2024-03-10)

Version 2.0.0 is the major redesign of the entire library. This version adds extendibility by introducing the **concept of plugins.**

There are two types of plugins:

### 1) Routine Source Plugins

The concept of routine represents something that is executed on the PostgreSQL server when the API endpoint is called. That may be a PostgreSQL function or procedure but it may be also a custom PostgreSQL query or a command.

The routine source is the source of information on available routines (based on the configuration) upon which REST API is built.

In the previous version of the library, there was only one routine source, which was PostgreSQL functions and procedures. Based on the given configuration, REST API was built on the available PostgreSQL functions and procedures.

In this version, the routine source is abstracted and it's possible to 

### 2) Code Generation Plugins

### Library Design

-----------

## Version [1.6.3](https://github.com/vb-consulting/NpgsqlRest/tree/1.6.3) (2024-02-19)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/1.6.2...1.6.3)

## Uptime Improved

The query that returns metadata for existing routines is heavily optimized. There were some problematic subqueries that caused execution to run more than one second.

As a result, uptime is slashed from more than one second to milliseconds.

## Npgsql Reference Update

- Npgsql 8.0.1 -> 8.0.2

Any dependency shall require an upgrade too.

## Version [1.6.2](https://github.com/vb-consulting/NpgsqlRest/tree/1.6.2) (2024-02-03)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/1.6.1...1.6.2)

- Fixed and tested full support for PostgreSQL stored procedures (as opposed to user-defined functions which are used primarily).

-----------

## Version [1.6.1](https://github.com/vb-consulting/NpgsqlRest/tree/1.6.1) (2024-02-02)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/1.6.0...1.6.1)

### 1) Smaller Options Tweaks

The following options will be ignored under these conditions:

- `SchemaSimilarTo`: ignore if the string is empty.
- `SchemaNotSimilarTo`: ignore if the string is empty.
- `IncludeSchemas`: ignore if the array is empty.
- `ExcludeSchemas`: ignore if the array is empty.
- `NameSimilarTo`: ignore if the string is empty.
- `NameNotSimilarTo`: ignore if the string is empty.
- `IncludeNames`: ignore if the array is empty.
- `ExcludeNames`: ignore if the array is empty.

Previously, they were ignored only when they were NULL.

### 2) Logging Improvements

NpgsqlRest default logger is now created at the build stage by the default application logger factory. That means when the default ASP.NET application is configured then:

a) The Logger name is now by default the same as the default namespace of the library which is `NpgsqlRest`.

Previously, the default application logger was used and the default name was equal to your application default logger which made it harder to distinguish and configure.

It's possible to set a custom logger name with the new configuration option: `LoggerName`.
The `LoggerName` when set to null (default) will use the default logger name which is `NpgsqlRest` (the default namespace).

b) Since the logger is created with a different name, now it's possible to apply configuration from the configuration file, such as `appsettings.json` file:

```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "NpgsqlRest": "Warning"
    }
  }
```

This example configures the `NpgsqlRest` logger to the warning level only.

### 3) LogCommands Includes Request Info

When logging commands (`LogCommands` option set to true), request info (method and the URL) will be included in log output:

```console
info: NpgsqlRest[0]
      -- POST http://localhost:5000/api/perf-test
      -- $1 integer = 1
      -- $2 text = 'XYZ'
      -- $3 integer = 3
      -- $4 timestamp without time zone = '2024-04-04T03:03:03'
      -- $5 boolean = false
      select id1, foo1, bar1, datetime1, id2, foo2, bar2, datetime2, long_foo_bar, is_foobar from public.perf_test($1, $2, $3, $4, $5)

info: NpgsqlRest[0]
      -- GET http://localhost:5000/api/get-csv-data
      select id, name, date, status from public.get_csv_data()
```

### 3) Fix Using Identifiers

PostgreSQL allows language identifiers to be used as names with double quotes. For example, a function that uses `select`, `group`, `order`, `from`, `join` as names:

```sql
create function "select"("group" int, "order" int) 
returns table (
    "from" int,
    "join" int
)
language plpgsql
as 
$$
begin
    return query 
    select t.*
    from (
        values 
        ("group", "order")
    ) t;
end;
$$;
```

This type of function would cause errors in previous versions. This is now fixed and it works normally.


### 4) Other Changes

- `DefaultUrlBuilder` and `DefaultNameConverter` static classes made public.
- Removed a need for suppression of AOT warning in an HTTP file handler module when getting the host from the configuration.

-----------

## Version [1.6.0](https://github.com/vb-consulting/NpgsqlRest/tree/1.6.0) (2024-28-01)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/1.5.1...1.6.0)

### 1) HTTP File Improvements

- HTTP file generation will now consider if the endpoint has configured a `BodyParameterName` - the name of the parameter that is configured to be sent by the request body.

For example, if the function `body_param` has two parameters (`_i` and `_p`), and `_p` is configured to be sent by the request body, the HTTP file builder will create this callable example:

```console
POST {{host}}/api/body-param?i=1

XYZ
```

Note, that parameter `_p` is set to request body as `XYZ`.

- HTTP file endpoints with comment header will now optionally include a routine database comment. In the example above comment header is:

```console
HTTP
body-param-name _p
```

And resulting HTTP file generated callable endpoint with comment will now look like this:

```console
// function public.body_param(
//     _i integer,
//     _p text
// )
// returns text
//
// comment on function public.body_param is '
// HTTP
// body-param-name _p';
POST {{host}}/api/body-param?i=1

XYZ
```

This also applies to the full header comments.

This feature can be changed with `CommentHeaderIncludeComments` boolean option, in the `HttpFileOptions` options (default is true). Example:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    HttpFileOptions = new() 
    { 
        // turn off http file comments in header
        CommentHeaderIncludeComments = false
    }
});
```

### 2) Comment Annotation Breaking Change

Comment annotations that are not `HTTP` (for example authorize, etc) don't require `HTTP` line above. 

For example, before:

```sql
comment on function comment_authorize() is '
HTTP
Authorize';
```

And now it is enough to do:

```sql
comment on function comment_authorize() is 'Authorize';
```

Reason: it was really stupid, this is much better.

### 3) Logging Commands

There is a new option named `LogCommands` that, (well, obviously duh), enables logging command text before the execution, together with parameter values. The default is false (since it has a slight performance impact).

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    LogCommands = true,
});
```

Examples:

```console
// function public.case_get_default_params(
//     _p1 integer,
//     _p2 integer DEFAULT 2,
//     _p3 integer DEFAULT 3,
//     _p4 integer DEFAULT 4
// )
// returns json
GET {{host}}/api/case-get-default-params?p1=1&p2=2&p3=3&p4=4
```

Execution produces the following log:

```console
info: NpgsqlRestTestWebApi[0]
      -- $1 integer = 1
      -- $2 integer = 2
      -- $3 integer = 3
      -- $4 integer = 4
      select public.case_get_default_params($1, $2, $3, $4)
```

And of course, complicated and complex types are also supported:

```console
info: NpgsqlRestTestWebApi[0]
      -- $1 smallint = 1
      -- $2 integer = 2
      -- $3 bigint = 3
      -- $4 numeric = 4
      -- $5 text = 'XYZ'
      -- $6 character varying = 'IJK'
      -- $7 character = 'ABC'
      -- $8 json = '{}'
      -- $9 jsonb = '{}'
      -- $10 smallint[] = {10,11,12}
      -- $11 integer[] = {11,12,13}
      -- $12 bigint[] = {12,13,14}
      -- $13 numeric[] = {13,14,15}
      -- $14 text[] = {'XYZ','IJK','ABC'}
      -- $15 character varying[] = {'IJK','ABC','XYZ'}
      -- $16 character[] = {'ABC','XYZ','IJK'}
      -- $17 json[] = {'{}','{}','{}'}
      -- $18 jsonb[] = {'{}','{}','{}'}
      select public.case_get_multi_params1($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, $17, $18)
```
```console
info: NpgsqlRestTestWebApi[0]
      -- $1 real = 1.2
      -- $2 double precision = 2.3
      -- $3 jsonpath = '$.user.addresses[0].city'
      -- $4 timestamp without time zone = '2024-04-04T03:03:03'
      -- $5 timestamp with time zone = '2024-05-05T04:04:04.0000000Z'
      -- $6 date = '2024-06-06'
      -- $7 time without time zone = '06:06'
      -- $8 time with time zone = '07:07:00'
      -- $9 interval = '8 minutes 9 seconds'
      -- $10 boolean = true
      -- $11 uuid = '00000000-0000-0000-0000-000000000000'
      -- $12 bit varying = '101'
      -- $13 varbit = '010'
      -- $14 inet = '192.168.5.13'
      -- $15 macaddr = '00-B0-D0-63-C2-26'
      -- $16 bytea = '\\xDEADBEEF'
      select public.case_get_multi_params2($1, $2, $3, $4, $5, $6, $7, $8, $9::interval, $10, $11, $12::bit varying, $13::varbit, $14::inet, $15::macaddr, $16::bytea)
```

### 4) Command Callback

New feature: 
Now it's possible to define a lambda callback in options, that, if not null - will be called after every command is created and before it is executed.

This option has the following signature:

```csharp
/// <summary>
/// Command callback, if not null, will be called after every command is created and before it is executed.
/// Setting the the HttpContext response status or start writing response body will the default command execution.
/// </summary>
public Func<(Routine routine, NpgsqlCommand command, HttpContext context), Task>? CommandCallbackAsync { get; set; } = commandCallbackAsync;
```

- The input parameter is a **named tuple** `(Routine routine, NpgsqlCommand command, HttpContext context)`.
- The expected result is `Task` since it must be an async function.

If something is written into the `HttpContext` response, execution will skip the default behavior and exit immediately. This provides a chance to write a custom implementation logic.

For example, we have an ordinary function that returns some table:

```sql
create function get_csv_data() 
returns table (id int, name text, date timestamp, status boolean)
language sql as 
$$
select * from (
    values 
    (1, 'foo', '2024-01-31'::timestamp, true), 
    (2, 'bar', '2024-01-29'::timestamp, true), 
    (3, 'xyz', '2024-01-25'::timestamp, false)
) t;
$$;
```

And only for this function, we don't want JSON, we want CSV. We can do this now:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    CommandCallbackAsync = async p =>
    {
        if (string.Equals(p.routine.Name , "get_csv_data"))
        {
            p.context.Response.ContentType = "text/csv";
            await using var reader = await p.command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var line = $"{reader[0]},{reader[1]},{reader.GetDateTime(2):s},{reader.GetBoolean(3).ToString().ToLowerInvariant()}\n";
                await p.context.Response.WriteAsync(line);
            }
        }
    }
});
```

And, when we do the `GET {{host}}/api/get-csv-data`, the response will be:

```console
HTTP/1.1 200 OK
Content-Type: text/csv
Date: Sun, 28 Jan 2024 13:21:57 GMT
Server: Kestrel
Transfer-Encoding: chunked

1,foo,2024-01-31T00:00:00,true
2,bar,2024-01-29T00:00:00,true
3,xyz,2024-01-25T00:00:00,false
```

This is a bit advanced feature, but a useful one.

-----------

## Version [1.5.1](https://github.com/vb-consulting/NpgsqlRest/tree/1.5.1) (2024-27-01)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/1.5.0...1.5.1)

Small fix for JSON returning time stamps in an array. 

Previously, the JSON response didn't have T between date and time, which is required by the  ISO 8601 standard.

```
"{\"2024-01-24 00:00:00\",\"2024-01-20 00:00:00\",\"2024-01-21 00:00:00\"}"
```

Now it's fixed:

```
"{\"2024-01-24T00:00:00\",\"2024-01-20T00:00:00\",\"2024-01-21T00:00:00\"}"
```

-----------

## Version [1.5.0](https://github.com/vb-consulting/NpgsqlRest/tree/1.5.0) (2024-27-01)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/1.4.0...1.5.0)

### 1) New Feature: Strict Function Support

Functions in strict mode are functions that are declared with the `STRICT` or with `RETURNS NULL ON NULL INPUT` keyword. 

These functions are **not executed** at all if any of the parameters are NULL.

```sql
create function strict_function(_p1 int, _p2 int) 
returns text 
strict
language plpgsql
as 
$$
begin
    raise info '_p1 = %, _p2 = %', _p1, _p2;
    return 'strict';
end;
$$;

create function returns_null_on_null_input_function(_p1 int, _p2 int)
  returns text
  returns null on null input
  language plpgsql
as 
$$
begin
    raise info '_p1 = %, _p2 = %', _p1, _p2;
return 'returns null on null input';
end;
$$;
```

If any of these functions were called with NULL:

```sql
select strict_function(1,null);
select strict_function(null,1);
select strict_function(null,null);
select returns_null_on_null_input_function(1,null);
select returns_null_on_null_input_function(null,1);
select returns_null_on_null_input_function(null,null);
```

Then function will not be executed at all no info will be emitted with `raise info` since nothing is executed. 

In this version, NpgsqlRest will return `HTTP 204 NoContent` response if any of the parameters are NULL. and it will avoid calling database in the first place. 

### 2) Other Improvements

This release features a lot of internal refactoring and code cleanup.

-----------

## Version [1.4.0](https://github.com/vb-consulting/NpgsqlRest/tree/1.4.0) (2024-26-01)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/1.3.0...1.4.0)

### 1) Improved Untyped Functions

In the previous version, untyped functions were returning a JSON array where each element was an entire raw record as returned from PostgreSQL encoded into JSON string:

Following raw output: 

```console
(1,one)
(2,two)
(3,three)
```

would produce this JSON response: `["1,one","2,two","3,three"]`.

However, this response is not very usable, because clients still need to process each record which can be pretty tricky for many reasons:
For example, comma symbols are encoded into double-quoted strings: `foo,bar` -> `"foo,bar"`, which makes them very hard to split values by as CSV strings, and many other reasons.

In this version, each record is encoded into a JSON array and each element into its own JSON string. The example above now looks like (formatted):

```json
[
    ["1", "one"],
    ["2", "two"],
    ["3", "three"]
]
```

Note that are all types now encoded as strings, regardless of the actual type. This is because, as far as I know, it is impossible to figure out actual types from the untyped function with the `Npgsql` data driver.

This may create awkward situations, because, for example, the raw output from PostgreSQL for the `boolean true` value and `text 't'` value are identical `t` and will be encoded also identical `"t"`. The assumption is that clients will know what types and values should they expect and act accordingly. 

This makes the use of untyped functions a very powerful approach, because the actual result table definition, doesn't have to be explicitly defined every time:

```sql
create function get_test_records() 
returns setof record
language sql
as 
$$
select * from (values 
    (1, 'one'), 
    (2, 'two'), 
    (3, 'three')
) v;
$$;
```

### 2) Improved Logging

There are also some improvements in the logging mechanism and new logging options:

- When the endpoint attribute is set through the comment annotation, this event is now logged as an information-level log. Previously, it was hard to tell did we set the endpoint attribute or not, because, when an annotation is wrong, it is interpreted as plain comment and ignored. Now it's easy.
- We can turn off or on this feature with the `LogAnnotationSetInfo` build option. The default is true.
- There is also another new build option to control logging: `LogEndpointCreatedInfo`. Controls should we log the endpoint creation or not? The default is true.

----------

## Version [1.3.0](https://github.com/vb-consulting/NpgsqlRest/tree/1.3.0) (2024-23-01)

### 1) Support For Variadic Parameters

```sql
create function variadic_param_plus_one(variadic v int[]) 
returns int[] 
language sql
as 
$$
select array_agg(n + 1) from unnest($1) AS n;
$$;
```
```csharp
[Fact]
public async Task Test_variadic_param_plus_one()
{
    using var body = new StringContent("{\"v\": [1,2,3,4]}", Encoding.UTF8);
    using var response = await test.Client.PostAsync("/api/variadic-param-plus-one", body);
    var content = await response.Content.ReadAsStringAsync();

    response?.StatusCode.Should().Be(HttpStatusCode.OK);
    content.Should().Be("[2,3,4,5]");
}
```

### 2) Support For Table-Valued Functions

It seems that support for functions returning existing tables (set of table, or table-valued functions) was accidentally missing. This is now fixed. 

Example:

```sql
create table test_table (
    id int,
    name text not null
);

insert into test_table values (1, 'one'), (2, 'two'), (3, 'three');

create function get_test_table() 
returns setof test_table
language sql
as 
$$
select * from test_table;
$$;
```
```csharp
[Fact]
public async Task Test_get_test_table()
{
    using var response = await test.Client.GetAsync("/api/get-test-table");
    var content = await response.Content.ReadAsStringAsync();

    response?.StatusCode.Should().Be(HttpStatusCode.OK);
    response?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
    content.Should().Be("[{\"id\":1,\"name\":\"one\"},{\"id\":2,\"name\":\"two\"},{\"id\":3,\"name\":\"three\"}]");
}
```

### 2) Support for Untyped Functions

Untyped functions are functions that return a set of records of the unknown type. 

For example:

```sql
create function get_test_records() 
returns setof record
language sql
as 
$$
select * from (values (1, 'one'), (2, 'two'), (3, 'three')) v;
$$;
```

These cannot be called without the result column definition list:

```sql
select * from get_test_records()

-- throws error:
-- ERROR:  a column definition list is required for functions returning "record"
-- LINE 1: select * from get_test_records()

select * from get_test_records() as v(a int, b text)
-- returns result with columns a int, b text
```

There is no way of knowing which results these functions will return (as far as I know) and the only way to return a workable set is to execute it as a scalar function:

```sql
select get_test_records() 

-- returns values:
(1,one)
(2,two)
(3,three)
```

These are raw record values as returned from PostgreSQL without any parsing (for example text is not quoted). When NpgsqlRest executes this type of function it will return a JSON array with raw values from the PostgreSQL server without left and right parenthesis:

```csharp
[Fact]
public async Task Test_get_test_records()
{
    using var response = await test.Client.GetAsync("/api/get-test-records");
    var content = await response.Content.ReadAsStringAsync();

    response?.StatusCode.Should().Be(HttpStatusCode.OK);
    response?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
    content.Should().Be("[\"1,one\",\"2,two\",\"3,three\"]");
}
```

----------

## Version 1.2.0 (2024-22-01)

### 1) Fix AOT Warnings

A couple of issues have caused AOT warnings, although AOT worked before. This is now fixed:

#### 1) JSONify Strings

```csharp
[JsonSerializable(typeof(string))]
internal partial class NpgsqlRestSerializerContext : JsonSerializerContext { }

private static readonly JsonSerializerOptions plainTextSerializerOptions = new()
{
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    TypeInfoResolver = new NpgsqlRestSerializerContext()
};

// 
// AOT works but still emmits warnings IL2026 and IL3050
// 
JsonSerializer.Serialize(str, plainTextSerializerOptions)
```

This is fixed with suppression:

```csharp
[UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
[UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamic",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
private static string SerializeString(ref string value) => 
    JsonSerializer.Serialize(value, plainTextSerializerOptions);
```

#### 2) HTTP file endpoint

HTTP file endpoint was mapped with `MapGet`, and although it worked fine with AOT it emitted AOT warnings.

This is replaced with a middleware interceptor:

```csharp
builder.Use(async (context, next) =>
{
    if (!(string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) && 
        string.Equals(context.Request.Path, path, StringComparison.Ordinal)))
    {
        await next(context);
        return;
    }
    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync(content.ToString());
});
```

This resolved the warning but also removed the need for a routing component, which can make AOT builds even leaner and smaller:

```csharp
var builder = WebApplication.CreateEmptyBuilder(new ());
builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();
// not needed any more
// builder.Services.AddRoutingCore();
```

#### 3) Configuration Get Value From String

Method `GetHost` used `app.Configuration.GetValue<string>("ASPNETCORE_URLS")` configuration read which issued AOT warnings but it worked since it uses a string. This is suppressed with `[UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode", Justification = "Configuration.GetValue only uses string type parameters")]`.

### 2) Fix Arrays Encodings In Http Files

Fixed arrays encoding in query string parameters of the generated HTTP file sample.

### 3) Changed Method Type Logic

Change in logic that determines the endpoint default method:

- Before: endpoint is GET if the routine name contains string `get`.
- Now: it's complicated.

The endpoint is now GET if the routine name either:
- Starts with `get_` (case insensitive).
- Ends with `_get` (case insensitive).
- Contains `_get_` (case insensitive).

Otherwise, the endpoint is POST if the volatility option is VOLATILE. This assumes that the routine is changing some data, and that is also how PostgREST works and therefore endpoint is POST. Otherwise, it is GET.

You can see the source code here: [/DefaultEndpoint.cs](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/DefaultEndpoint.cs#L65)

This change may be breaking change in some rare circumstances (all 88 tests are passing without a change), but luckily not too many people are using this library at this point.

### 4) URL Path Handling Changes

Generated URLs by the default [DefaultUrlBuilder](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/DefaultUrlBuilder.cs#L5) will no longer add a slash character (`/`) at the end of the line.

However, when middleware tries to match the requested path, both versions will be matched (with an ending slash and without an ending slash).

That means that function with the signature `public.`test_func()` will match successfully both of these paths:
- `/api/test-func`
- `/api/test-func/`

But only one will be generated in an HTTP file: `/api/test-func`.

### 5) Fix Sending Request Headers With Parameter

There was a bug with the endpoint option to send request headers JSON through a routine parameter. It seems that the parameter name was always the default. This is fixed and [tested](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRestTests/RequestHeadersTests.cs#L115).

### 6) Sending Request Headers Parameter Defaults

Sending request headers with a routine parameter doesn't require that parameter to have a default value defined anymore.

### 7) New Feature: Body Parameter Name

There is a new feature that allows binding an entire body content to a particular routine parameter.

This is an endpoint-level value called `BodyParameterName`, which is NULL by default. 

- When this value is NULL (default), no parameter is assigned from body content. The same rules apply as before, nothing is changed.
- When this value is not NULL then:
  - If the endpoint has `RequestParamType` set to `BodyJson` it will be changed automatically to `QueryString` and a warning log will be emitted.
  - A parameter named `BodyParameterName` (matches original and converted names) will be assigned from the entire request body content.
  - If body content is NULL (not set), the parameter value will be NULL, and it will be always assigned from a body.
  - All other parameters will be assigned from the query string values (`RequestParamType = RequestParamType.QueryString`).

Example:

```sql
create function example_function(_p1 int, _content text) 
returns void 
language plpgsql
as 
$$
begin
    -- do something with _p1, and _content parameters
    raise info 'received % from _p1 and % from content', _p1, _content;
end;
$$;
```

```csharp
//
// Configure endpoint from example_function function to bind parameter _content from request body
//
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreated = (routine, endpoint) =>
    {
        if (routine.Name == "example_function")
        {
            return endpoint with { BodyParameterName = "_content" };
        }
        return endpoint;
    }
});
```

Note that this would also work if we used converted parameter name `content` (assuming we use the default converter that converts names to lower camel case).

This value is also configurable with comment annotations:

```sql
comment on function example_function(int, text) is '
HTTP
body-param-name content
';
```

In this comment annotation a keyword that comes before the parameter name could be any of these: `BodyParameterMame`, `body-parameter-name`, `body_parameter_name`, `BodyParamMame`, `body-param-name` or `body_param_name`.

----------

## Version 1.1.0 (2024-19-01)

### 1) RoutineEndpoint Type Change

`RoutineEndpoint` type changed to `readonly record struct`

This allows for the manipulation of the endpoint parameter in the `EndpointCreated` callback event. 

This is useful to force certain endpoint configurations from the code, rather than through comment annotations.

Example:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreated = (routine, endpoint) =>
    {
        if (routine.SecurityType == SecurityType.Definer)
        {
            // filter out routines that can run as the definer user (this is usually superuser)
            return null;
        }
        if (string.Equals(routine.Name, "get_data", StringComparison.Ordinal))
        {
            // override the default response content type to be csv and don't rquire authorization 
            return endpoint with
            { 
                RequiresAuthorization = false, 
                ResponseContentType = "text/csv"
            };
        }
        // require authorization for all endpoints and force GET method
        return endpoint with { RequiresAuthorization = true, Method = Method.GET };
    }
});
```

### 2) New Event Option EndpointsCreated

`EndpointsCreated` option event:

- If defined (not null) - will be executed after all endpoints have been created and are ready for execution. This happens during the build phase.

- Receives one immutable parameter array of routine and e+ndpioint tuples: `(Routine routine, RoutineEndpoint endpoint)[]`.

- The option has the following signature: `public Action<(Routine routine, RoutineEndpoint endpoint)[]>? EndpointsCreated { get; set; }`.

- Example:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointsCreated = (endpoints) =>
    {
        foreach (var (routine, endpoint) in endpoints)
        {
            Console.WriteLine($"{routine.Type} {routine.Schema}.{routine.Signature} is mapped to {endpoint.Method} {endpoint.Url}");
        }
    }
});
```

This is useful in situations when we want to generate and create source code files based on generated endpoints such as Typescript or C# interfaces for example. It enables further automatic code generation based on generated endpoints.

### 3) New Custom Logger Option

There is a new option called `Logger` with the following signature: `public ILogger? Logger { get; set; }`

When provided (not null), this is the logger that will be used by default for all logging. You can use this to provide a custom logging implementation:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    Logger = new EmptyLogger()
});

class EmptyLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // empty
    }
}
```

-----------