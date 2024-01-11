# NpgsqlRest

> Note: this still is a work in progress NuGet library hasn't been published yet.

NpgsqlRest is a .NET 8 library that builds PostgreSQL functions and procedures into RESTful APIs. Simple example:

## 1) PostgreSQL Function

```sql
create function hello_world() 
returns text 
language sql
as $$
select 'Hello World'
$$;
```

## 2) .NET8 AOT Ready Web App

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();
app.UseNpgsqlRest(new("Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"));
app.Run();
```

## 3) Auto-Generated HTTP File (Optional)

```
@host=http://localhost:5000

// function public.hello_world()
// returns text
POST {{host}}/api/hello-world/
```

## 4) Endpoint Response

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
- Customizable URL paths, verbs, headers, authorization control, logging, etc, for each endpoint.
- Individual configuration and customization through function or procedure comments.
- Automatic HTTP file creation.
- Native AOT (ahead-of-time compilation) deployment: AOT ready.

## Getting Started

## Dependencies

- net8.0
- Microsoft.NET.Sdk.Web 8.0
- Npgsql 8.0.1

## Installation

## Documentation

For more detailed information on how to use NpgsqlRest, please refer to the documentation.

## Contributing

We welcome contributions from the community. Please read our contributing guide for more information.

## License

This project is licensed under the terms of the MIT license.

## Contact

If you have any questions or feedback, please feel free to contact us.
