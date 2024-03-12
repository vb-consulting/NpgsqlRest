# Changelog

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
dotnet add package NpgsqlRest.CrudSource --version 1.0.0
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

Changes in this major version are too numerous. See the documentation pages instead:

- **[See Options Reference](https://vb-consulting.github.io/npgsqlrest/options/)**
- **[See Comment Annotations Reference](https://vb-consulting.github.io/npgsqlrest/annotations/)**

-----------
