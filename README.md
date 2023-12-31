# NpgsqlRest

Note: this is a work in progress Nugget hasn't been published yet.

NpgsqlRest is a .NET 8 library that builds PostgreSQL functions and procedures into RESTful APIs:

```sql
create function hello_world() 
returns text 
language sql
as $$
select 'Hello World'
$$;
```

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();
app.UseNpgsqlRest(new("Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"));
app.Run();
```

```
@host=http://localhost:5000

// function public.hello_world()
// returns text
POST {{host}}/api/hello-world/
```

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
- Customizable URL paths, verbs, headers, etc for each endpoint.
- Built-in logging and error handling.
- Support for authorization and security measures.
- Extensive configuration options to suit your needs.

## Getting Started

### Prerequisites

- .NET 8
- PostgreSQL

### Installation

1. Clone the repository: `git clone https://github.com/yourusername/NpgsqlRest.git`
2. Navigate to the project directory: `cd NpgsqlRest`
3. Build the project: `dotnet build`

### Documentation

For more detailed information on how to use NpgsqlRest, please refer to the documentation.

### Contributing

We welcome contributions from the community. Please read our contributing guide for more information.

### License

This project is licensed under the terms of the MIT license.

### Contact

If you have any questions or feedback, please feel free to contact us.
