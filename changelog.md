# Changelog

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