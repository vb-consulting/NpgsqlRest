# Changelog

Note: For a changelog for a client application [see the client application page changelog](https://vb-consulting.github.io/npgsqlrest/client/#changelog).

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


## Version [2.8.5](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.5) (2024-06-25)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.4...2.8.5)

Fix default parameter validation callback.

## Version [2.8.4](https://github.com/vb-consulting/NpgsqlRest/tree/2.8.4) (2024-06-22)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.8.3...2.8.4)

Slight improvements in logging and enabling it to work with the client application.

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

```cs
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

```cs
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

```cs
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