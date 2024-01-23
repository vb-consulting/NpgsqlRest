# NpgsqlRest

![build-test-publish](https://github.com/vb-consulting/NpgsqlRest/workflows/build-test-publish/badge.svg)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

Automatic REST API for Any Postgres Database implemented as AOT Ready .NET8 Middleware

#### 1) Your PostgreSQL Function

```sql
create function hello_world()                                    
returns text 
language sql
as $$
select 'Hello World'
$$;
```

#### 2) .NET8 AOT Ready Web App

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();
var connectionStr = "Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres";
app.UseNpgsqlRest(new(connectionStr));
app.Run();
```

#### 3) Optionally, Auto-Generated HTTP File

```http
@host=http://localhost:5000                                      

// function public.hello_world()
// returns text
POST {{host}}/api/hello-world/
```

#### 4) Endpoint Response

```http
HTTP/1.1 200 OK                                                  
Connection: close
Content-Type: text/plain
Date: Tue, 09 Jan 2024 14:25:26 GMT
Server: Kestrel
Transfer-Encoding: chunked

Hello World
```

## Features

- Automatic generation of the HTTP REST endpoints from PostgreSQL functions and procedures.
- Native AOT Ready. AOT is ahead-of-time compiled to the native code. No dependencies, native executable, it just runs and it's very fast.
- Customization of endpoints with comment annotations. You can easily configure any endpoint by adding annotation labels to routine comments. Like for example HTTP GET if you want to change the method verb to GET.
- Automatic HTTP files. Create ready-to-run HTTP files easily, for testing, debugging and discovery.
- Interact seamlessly with .NET8 backend and take advantage of .NET8 features.
- High performance with or without native AOT, up to 6 times higher throughput than similar solutions.

### Automatic Generation of REST Endpoints

See the introductory example above. Automatically build HTTP REST endpoints from PostgreSQL functions and procedures and configure them the way you like.

### Native AOT Ready

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

```http
Connection: close                                                
Content-Type: text/html
Date: Thu, 18 Jan 2024 11:00:39 GMT
Server: Kestrel
Transfer-Encoding: chunked

<div>Hello World</div>
```

### Automatic HTTP Files

Create automatically [HTTP file(s[)](https://learn.microsoft.com/en-us/aspnet/core/test/http-files?view=aspnetcore-8.0) with ready-to-run randomized test example calls.

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

K6 performance tests, 10 seconds, 100 virtual users.

PostgREST:

```console
   ✓ http_req_duration..............: avg=37.91ms min=559.1µs  med=1.67ms  max=1.5s    p(90)=113.81ms p(95)=200.96ms
     http_reqs......................: 13377   1315.56161/s
```

NpgsqlRest AOT build:

```console
   ✓ http_req_duration..............: avg=5.65ms   min=524.55µs med=4.61ms  max=77.83ms p(90)=10.9ms   p(95)=13.61ms
     http_reqs......................: 86458   8624.438772/s
```

> In simple terms, for a period of a 10 seconds, and for 100 simulated virtual concurrent users, PostgREST has generated 13377 succesful requests with average duration of 37.91ms, and NpgsqlRest AOT build has generated 86458 succesful requests with average duration of 5.65ms.

See more details [here](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRestPerfTest).
  
## Getting Started

### Installation

Install the package from NuGet by using any of the available methods:

```console
dotnet add package NpgsqlRest --version 1.2.0
```
```console
NuGet\Install-Package NpgsqlRest -version 1.2.0
```
```xml
<PackageReference Include="NpgsqlRest" Version="1.2.0" />
```
```console
#r "nuget: NpgsqlRest, 1.2.0"
```

### First Use

Your application builder code:

```csharp
var app = builder.Build();
app.UseNpgsqlRest(new("Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"));
app.Run();
```

For all available build options, please consult a [source code file](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/NpgsqlRestOptions.cs), until the documentation website is built.

## Dependencies

- net8.0
- Microsoft.NET.Sdk.Web 8.0
- Npgsql 8.0.1
- PostgreSQL >= 13

Note: PostgreSQL 13 minimal version is required to run the initial query to get the list of functions. The source code of this query can be found [here](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/RoutineQuery.cs#L9C9-L9C49). For versions prior to version 13, this query can be replaced with a custom query that can run on older versions.

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
