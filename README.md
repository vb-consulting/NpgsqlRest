# NpgsqlRest

[![Build, Test, Publish and Release](https://github.com/vb-consulting/NpgsqlRest/actions/workflows/build-test-publish.yml/badge.svg)](https://github.com/vb-consulting/NpgsqlRest/actions/workflows/build-test-publish.yml)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

**Automatic REST API** for PostgreSQL Databases implemented as **AOT-Ready .NET8 Middleware**

>
> If you have a PostgreSQL database - NpgsqlRest can create **blazing fast REST API automatically** and **write client code** for your project.
>

Features:

- Nuget package for .NET that creates discoverable REST API automatically from PostgreSQL database.
- **High Performance**. See [Performances Benchmarks](https://github.com/vb-consulting/pg_function_load_tests).
- **Modular Design** with a Plug-in System. Create API for functions and procedure, create CRUD endpoints for your tables, create HTTP files, and Typescript client code.
- **AOT-Ready**. Ahead-of-time compiled to the native code. No dependencies, native executable, it just runs and it's very fast.
- **Customizable**. Configure endpoints with comment annotations. You can easily configure any endpoint by adding comment annotation labels to [PostgreSQL Comments](https://www.postgresql.org/docs/current/sql-comment.html).
- **Standalone Executable Web Client.** Download the executable and run it. No installation required. See [Releases](https://github.com/vb-consulting/NpgsqlRest/releases). 

## Quick Example

#### 1) Your PostgreSQL Function

```sql
create function hello_world()                                    
returns text 
language sql
as $$
select 'Hello World'
$$;
```

#### 2) .NET8 AOT-Ready Web App

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();
var connectionStr = "Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres";
app.UseNpgsqlRest(new(connectionStr));
app.Run();
```

#### 3) Auto-Generated HTTP File

```console
@host=http://localhost:5000                                      

// function public.hello_world()
// returns text
POST {{host}}/api/hello-world/
```

#### 4) Auto-Generated Typescript Client Module

```ts
const _baseUrl = "http://localhost:5000";                        


/**
* function public.get_latest_customer()
* returns customers
* 
* @remarks
* GET /api/get-latest-customer
* 
* @see FUNCTION public.get_latest_customer
*/
export async function getHelloWorld() : Promise<string> {
    const response = await fetch(_baseUrl + "/api/hello-world", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    return await response.text() as string;
}
```

#### 5) Endpoint Response

```console
HTTP/1.1 200 OK                                                  
Connection: close
Content-Type: text/plain
Date: Tue, 09 Jan 2024 14:25:26 GMT
Server: Kestrel
Transfer-Encoding: chunked

Hello World
```

### Comment Annotations

Configure individual endpoints with powerful and simple routine comment annotations. You can use any PostgreSQL administration tool or a simple script:

Function:

```sql
create function hello_world_html()                               
language sql 
as 
$$
select '<div>Hello World</div>';
$$

comment on function hello_world_html() is '
HTTP GET /hello
Content-Type: text/html';
```

Will have content type `text/html` as visible in comment annotation:

```console
Connection: close                                                
Content-Type: text/html
Date: Thu, 18 Jan 2024 11:00:39 GMT
Server: Kestrel
Transfer-Encoding: chunked

<div>Hello World</div>
```

## Getting Started

### Using Library

#### Library Installation

Install the package from NuGet by using any of the available methods:

```console
dotnet add package NpgsqlRest --version 2.13.0
```
```console
NuGet\Install-Package NpgsqlRest -version 2.13.0
```
```xml
<PackageReference Include="NpgsqlRest" Version="2.13.0" />
```

#### Library First Use

Your application builder code:

```csharp
var app = builder.Build();
app.UseNpgsqlRest(new("Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"));
app.Run();
```

For all available build options, see the **[options documentation](https://vb-consulting.github.io/npgsqlrest/options/).**

#### Library Dependencies

- net9.0
- Microsoft.NET.Sdk.Web 9.0
- Npgsql 8.0.5
- PostgreSQL >= 13

Note: PostgreSQL 13 minimal version is required to run the initial query to get the list of functions. The source code of this query can be found [here](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/RoutineQuery.cs). For versions prior to version 13, this query can be replaced with a custom query that can run on older versions.

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
