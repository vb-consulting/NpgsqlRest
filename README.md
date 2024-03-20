# NpgsqlRest

![build-test-publish](https://github.com/vb-consulting/NpgsqlRest/workflows/build-test-publish/badge.svg)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

**Automatic REST API** for PostgreSQL Databases implemented as **AOT-Ready .NET8 Middleware**

>
> If you have a PostgreSQL database - based on your configuration, NpgsqlRest can create **blazing fast REST API automatically** and **write client code** for your project.
>

Read the **[NpgsqlRest Version 2 Blog Post](https://vb-consulting.github.io/blog/npgsqlrest-v2/).**

See the changelog for the latest release changes in the **[Changelog](https://vb-consulting.github.io/npgsqlrest/changelog/).**

#### High Performance

[See More](#high-performances)

<p style="text-align: center; width: 100%">
    <img src="/npgsqlrest-chart.png?v3" style="width: 50%;"/>
</p>

#### Modular Design 

[See More](#plug-in-system)

<p style="text-align: center; width: 100%">
    <img src="/npgsqlrest-v2.png" style="width: 70%;"/>
</p>

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

## Features

- Automatic **generation of the HTTP REST endpoints** from PostgreSQL functions and procedures.
- **Native AOT-Ready**. AOT is ahead-of-time compiled to the native code. No dependencies, native executable, it just runs and it's very fast.
- **Customization** of endpoints with comment annotations. You can easily configure any endpoint by adding comment annotation labels to [PostgreSQL Comments](https://www.postgresql.org/docs/current/sql-comment.html). 
- Interact seamlessly with **.NET8 backend** and take advantage of .NET8 features.
- **High performance** with or without native AOT, up to 6 times higher throughput than similar solutions.
- **Plug-in system** with additional functionalities: table CRUD support, code generation for HTTP Files and Typescript client and more.

### Automatic Generation of REST Endpoints

See the introductory example above. Automatically build HTTP REST endpoints from PostgreSQL functions and procedures and configure them the way you like.

### Native AOT-Ready

With the NET8 you can build into native code code (ahead-of-time (AOT) compilation). 

NpgsqlRest is fully native AOT-ready and AOT-tested.

AOT builds have faster startup time, smaller memory footprints and don't require any .NET runtime installed.

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

### NET8 backend

NpgsqlRest is implemented as a NET8 middleware component, which means that anything that is available in NET8 is also available to the NpgsqlRest REST endpoints. 

And that is, well, everything... from rate limiters to all kinds of authorization schemas, to name a few.

You can also interact with the NET8 calling code to: 

- Provide custom parameter validations.
- Pass custom values to function/procedure parameters.

For example, pass a `Context.User.Identity.Name` to every parameter named `user`:

```csharp
var app = builder.Build();                                       
app.UseNpgsqlRest(new(connectionString)
{
    ValidateParameters = p =>
    {
        if (p.Context.User.Identity?.IsAuthenticated == true && 
            string.Equals(p.ParamName, "user", StringComparison.OrdinalIgnoreCase))
        {
            p.Parameter.Value = p.Context.User.Identity.Name;
        }
    } 
});
app.Run();
```

### High Performances

NpgsqlRest has an extremely high throughput:

| Platform | 10 Records | 100 Records |
| -------- | ---------: | ----------: |
| **AOT** is an ahead-of-time native compilation of `NpgsqlRest`. `NpgsqlRest` compiled to the native binary. | **423,515** | **100,542** |
| **JIT** is a just-in-time compilation of `NpgsqlRest` to NET8 CLR (Common Language Runtime) on NET8 runtime. | **606,410**  | **126,154** |
| **ADO** is NET8 Raw ADO Data Reader approach. [Source](https://vb-consulting.github.io/npgsqlrest/performances/#4-ado) | 440,896 | 314,198 |
| **EF** is Entity Framework Core 8 on NET8 runtime. [Source](/https://vb-consulting.github.ionpgsqlrest/performances/#3-ef) | 337,612 | 235,331 |
| **PostgREST** version 12.0.2 | 72,305 | 40,456 |
| **Django** REST Framework 4.2.10 on Python 3.8 [Source Link](https://vb-consulting.github.io/npgsqlrest/performances/#5-django) | 21,193 | 18,345  |
| **Express** on NodeJS v20.11.1, express v4.18.3, pg 8.11.3 [Source Link](https://vb-consulting.github.io/npgsqlrest/performances/#6-express) | 160,241 | 55,119 |
| **GO** version go1.13.8 [Source Link](https://vb-consulting.github.io/npgsqlrest/performances/#7-go) | 78,530 | 55,119 |
| **FastAPI** version 0.110.0 on Python 3.8 [Source Link](https://vb-consulting.github.io/npgsqlrest/performances/#8-fastapi)| 13,650 | 9,666 |

See more details [here](https://github.com/vb-consulting/NpgsqlRest/tree/master/PerfomanceTests).

### Plug-in System

NpgsqlRest has a plug-in system that allows you to extend the functionality of the generated REST API from your PostgreSQL database. 

Currently, the following plug-ins are available:

- **[CRUD support](https://vb-consulting.github.io/npgsqlrest/crudsource)**. Automatically generate CRUD endpoints for your PostgreSQL tables and views.
- **[HTTP File support](https://vb-consulting.github.io/npgsqlrest/httpfiles/)**. Automatically generate HTTP files for testing, with the list of available endpoints.
- **[Typescript Client support](https://vb-consulting.github.io/npgsqlrest/tsclient/)**. Automatically generate Typescript client code from the NpgsqlRest endpoints for your Typescript projects.

## Getting Started

### Using Library

#### Library Installation

Install the package from NuGet by using any of the available methods:

```console
dotnet add package NpgsqlRest --version 2.0.0
```
```console
NuGet\Install-Package NpgsqlRest -version 2.0.0
```
```xml
<PackageReference Include="NpgsqlRest" Version="2.0.0" />
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

- net8.0
- Microsoft.NET.Sdk.Web 8.0
- Npgsql 8.0.1
- PostgreSQL >= 13

Note: PostgreSQL 13 minimal version is required to run the initial query to get the list of functions. The source code of this query can be found [here](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/RoutineQuery.cs). For versions prior to version 13, this query can be replaced with a custom query that can run on older versions.

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
