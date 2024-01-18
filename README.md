# NpgsqlRest

> Note: this still is a work in progress NuGet library hasn't been published yet.

![build-test-publish](https://github.com/vb-consulting/NpgsqlRest/workflows/build-test-publish/badge.svg)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

Automatic REST API for Any Postgres Database as NET8 Middleware

**1) PostgreSQL Function**

```sql
create function hello_world() 
returns text 
language sql
as $$
select 'Hello World'
$$;
```

**2) .NET8 AOT Ready Web App**

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();
app.UseNpgsqlRest(new("Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"));
app.Run();
```

**3) Auto-Generated HTTP File (Optional)**

```
@host=http://localhost:5000

// function public.hello_world()
// returns text
POST {{host}}/api/hello-world/
```

**4) Endpoint Response**

```
HTTP/1.1 200 OK
Connection: close
Content-Type: text/plain
Date: Tue, 09 Jan 2024 14:25:26 GMT
Server: Kestrel
Transfer-Encoding: chunked

Hello World
```

## Features

- Automatic generation of REST endpoints from PostgreSQL functions.
- Native AOT Ready.
- Customization of endpoints with comment annotations.
- Automatic HTTP files.
- Interact seamlessly with NET8 backend and take advantage of NET8 features.

### Automatic Generation of REST Endpoints

See the introductory example above.

### Native AOT Ready

With the NET8 you can build into native code code (ahead-of-time (AOT) compilation). 

NpgsqlRest is fully native AOT-ready and AOT-tested.

AOT builds have faster startup time, smaller memory footprints and don't require any .NET runtime installed.

### Comment Annotations

Configure individual endpoints with powerful and simple routine comment annotations. You can use any PostgreSQL administration tool or a simple script:

```sql
create function hello_world_html() returns text language sql as 
$$
select '<div>Hello World</div>';
$$

comment on function hello_world_html() is '
Using comment annotations to configure this endpoint.
HTTP GET /hello
Content-Type: text/html';
```

```
info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
      Request finished HTTP/1.1 GET http://localhost:5000/api/hello - 200 - text/html 29.7810ms
```

```
Connection: close
Content-Type: text/html
Date: Thu, 18 Jan 2024 11:00:39 GMT
Server: Kestrel
Transfer-Encoding: chunked

<div>Hello World</div>
```

### Automatic HTTP Files

Create automatically [HTTP file(s)](https://learn.microsoft.com/en-us/aspnet/core/test/http-files?view=aspnetcore-8.0) with ready-made randomized test example calls.

## NET8 backend

NpgsqlRest is implemented as a NET8 middleware component, which means that anything that is available in NET8 is also available to the NpgsqlRest REST endpoints. And that is, well, everything. From rate limiters to all kinds of authorization schemas, to name a few.

You can also interact with the NET8 calling code. Do you want to supply the username to all parameters named "user"? No problem, how about this:

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

## Getting Started

### Installation

- .NET CLI:
```
dotnet add package NpgsqlRest
```

- Package Manager:
```
NuGet\Install-Package Norm.net
```

- Package Reference:
```xml
<PackageReference Include="NpgsqlRest" />
```

- Script & Interactive:
```
#r "nuget: NpgsqlRest"
```

### First Use

Your application builder code:

```csharp
var app = builder.Build();
app.UseNpgsqlRest(new("Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"));
app.Run();
```

For all available build options, please consult a [source code file](https://github.com/vb-consulting/NpgsqlRest/blob/master/source/NpgsqlRest/NpgsqlRestOptions.cs), until proper documentation is built.

## Dependencies

- net8.0
- Microsoft.NET.Sdk.Web 8.0
- Npgsql 8.0.1


## Contributing

We welcome contributions from the community. Please make a pull request if you whish to contribute.

## License

This project is licensed under the terms of the MIT license.

## Contact

If you have any questions or feedback, please feel free to contact us.
