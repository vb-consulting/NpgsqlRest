# Changelog

Note: For a changelog for a client application [see the client application page changelog](https://vb-consulting.github.io/npgsqlrest/client/#changelog).

---

DONE:

# TsClient fixes:

- Fixed TsClient plugin to handle booleans correctly.
- Added JsCode style comments for parameters and return values in TsClient plugin.
- Added upload support for TS and JS client.
- Added support for XsrfTokenHeaderName if used. This is used by the Uplaod endpoints.
- Smaller fixes in the TsClient plugin to handle some edge cases.

# Core NpgsqlRest Library fixes:

- Added PATH to comment annotation parser: Ability to set just HTTP path without method.
    - If HTTP tag has only two params and second param is not VERB, it is treated ass the path then
    - New comment annotation: PATH path

- Added comment annotation
    securitysensitive
    sensitive
    security
    security_sensitive
    security-sensitive
This will manually obfuscates all log parameters

- Ability to mark the parameter as hash, and value will be pre-hashed.
    - New comment annotation: 
        - "param param_name1 is hash of param_name2"
        - "param param_name is hash of param_name"

- Ability of the Login endpoints to compare and test the password hash.
    - Login endpoints can now return 'hash' (configurable) column and when they do, it will be verified against parameter that contains "pass" (configurable).
    - If verification fails, login will return 404 with no message.

- Add file upload support from settings and from comment annotations. 
    - New annotations:
    - "upload" - mark as upload
    - "upload param_name as metadata" - marks as upload and sets the parameter as metadata
    - "param param_name1 is upload metadata" - marks as upload and sets the parameter as metadata (same thing)
    - "upload for handler_name1, handler_name2 [, ...]" - marks as upload and sets the upload handler or multiple handlers
    - Currently implemented upload handlers are (by key):
        - "large_object" - upload to PostgreSQL large object storage. Metadata example: 
        ```jsonc
        {
            "type": "large_object",
            "fileName": "test.txt",
            "fileType": "text/plain",
            "size": 100,
            "oid": 1234
        }
        ```
    - "file_system" - upload to PostgreSQL large object storage. Metadata example: 
        ```jsonc
        {
            "type": "file_system",
            "fileName": "test.txt",
            "fileType": "text/plain",
            "size": 100,
            "filePath": "/tmp/uploads/ADF3B177-D0A5-4AA0-8805-FB63F8504ED8.txt"
        }
        ```
    - For multiple handlers metadata parameter should be json array, otherwise for a single handler it is text or json.

- Fixed issue with endpoint with default parameters, when they receive not existing parameters in same number as default parameters. Endpoint now returns 404 instead of 200 error as it should be.
- Fixed serialization of binary data in the response. From now on, endpoints that return either:
    - single value of type bytea (binary)
    - single column of type setof bytea (binary)
    - will be written raw directly to response. This allows for example, displaying images directly from the database.

# NpgsqlRest Client App fixes:

- External login was fundamentally broken, now it is fixed.
- External login function is called with the following parameters:
  - external login provider (if param exists)
  - external login email (if param exists)
  - external login name (if param exists)
  - external login json data received (if param exists)
  - client browser analytics json data (if param exists)
- To accommodate client browser analytics parameter support, two new config keys were added:
    - ClientAnaliticsData - javascript object definition
    - ClientAnaliticsIpKey - key name for the IP address that is added to the analytics data
- Added default configurations for Microsoft and Facebook too

- Add caching static files to the middleware.
    - New key: StaticFiles -> ParseContentOptions -> CacheParsedFile - caches parsed content, default true

- Add custom message on client started listeting...
    - Added StartupMessage key to the configuration:
  //
  // Logs at startup, placeholders:
  // {0} - startup time
  // {1} - listening on urls
  // {2} - current version
  //
  "StartupMessage": "Started in {0}, listening on {1}, version {2}",

- Custom logging context name instead of "NpgsqlRest":
    - ApplicationName config key is now doing this purpose.


- Add support for configuration of the antiforgery token endpoint in the client app.
    - New configuration section: Antiforgery
  "Antiforgery": {
    "Enabled": false,
    "CookieName": null,
    "FormFieldName": "__RequestVerificationToken",
    "HeaderName": "RequestVerificationToken",
    "SuppressReadingTokenFromFormBody": false,
    "SuppressXFrameOptionsHeader": false
  },
  - New ParseContentOptions options for StaticFiles:
      //
      // Name of the configured Antiforgery form field name to be used in the static files (see Antiforgery FormFieldName setting).
      //
      "AntiforgeryFieldName": "antiForgeryFieldName",
      //
      // Value of the Antiforgery token if Antiforgery is enabled..
      //
      "AntiforgeryToken": "antiForgeryToken"
  - Add new NpgsqlRestClient config section to NpgsqlRest:
    //
    // Upload handlers options
    //
    "UploadHandlers": {
      //
      // Handler that will be used when upload handler or handlers are not specified.
      //
      "DefaultUploadHandler": "large_object",
      //
      // Enables upload handlers for the NpgsqlRest endpoints that uses PostgreSQL Large Objects API
      // Metadata example: {"type": "large_object", "fileName": "file.txt", "contentType": "text/plain", "size": 1234567890, "oid": 1234}
      //
      "LargeObjectEnabled": true,
      "LargeObjectKey": "large_object",
      "LargeObjectHandlerBufferSize": 8192,

      //
      // Enables upload handlers for the NpgsqlRest endpoints that uses file system
      // Metadata example: {"type": "file_system", "fileName": "file.txt", "contentType": "text/plain", "size": 1234567890, "filePath": "/tmp/uploads/CB0B16D6-FF10-4A39-94A4-C7017C09D869.txt"}
      //
      "FileSystemEnabled": true,
      "FileSystemKey": "file_system",
      "FileSystemHandlerPath": "/tmp/uploads",
      "FileSystemHandlerUseUniqueFileName": true,
      "FileSystemHandlerCreatePathIfNotExists": true,
      "FileSystemHandlerBufferSize": 8192
    }


## Version [2.22.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.22.0 (2025-04-07)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.22.0...2.21.0)

## Improved Logging

Improved Logging for Endpoint creation to include routine and endpoint:

```console
[14:56:17.477 INF] Function auth.login mapped to POST /api/auth/login has set ALLOW ANONYMOUS by the comment annotation. [NpgsqlRest]
```

## Fixed CRUD Plugin

CRUD Endpoints are finally getting some love: 
- Fixed issue with connection as data source.
- Added new tags: `onconflict`, `on_conflict` and `on-conflict` to generated endpoints handling with on conflict resoluions.

Now it's possible to enable or disable those endpoints explicitly with comment annotations:

```sql
create table test.crud_table (
    id bigint generated always as identity primary key,
    name text not null,
    description text,
    created_at timestamp default now()
);

comment on table test.crud_table is '
HTTP
for on-conflict 
disabled
';
```

## Version [2.21.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.21.0 (2025-03-24)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.21.0...2.20.0)

### Support For Multiple Connections

In this version, it is possible to set multiple connections by using the `ConnectionStrings` dictionary option and setting the correct `ConnectionName` on the routine endpoint.

- New option:

```csharp
/// <summary>
/// Dictionary of connection strings. The key is the connection name and the value is the connection string.
/// This option is used when the RoutineEndpoint has a connection name defined.
/// This allows the middleware to use different connection strings for different routines.
/// For example, some routines might use the primary database connection string, while others might use a read-only connection string from the replica servers.
/// </summary>
public IDictionary<string, string>? ConnectionStrings { get; set; } = connectionStrings;
```

- New property on the routine endpoint:

```csharp
public string? ConnectionName { get; set; } = connectionName;
```

The default value for all these properties is null.

Set the ConnectionStrings dictionary to alternate connection and then set ConnectionName for a specific connection key name. If the key doesn't exist, the endpoint will return 500 (Interval Server Error).

This feature was added to add support for configuring certain routines to be executed on read-only replicas.


## Version [2.20.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.20.0 (2025-03-05)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.20.0...2.19.0)

Many changes:

### 1) Breaking changes in interfaces

There are some small breaking changes in public interfaces. These are just simplifications: 

From this version, `RoutineEndpoint` has a `Routine` read-only property. This allows simplification where we don't have to have these two parameters: `RoutineEndpoint` and `Routine`. 

We can only have one `RoutineEndpoint`. Every interface and structure that had these two parameters, fields, or properties now only has one: `RoutineEndpoint.` 

### 2) Logging improvements

- New option:

```cs
/// <summary>
/// MessageOnly - Log only connection messages.
/// FirstStackFrameAndMessage - Log first stack frame and the message.
/// FullStackAndMessage - Log full stack trace and message.
/// </summary>
public PostgresConnectionNoticeLoggingMode LogConnectionNoticeEventsMode { get; set; } = PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage;
```

The default is the `FirstStackFrameAndMessage` that logs only the first stack frame and the message. In chained calls, the stack frame can be longer and obfuscate the log message. This option will show only the first (starting) stack frame along with the message.

- The log pattern was also slightly changed. The last two options now look like this: `" {where}:\n{message}" `.

- Fix: Fixed parameter ordinal number when logging command parameters (`LogCommands` and `LogCommandParameters` options are true).

### 3) Caching Improvements

In the previous version, some basic response caching in sever memory was introduced (when the routine is marked as cached). This feature is now massively improved:

- Injectible Cache Object

Cache Object can be injected in options by using the new `DefaultRoutineCache`

```cs
/// <summary>
/// Default routine cache object. Inject custom cache object to override default cache.
/// </summary>
public IRoutineCache DefaultRoutineCache { get; set; } = defaultRoutineCache ?? new RoutineCache();
```

Interface `IRoutineCache` can implement any caching strategy:

```cs
public interface IRoutineCache
{
    bool Get(RoutineEndpoint endpoint, string key, out string? result);
    void AddOrUpdate(RoutineEndpoint endpoint, string key, string? value);
}
```

There is a new default cache implementation that supports a cache expiration time span.

- Cache Expiration

Cache expiration can be defined on a `RoutineEndpoint` property, together with other caching properties

```cs
public bool Cached { get; set; } = cached;
public HashSet<string>? CachedParams { get; set; } = cachedParams?.ToHashSet();
public TimeSpan? CacheExpiresIn { get; set; } = cacheExpiresIn;
```

It can be also set as routine comment annotation by using any of these annotation tags:

```
cacheexpires [ time_span_value ]
cacheexpiresin [ time_span_value ]
cache-expires [ time_span_value ]
cache-expires-in [ time_span_value ]
cache_expires [ time_span_value ]
cache_expires_in [ time_span_value ]
```

Value is a simplified PostgreSQL interval value, for example, `10s` or `10sec` for 10 seconds, `5d` is for 5 days, and so on. Space between is also allowed. For example, `3h`, `3 hours`, `3 h`, and `3 hours` are the same.

Valid abbreviations are:

| abbr | meaning |
| ---- | ------------------------------- |
| `s`, `sec`, `second` or `seconds` | value is expressed in seconds |
| `m', `min`, `minute` or `minutes` | value is expressed in minutes |
| `h`, `hour`, `hours` | value is expressed in hours |
| `d`, `day`, `days` | value is expressed in days |

- Other Cache Improvements

There are some other improvements, such as avoiding unnecessary allocations when returning from the cache, creating a connection, and so on.

There is also a new option to set cache prune interval:

```cs
/// <summary>
/// When cache is enabled, this value sets the interval in minutes for cache pruning (removing expired entires). Default is 1 minute.
/// </summary>
public int CachePruneIntervalMin { get; set; } = 1;
```

This is the interval in minutes that, if the cache is used, will remove expired items in this interval.

### 4) Responses Parser

From this version, it is possible to inject a custom response parser mechanism by using a new option:

```cs
/// <summary>
/// Default response parser object. Inject custom response parser object to add default response parser.
/// </summary>
public IResponseParser? DefaultResponseParser { get; set; } = null;
```

You can set a custom implementation to the `DefaultResponseParser` option that implements the `IResponseParser` interface:

```cs
public interface IResponseParser
{
    /// <summary>
    /// Parse response from PostgreSQL.
    /// </summary>
    /// <returns>Response string</returns>
    ReadOnlySpan<char> Parse(ReadOnlySpan<char> input, RoutineEndpoint endpoint, HttpContext context);
}
```

This is useful when response content needs to be enriched from the Http context, such as user claims, IP address, etc.

In order to call this parser, `RoutineEndpoint` needs to have `ParseResponse` set to true (default is false).

This also can be set by using new comment annotations:

```
parse
parseresponse
parse_response
parse-response
```

When `ParseResponse` is set to true on the endpoint, and DefaultResponseParser has been set to nonnull instance, the response will be parsed and returned from the `Parse` method always, even when it is cached.

## Version [2.19.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.19.0 (2025-02-24)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.19.0...2.18.0)

New routine annotation and new enpoint options:

```console
                                                
cached                                          
cached [ param1, param2, param3 [, ...] ]       
cached [ param1 param2 param3 [...] ]           
                                                
```

If the routine returns a single value of any type, result will be cached in memory and retrieved from memory on next call. Use the optional list of parameter names (original or converted) to be used as additional cache keys.

Same will can be set programmatically directly on the endpoint settings:

```csharp
    public bool Cached { get; set; } = false;
    public HashSet<string>? CachedParams { get; set; } = null;
```

If the associated routine doesn't return a single value of any type, there will be a warning on startup and cache will be ignored.

Results from cache will have `[from cache]` tag in execution log.

## Version [2.18.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.18.0 (2025-02-23)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.18.0...2.17.0)

Improve `ValidateParameters` and `ValidateParametersAsync` callbacks:

From this version they will be triggered even for paramaters that exits with default value but they are not supplied. 

In this case, parameter value will be `null` and if we want default value, we can leave it as `null`. Otherwise it is possible to assign a new value.

## Version [2.17.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.17.0 (2024-01-09)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.16.1...2.17.0)

Fix refresh path metadata logic. Refreshing metdata now also runs all plugins for source generation.

## Version [2.16.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.16.1 (2024-01-06)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.16.0...2.16.1)

Fixed rare issue with path resolve logic.

## Version [2.16.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.16.0 (2024-12-30)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.15.0...2.16.0)

Internal optimizations:
- Using sequential access by default for all reader operations.
- Optimized parameter parser to use only single loop.

Added `string[][]? CommentWordLines` get property to `RoutineEndpoint` data structure. This representes parsed words from a routine comment which will make easier to parse comments in plugins.

## Version [2.15.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.15.0 (2024-12-21)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.14.0...2.15.0)

This version if full rewrite of the library bringing code simplification and many perfomance optimizations.

If has one new feature that allows refreshing medata without program restart. To facilitate this functionality, there are three new options:

### RefreshEndpointEnabled

- Type: `bool`
- Default: `false`

Enables or disables refresh endpoint. When refresh endpoint is invoked, the entire metadata for NpgsqlRest endpoints is refreshed. When metadata is refreshed, endpoint returns status 200.

### RefreshMethod

- Type: `string`
- Default: `"GET"`

### RefreshPath

- Type: `string`
- Default: `"/api/npgsqlrest/refresh"`

## Version [2.14.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.14.0 (2024-11-25)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.13.1...2.14.0)

Improved connection handling:

- Added new option: `public NpgsqlDataSource? DataSource { get; set; }`

When `DataSource` is set, `ConnectectionString` is ignored. `DataSource` is then used to create new connections for each request by calling `CreateConnection()`. This should be much faster then creating a new connection with `new NpgsqlConnection(connectionString)`.

- Added new option: `public ServiceProviderObject ServiceProviderMode { get; set; } `

`ServiceProviderObject` is enum that defines how handle service provider objects when fetching connection from service provdiders:

```csharp
public enum ServiceProviderObject
{
    /// <summary>
    /// Connection is not provided in service provider. Connection is supplied either by ConnectionString or by DataSource option.
    /// </summary>
    None,
    /// <summary>
    /// NpgsqlRest attempts to get NpgsqlDataSource from service provider (assuming one is provided).
    /// </summary>
    NpgsqlDataSource,
    /// <summary>
    /// NpgsqlRest attempts to get NpgsqlConnection from service provider (assuming one is provided).
    /// </summary>
    NpgsqlConnection
}
```

Default is `ServiceProviderObject.None`.

- Removed `public bool ConnectionFromServiceProvider { get; set; }`. Replaced by `ServiceProviderMode`.


## Version [2.13.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.13.1 (2024-11-23)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.13.0...2.13.1)

Upgrade Npgsql to 9.0.1

## Version [2.12.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.13.0 (2024-11-17)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.12.1...2.13.0)

- Upgrade all projects and libraries to .NET 9
- Improve the performance of the middlweware by using:
  - GetAlternateLookup extensions method to get the value from the dictionary.
  - System.IO.Pipelines.PipeWriter instead of writing directly to the response stream.
  - Spans where possible.
  
Performance benchmarks show up to 50% improvement in the middleware performance. 

The [benchmark projects](https://github.com/vb-consulting/pg_function_load_tests) is being built and will be available soon. 

## Version [2.12.1](https://github.com/vb-consulting/NpgsqlRest/tree/2.12.1 (2024-11-06)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.12.0...2.12.1)

Reverted changes in reader logic (Reader Optimizations and Provider-Specific Values). Detailed perfomance load tests and examining the Npgsql sozrce does not justify these changes.

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