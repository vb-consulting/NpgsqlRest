# Changelog

Note: For a changelog for a client application [see the client application page changelog](https://vb-consulting.github.io/npgsqlrest/client/#changelog).

---

## Version [2.23.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.23.0 (2025-04-26)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.23.0...2.22.0)

### TsClient Plugin Fixes

- Fixed TsClient plugin to handle booleans correctly.
- Added JsCode style comments for parameters and return values in TsClient plugin.
- Added upload support for TS and JS client.
- Added support for XsrfTokenHeaderName if used. This is used by the Uplaod endpoints.
- Smaller fixes in the TsClient plugin to handle some edge cases.

### Core NpgsqlRest Library fixes

#### Added PATH to Comment Annotation Parser 

- New comment annotation: `PATH path`.
- Ability to set just HTTP path without method by using `PATH /my-path/`
- If HTTP tag has only two params and second param is not valid VERB, it is treated as the path.

#### Added SecuritySensitive Routine Option

- New Endpoint Option and Comment Annotation:
    `securitysensitive`
    `sensitive`
    `security`
    `security_sensitive`
    `security-sensitive`

- This will manually obfuscates all parameter values before sending them to log.

#### Hashing Capabilities

- This is completely new feature. There is a default hasher class that can be injected into system with the new option called `PasswordHasher`.

```csharp
/// <summary>
/// Default password hasher object. Inject custom password hasher object to add default password hasher.
/// </summary>
public IPasswordHasher PasswordHasher { get; set; } = new PasswordHasher();
```

- Default implementation in `PasswordHasher` class is using PBKDF2 (Password-Based Key Derivation Function 2) with SHA-256 and it incorporates 128-bit salt with 600,000 iterations (OWASP-recommended as of 2025). So it's secure, but there is option to inject a different one.

##### Mark Parameter as Hash

- Ability to mark the parameter as hash and the value will be pre-hashed with the default hasher.

- New comment annotations: 
    - `param param_name1 is hash of param_name2` - the first pararameter `param_name1` will have the hashed value of the first `param_name2`.
    - `param param_name is hash of param_name` - this pararameter `param_name` will have the original value hashed.
    - Typical usage: `param _hashed_password is hash of _password` or just `param _password is hash of _password`.

- This can be set programatically also: 

```csharp
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreated = endpoint => endpoint with { AuthorizeRoles = ["admin", "super"] }
});
```

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


-----------

## Older Versions

The changelog for the previous version can be found here: [Changelog Archive](https://github.com/vb-consulting/NpgsqlRest/blob/2.0.0/changelog-old.md)

-----------