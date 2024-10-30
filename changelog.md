# Changelog

Note: For a changelog for a client application [see the client application page changelog](https://vb-consulting.github.io/npgsqlrest/client/#changelog).

---

## Version [2.12.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.12.0) (2024-10-29)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.11.0...2.12.0)

### Login Endpoint Custom Claims Maintain the Original Field Names

This is a small but breaking change that makes the system a bit more consistent.

For example, if you have a login endpoint that returns fields that are not mapped to any known [AD Federation Services Claim Types](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0#fields) like this:

```sql
create function auth.login(
    _username text,
    _password text
) 
returns table (
    status int,             -- indicates login status 200, 404, etc
    name_identifier text,   -- name_identifier when converted with default name converter becomes name_identifier and mapps to http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
    name text,              -- http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name
    role text[],            -- http://schemas.microsoft.com/ws/2008/06/identity/claims/role
    company_id int             -- company_id can't be mapped to AD Federation Services Claim Types
)

--
-- ... 
--

comment on function auth.login(text, text) is '
HTTP POST
Login
';
```

So, in this example, `company_id` can't be mapped to any AD Federation Services Claim Types, and a claim with a custom claim name will be created. In the previous version, that name was parsed with name converter, and the default name converter is the camel case converter, and the newly created claim name, in this case, was `companyId`.

This is now changed to use the original, unconverted column named, and the newly created claim name, in this case, is now `company_id`.

This is important with the client configuration when mapping a custom claim to a parameter name, like this:

```jsonc
{
    //...
    "NpgsqlRest": {
        //...
        "AuthenticationOptions": {
            //...
            "CustomParameterNameToClaimMappings": {
                "_company_id": "company_id"
            }
        }
    }
}
```

### Reader Optimizations and Provider-Specific Values

From this version, all command readers are using `GetProviderSpecificValue(int ordinal)` method instead of `GetValue(int ordinal)`.

This change ensures that data is retrieved in its native PostgreSQL format, preserving any provider-specific types. This change enhances accuracy and eliminates the overhead of automatic type conversion to standard .NET types.

Also, when reading multiple rows, the reader will now take advantage of the `GetProviderSpecificValues(Object[])` method to initialize the current row into an array. Previously, the entire row was read with `GetValue(int ordinal)` one by one. This approach **improves performances when reading multiple rows by roughly 10%** and slightly increases memory consumption.

### Updated Npgsql Reference

Npgsql reference updated from 8.0.3 to 8.0.5:

- 8.0.4 list of changes: https://github.com/npgsql/npgsql/milestone/115?closed=1
- 8.0.5 list of changes: https://github.com/npgsql/npgsql/milestone/118?closed=1

Any project using NpgsqlRest library will have to upgrade Npgsql minimal version to 8.0.5.

### Custom Types Support

From this version PostgreSQL custom types can be used as:

1) Parameters (single paramtere or combined).
2) Return types (single type or combined as a set).

Example:

```sql
create type my_request as (
    id int,
    text_value text,
    flag boolean
);

create function my_service(
    request my_request
)
returns void
language plpgsql as 
$$
begin
    raise info 'id: %, text_value: %, flag: %', request.id, request.text_value, request.flag;
end;
$$;
```

Normally, to call this function calling a `row` constructor is required and then casting it back to `my_request`:
```sql
select my_service(row(1, 'test', true)::my_request);
```

NpgslRest will construnct a valid endpoint where parameters names are constructed by following rule:
- Parameter name (in this example `request`) plus `CustomTypeParameterSeparator` value (default is underscore) plus custom type field name.

This gives as these three parameters:

1) `request_id`
2) `request_text_value`
3) `request_flag`

And when translated by the default name translator (camle case), we will get these three parameters:

1) `requestId`
2) `requestTextValue`
3) `requestFlag`

So, now, we have valid REST endpoint:

```csharp
using var body = new StringContent("""
{  
    "requestId": 1,
    "requestTextValue": "test",
    "requestFlag": true
}
""", Encoding.UTF8, "application/json");

using var response = await test.Client.PostAsync("/api/my-service", body);
response?.StatusCode.Should().Be(HttpStatusCode.NoContent); // void functions by default are returning 204 NoContent
```

Custom type parameters can be mixed with normal parameters:

```sql
create function my_service(
    a text,
    request my_request,
    b text
)
returns void
/*
... rest of the function
*/
```

In this example parameters `a` and `b` will behace as before, just three new parameters from the custom types will also be present.

Custom types can now also be a return value:

```sql
create function get_my_service(
    request my_request
)
returns my_request
language sql as 
$$
select request;
$$;
```

This will produce a valid json object, where properties are custom type fields, as we see in the client code test:

```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-my-service/{query}");
var content = await response.Content.ReadAsStringAsync();
response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("{\"id\":1,\"textValue\":\"test\",\"flag\":true}");
```

If return a set of, instead of single array, we will get an aray as expected:

```sql
create function get_setof_my_requests(
    request my_request
)
returns setof my_request
language sql as 
$$
select request union all select request;
$$;
```
```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-setof-my-requests/{query}");
var content = await response.Content.ReadAsStringAsync();

response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("[{\"id\":1,\"textValue\":\"test\",\"flag\":true},{\"id\":1,\"textValue\":\"test\",\"flag\":true}]");
```

The same, completely identical result - we will get if we return a table with a single custom type:

```sql
create function get_table_of_my_requests(
    request my_request
)
returns table (
    req my_request
)
language sql as 
$$
select request union all select request;
$$;
```
```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-table-of-my-requests/{query}");
var content = await response.Content.ReadAsStringAsync();

response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("[{\"id\":1,\"textValue\":\"test\",\"flag\":true},{\"id\":1,\"textValue\":\"test\",\"flag\":true}]");
```

In this case, the actual table field name `req` is ignored, and a single field is replace with trhee fields from the custom type.

When returning a table type, we can mix custom types with normal fields, for example:

```sql
create function get_mixed_table_of_my_requests(
    request my_request
)
returns table (
    a text,
    req my_request,
    b text
)
language sql as 
$$
select 'a1', request, 'b1'
union all 
select 'a2', request, 'b2'
$$;
```
```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-mixed-table-of-my-requests/{query}");
var content = await response.Content.ReadAsStringAsync();

response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("[{\"a\":\"a1\",\"id\":1,\"textValue\":\"test\",\"flag\":true,\"b\":\"b1\"},{\"a\":\"a2\",\"id\":1,\"textValue\":\"test\",\"flag\":true,\"b\":\"b2\"}]");
```

### Table Types Support

Table types were supported before, bot now, everything that is true for custom types is also true for table types. They can be used as:

1) Parameters (single paramtere or combined).
2) Return types (single type or combined as a set).

Examples:

```sql
create table my_table (
    id int,
    text_value text,
    flag boolean
);

create function my_table_service(
    request my_table
)
returns void
language plpgsql as 
$$
begin
    raise info 'id: %, text_value: %, flag: %', request.id, request.text_value, request.flag;
end;
$$;
```
```csharp
using var body = new StringContent("""
{  
    "requestId": 1,
    "requestTextValue": "test",
    "requestFlag": true
}
""", Encoding.UTF8, "application/json");

using var response = await test.Client.PostAsync("/api/my-table-service", body);
response?.StatusCode.Should().Be(HttpStatusCode.NoContent);
```

```sql
create function get_my_table_service(
    request my_table
)
returns my_table
language sql as 
$$
select request;
$$;
```
```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-my-table-service/{query}");
var content = await response.Content.ReadAsStringAsync();

response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("{\"id\":1,\"textValue\":\"test\",\"flag\":true}");
```

```sql
create function get_setof_my_tables(
    request my_table
)
returns setof my_table
language sql as 
$$
select request union all select request;
$$;
```
```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-setof-my-tables/{query}");
var content = await response.Content.ReadAsStringAsync();

response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("[{\"id\":1,\"textValue\":\"test\",\"flag\":true},{\"id\":1,\"textValue\":\"test\",\"flag\":true}]");
```

```sql
create function get_table_of_my_tables(
    request my_table
)
returns table (
    req my_table
)
language sql as 
$$
select request union all select request;
$$;
```
```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-table-of-my-tables/{query}");
var content = await response.Content.ReadAsStringAsync();

response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("[{\"id\":1,\"textValue\":\"test\",\"flag\":true},{\"id\":1,\"textValue\":\"test\",\"flag\":true}]");
```

```sql
create function get_mixed_table_of_my_tables(
    request my_table
)
returns table (
    a text,
    req my_table,
    b text
)
language sql as 
$$
select 'a1', request, 'b1'
union all 
select 'a2', request, 'b2'
$$;
```
```csharp
var query = new QueryBuilder
{
    { "requestId", "1" },
    { "requestTextValue", "test" },
    { "requestFlag", "true" },
};
using var response = await test.Client.GetAsync($"/api/get-mixed-table-of-my-tables/{query}");
var content = await response.Content.ReadAsStringAsync();

response?.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Be("[{\"a\":\"a1\",\"id\":1,\"textValue\":\"test\",\"flag\":true,\"b\":\"b1\"},{\"a\":\"a2\",\"id\":1,\"textValue\":\"test\",\"flag\":true,\"b\":\"b2\"}]");
```

### RoutineSources Options Property

The `RoutineSources` options property access was changed from internal to public. This property defines routine sources (functions and procedures and/or CRUD table source) that will be used to create REST API endpoints. Default is functions and procedures.

This change was made to facilitate easier client configuration.

### Routine Source Options

`RoutineSource` class, which is used to build REST endpoints from PostgreSQL functions and procedures have some new options. These were added to serve client applications better. These new options are:

- `CustomTypeParameterSeparator`

This values is used as a separator between parameter name and a custom type (or table type) field name. See changes in parameter creation above. Default is underscore `_`. 

- `IncludeLanguagues`

Include functions and procedures with these languagues (case insensitive). Default is null (all are included). 

- `ExcludeLanguagues`

Exclude functions and procedures with these languagues (case insensitive). Default is C and internal (array `['c', 'internal']`).

### Reference Update

Npgsql reference from 8.0.3 to 8.0.5.

---

## Version [2.11.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.11.0) (2024-09-03)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.10.0...2.11.0)

1) Default value for the `CommentsMode` is changed. For now on, default value for this option is more restrictive `OnlyWithHttpTag` instead of previously `ParseAll`.
2) New routine endpoint option `bool RawColumnNames` (default false) with the following annotation `columnnames` or `column_names` or `column-names`. If this option is set to true (in code or with comment annotation) - and if the endpoint is int the "raw" mode - the endpoint will contain a header names. If separators are applied, they will be used also.

---

## Version [2.10.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.10.0) (2024-08-06)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.9.0...2.10.0)

### New Routine Endpoint Options With Annotations

These two options will only apply when [`raw`](https://vb-consulting.github.io/npgsqlrest/annotations/#raw) is on. Also, comment annotation values will accept standard escape sequences such as `\n`, `\r` or `\t`, etc.

These are:

- `RoutineEndpoint` option `public string? RawValueSeparator { get; set; } = null;` that maps to new comment annotation `separator`

Defines a standard separator between raw values.

- `RoutineEndpoint` option `public string? RawNewLineSeparator { get; set; } = null;` that maps to new comment annotation `newline`

Defines a standard separator between raw value columns.

### Dynamic Custom Header Values

In this version, when you define custom headers, either trough:

- [`CustomRequestHeaders` option](https://vb-consulting.github.io/npgsqlrest/options/#customrequestheaders) for all requests.
- On [`EndpointsCreated` option event](https://vb-consulting.github.io/npgsqlrest/options/#endpointscreated) as `ResponseHeaders` on a specific endpoint.
- Or, as [comment annotation](https://vb-consulting.github.io/npgsqlrest/annotations/#headers) on a PostgreSQL routine.

Now, you can set header value from a routine parameter. For, example, if the header value has a name in curly brackets like this `Content-Type: {_type}`, then a `Content-Type` header will have to value of the `_type` routine parameter (if the parameter exists).

See CSV example:

### CSV Example:

- Routine:

```sql
create function header_template_response1(_type text, _file text) 
returns table(n numeric, d timestamp, b boolean, t text)
language sql
as 
$$
select sub.* 
from (
values 
    (123, '2024-01-01'::timestamp, true, 'some text'),
    (456, '2024-12-31'::timestamp, false, 'another text')
)
sub (n, d, b, t)
$$;

comment on function header_template_response1(text, text) is '
raw
separator ,
newline \n
Content-Type: {_type}
Content-Disposition: attachment; filename={_file}
';
```

Request on this enpoint with parameters:
- _type = 'text/csv'
- _file = 'test.csv'

Will produce a download response to a 'test.csv' file with content type 'test.csv'. Raw values separator will be `,` character and row values separator will be new line.

---

## Version [2.9.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.9.0) (2024-08-02)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.5...2.9.0)

Added `raw` endpoint options and comment annotation:

Sets response to a "raw" mode. HTTP response is written exactly as it is received from PostgreSQL (raw mode).

This is useful for creating CSV responses automatically. For example:

```sql
create function raw_csv_response1() 
returns setof text
language sql
as 
$$
select trim(both '()' FROM sub::text) || E'\n' from (
values 
    (123, '2024-01-01'::timestamp, true, 'some text'),
    (456, '2024-12-31'::timestamp, false, 'another text')
)
sub (n, d, b, t)
$$;
comment on function raw_csv_response1() is '
raw
Content-Type: text/csv
';
```

Produces the following response:

```
HTTP/1.1 200 OK                                                  
Connection: close
Content-Type: text/csv
Date: Tue, 08 Aug 2024 14:25:26 GMT
Server: Kestrel
Transfer-Encoding: chunked

123,"2024-01-01 00:00:00",t,"some text"
456,"2024-12-31 00:00:00",f,"another text"

```

---

## Version [2.8.5](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.5) (2024-06-25)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.4...2.8.5)

Fix default parameter validation callback.

---

## Version [2.8.4](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.4) (2024-06-22)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.3...2.8.4)

Slight improvements in logging and enabling it to work with the client application.

---

## Version [2.8.3](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.3) (2024-06-11)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.2...2.8.3)

Fix inconsistency with sending request parameters by routine parameters.

Previously it was possible to send request parameters to parameters without default values. To use request parameters in routine parameters, that parameter has to have a default value always.

This inconsistency is actually a bug in cases when the request header parameter name wasn't provided a value.

This is fixed now.

TsClient 1.8.1:

- If all routines are skipped, don't write any files.

---

## Version [2.8.2](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.2) (2024-06-09)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.1...2.8.2)

### Fixed bug with default parameters

Using a routine that has default parameters and supplying one of the parameters would sometimes cause mixing of the parameter order. For example, if the routine is defined like this:

```sql
create function get_two_default_params(
    _p1 text = 'abc', 
    _p2 text = 'xyz'
) 
returns text 
language sql
as 
$$
select _p1 || _p2;
$$;
```

Invoking it with only the second parameter parameter (p2) would mix p1 and p2 and wrongly assume that the first parameter is the second parameter and vice versa.

This is now fixed and the parameters are correctly assigned.

### Fixed bug with default parameters for PostgreSQL roles that are not super-users.

When using a PostgreSQL role that is not a super-user, the default parameters were not correctly assigned.

This may be a bug in PostgreSQL itself (reported), column `parameter_default` in system table `information_schema.parameters` always returns `null` for roles that are not super-users.

Workaround is implemented and tested to use the `pg_get_function_arguments` function and then to parse the default values from the function definition.

---

## Version [2.8.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.1) (2024-05-10)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.0...2.8.1)

- Upgrade Npgsql from 8.0.0 to 8.0.3
- Fix null dereference of a possibly null build warning.

---

## Version [2.8.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.0) (2024-05-02)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.7.1...2.8.0)

- New Option: `Dictionary<string, StringValues> CustomRequestHeaders`

Custom request headers dictionary that will be added to NpgsqlRest requests. 

Note: these values are added to the request headers dictionary before they are sent as a context or parameter to the PostgreSQL routine and as such not visible to the browser debugger.

### TsClient Version 1.7.0

- Sanitaze generated TypeScript names.
- Add `SkipRoutineNames`, `SkipFunctionNames` and `SkipPaths` options.

---

## Version [2.7.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.7.1) (2024-04-30)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.7.0...2.7.1)

- Small fix on the Login endpoint that fixed the problem with the custom message not being written to the response in some rare circumstances.
- Redesigned the auth module and changed the access modifiers to the public of the ClaimTypes Dictionary to be used with the client application.

---

## Version [2.7.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.7.0) (2024-04-17)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.6.1...2.7.0)

New callback option: `Action<NpgsqlConnection, Routine, RoutineEndpoint, HttpContext>? BeforeConnectionOpen`.

This is used to set the application name parameter (for example) without having to use the service provider. It executes before the new connection is open for the request. For example:

```csharp
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    BeforeConnectionOpen = (NpgsqlConnection connection, Routine routine, RoutineEndpoint endpoint, HttpContext context) =>
    {
        var username = context.User.Identity?.Name;
        connection.ConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = string.Concat(
                    "{\"app\":\"",
                    builder.Environment.ApplicationName,
                    username is null ? "\",\"user\":null}" : string.Concat("\",\"user\":\"", username, "\"}"))
        }.ConnectionString;
    }
}
```

---

## Version [2.6.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.6.1) (2024-04-16)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.6.0...2.6.1)

Don't write the response body on status 205, which is forbidden.

## Version [2.6.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.6.0) (2024-04-16)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.5.0...2.6.0)

Improved error handling. Two new options are available:

### `ReturnNpgsqlExceptionMessage`:

- Set to true to return message property on exception from the `NpgsqlException` object on response body. The default is true. 

- Set to false to return empty body on exception.

### `PostgreSqlErrorCodeToHttpStatusCodeMapping`

Dictionary setting that maps the PostgreSQL Error Codes (see the [errcodes-appendix](https://www.postgresql.org/docs/current/errcodes-appendix.html) to HTTP Status Codes. 

Default is `{ "57014", 205 }` which maps PostgreSQL `query_canceled` error to HTTP `205 Reset Content`. If the mapping doesn't exist, the standard HTTP  `500 Internal Server Error` is returned.

---

## Version [2.5.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.5.0) (2024-04-15)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.4.2...2.5.0)

- New endpoint parameter option `BufferRows`.

Now it's possible to set the number of buffered rows that are returned before they are written to HTTP response on an endpoint level.

It is also possible to set this endpoint parameter as a comment annotation:

```sql
comment on function my_streaming_function() is 'HTTP GET
bufferrows 1';
```

See the full comment [annotation list here](https://github.com/vb-consulting/NpgsqlRest/blob/master/annotations.md).

Setting this parameter to 1 is useful in the HTTP streaming scenarios.

- New TsClient plugin options and fixes

```csharp
/// <summary>
/// Module name to import "baseUrl" constant, instead of defining it in a module.
/// </summary>
public string? ImportBaseUrlFrom { get; set; } = importBaseUrlFrom;

/// <summary>
/// Module name to import "pasreQuery" function, instead of defining it in a module.
/// </summary>
public string? ImportParseQueryFrom { get; set; } = importParseQueryFrom;
```

---

## Version [2.4.2](https://github.com/vb-consulting/NpgsqlRest/tree/2.4.2) (2024-04-14)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.4.1...2.4.2)

- Fix double logging the same message on the connection notice.

---

## Version [2.4.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.4.1) (2024-04-12)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.4.0...2.4.1)

- Fix missing Text type for types in need of JSON escaping.

---

## Version [2.4.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.4.0) (2024-04-08)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.3.1...2.4.0)

- Remove `AotTemplate` subproject directory. AOT Template is now `NpgsqlRestTestWebApi` with full configuration for the entire application.
- The auth handler doesn't complete the response if it doesn't have to on login and logout.
- Changed wording `Schema` to `Scheme` everywhere because that appears to be standard with the auth (unlike with databases).
- Changed `IRoutineSource` interface to:
  -  Have `CommentsMode` mode fully exposed with getters and setters.
  -  Have `Query` property exposed.
  -  Every parameter for the query exposed (`SchemaSimilarTo`, `SchemaNotSimilarTo`, etc).
- Replaced interfaces with concrete implementation for better performance.
- Fixed bug with service scope disposal when using `ConnectionFromServiceProvider`.
- Obfuscated auth parameters in logs (with `ObfuscateAuthParameterLogValues` option).
- Implemented `SerializeAuthEndpointsResponse` to serialize the auth (log in or log out) when it's possible.
- Fixed `ParameterValidationValues` to use `NpgsqlRestParameter` instead of `NpgsqlParameter` to expose the actual parameter name.
- Added `IsAuth` read-only property to the endpoint.
- Fixed automatic port detection in the code-gen plugins.
- Added `CommentHeaderIncludeComments` to the `TsClient` plugin.

---

## Version [2.3.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.3.1) (2024-04-05)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.3.0...2.3.1)

* Fix the "Headers are read-only, response has already started." error during the logout execution.

---

## Version [2.3.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.3.0) (2024-04-04)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.2.0...2.3.0)

- Login Endpoints can return text messages.
- A new option that supports this feature: `AuthenticationOptions.MessageColumnName`.
- Login endpoints always return text.
- Interface `IRoutineSource` exposes `string Query { get; set; }`. If the value doesn't contain blanks it is interpreted as the function name.
- TsClient plugin new version  (1.2.0):
  - New TsClweint option BySchema. If true, create a file by schema. The default is false.
  - Fix handling login endpoints.
  - Bugfixes.

---

## Version [2.2.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.2.0) (2024-04-02)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.1.0...2.2.0)

- Login endpoints
- Logout endpoints
- Small name refactoring (ReturnRecordNames -> ColumnNames)

To enable authentication, the authentication service first needs to be enabled in the application:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication().AddCookie();
```

The login-enabled endpoints must return a named record.

The program will result in the `ArgumentException` exception if the login-enabled routine is either of these:
- void
- returns simple value
- returns set of record unnamed records

The login operation will be interpreted as an unsuccessful login attempt and return the status code `401 Unauthorized` without creating a user identity if either:
- The routine returns an empty record.
- The returned record includes a [status column](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname), and the value of the status column name is:
  - False for boolean types.
  - Not 200 for numeric types.

The login operation will be interpreted as a successful login attempt and return the status code `200 OK` with creating a new user identity if either:
- The routine returns a record without status [status column](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname).
- The returned record includes a [status column](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname), and the value of the status column name is:
  - True for boolean types.
  - 200 for numeric types.

To authorize a different authorization scheme, return a [schema column name](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname) with the value of that schema.

Any other records will be set as new claims for the created user identity on successful login, where:
- The column name is claim type. This type will by default try to match the constant name in the [ClaimTypes class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0) to retrieve the value. Use the [`UseActiveDirectoryFederationServicesClaimTypes` Option][https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseactivedirectoryfederationservicesclaimtypes] to control this behavior.
- The record value (as string) is the claim value.

---

## Version [2.1.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.1.0) (2024-03-29)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.0.0...2.1.0)

### Role-Based Security

This version supports the **Roles-Based Security** mechanism.

The Endpoint can be authorized for only certain roles. 

For example, all endpoints must be in `admin` or `superadmin` roles:

```csharp
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreated = (routine, endpoint) => endpoint with { AuthorizeRoles = ["admin", "super"] }
});
```

Same thing, only for the function with name `protected_func`:

```csharp
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreated = (routine, endpoint) => routine.Name == "protected_func" ? endpoint with { AuthorizeRoles = ["admin", "super"] } : endpoint
});
```

There is also support for the comment annotations. Add a list of roles after the `RequiresAuthorization` annotation tag:

```sql
comment on function protected_func() is 'authorize admin, superadmin';
```

See more details on the [`RequiresAuthorization` annotation tag](https://vb-consulting.github.io/npgsqlrest/annotations/#requiresauthorization).

Note: If the user is authorized but not in any of the roles required by the authorization, the endpoint will return the status code `403 Forbidden`.

### TsClient IncludeStatusCode

The New version of the `NpgsqlRest.TsClient` (1.1.0) plugin now includes the `IncludeStatusCode` option.

When set to true (default is false), the resulting value will include the response code in the function result, for example:

```ts
export async function getDuplicateEmailCustomers() : Promise<{status: number, response: IGetDuplicateEmailCustomersResponse[]}> {
    const response = await fetch(_baseUrl + "/api/get-duplicate-email-customers", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return {status: response.status, response: await response.json() as IGetDuplicateEmailCustomersResponse[]};
}
```

---

## Version [2.0.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.0.0) (2024-03-10)

Version 2.0.0 is the major redesign of the entire library. This version adds extendibility by introducing the **concept of plugins.**

There are two types of plugins:

### 1) Routine Source Plugins

The concept of a routine in NpgsqlRest refers to an action that is executed on the PostgreSQL server when an API endpoint is called. This can include PostgreSQL functions, procedures, custom queries, or commands.

In previous versions of the library, only PostgreSQL functions and procedures were considered routine sources. The REST API was built on the available functions and procedures in the PostgreSQL database and provided configuration.

However, in the latest version, the routine source has been abstracted, allowing for the addition of new routine sources as plugins. This provides extendibility and flexibility in building the REST API based on different sources of routines.

For example, a plugin that can build CRUD (create, read, update, delete) support for tables and views is published as an independent, standalone package: **[NpgsqlRest.CrudSource](https://vb-consulting.github.io/npgsqlrest/crudsource)**.

To add CRUD support to API generation, first, reference `NpgsqlRest.CrudSource` plugin library with:

```console
dotnet add package NpgsqlRest.CrudSource --version 1.0.0
```

And then include the `CrudSource` source into existing sources:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    SourcesCreated = sources => sources.Add(new CrudSource())
});

app.Run();
```

Note that the routine source for functions and procedures is already present in the basic library. It's part of the basic functionality and is not separated into a plugin package.

### 2) Code Generation Plugins

The second type is the code generator plugins, capable of generating a client code that can call those generated REST API endpoints.

In the previous version, there is support for the automatic generation of [HTTP files](https://learn.microsoft.com/en-us/aspnet/core/test/http-files?view=aspnetcore-8.0). This support is now moved to a separate plugin library **[NpgsqlRest.HttpFiles](https://vb-consulting.github.io/npgsqlrest/httpfiles/)**.

To use the HTTP files support, first, reference `NpgsqlRest.HttpFiles` plugin library with:

```console
dotnet add package NpgsqlRest.HttpFiles --version 1.0.0
```

And then include the `HttpFiles` in the list of generators in the `EndpointCreateHandlers` list option:

```csharp
using NpgsqlRest;
using NpgsqlRest.HttpFiles;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreateHandlers = [new HttpFile(), /* other gnerator plugins */],
});

app.Run();
```

There is also a client code generator for Typescript that can generate a Typescript module to call the generated API: **[NpgsqlRest.TsClient](https://vb-consulting.github.io/npgsqlrest/tsclient/)**


To include Typesscipt client:

```console
dotnet add package NpgsqlRest.TsClient --version 1.0.0
```

And add `TsClient` to the list:

```csharp
using NpgsqlRest;
using NpgsqlRest.TsClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreateHandlers = [new TsClient("../Frontend/src/api.ts")],
});

app.Run();
```

### System Design

System design can be illustrated with the following diagram:

<p style="text-align: center; width: 100%">
    <img src="/npgsqlrest-v2.png" style="width: 70%;"/>
</p>

The initial bootstrap with all available plugins looks like this:

```csharp
using NpgsqlRest;
using NpgsqlRest.CrudSource;
using NpgsqlRest.HttpFiles;
using NpgsqlRest.TsClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreateHandlers = [new HttpFile(), new TsClient("../Frontend/src/api.ts")],
    SourcesCreated = sources => sources.Add(new CrudSource())
});

app.Run();
```

### Other Changes

Other changes include:

- Optimizations
- Bugfixes for edge cases

Full list of available options and annotations for version 2:

- **[See Options Reference](https://vb-consulting.github.io/npgsqlrest/options/)**
- **[See Comment Annotations Reference](https://vb-consulting.github.io/npgsqlrest/annotations/)**

-----------

## Older Versions

The changelog for the previous version can be found here: [Changelog Version 1](https://github.com/vb-consulting/NpgsqlRest/blob/2.0.0/changelog-old.md)

-----------