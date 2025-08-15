# NpgsqlRest

[![Build, Test, Publish and Release](https://github.com/vb-consulting/NpgsqlRest/actions/workflows/build-test-publish.yml/badge.svg)](https://github.com/vb-consulting/NpgsqlRest/actions/workflows/build-test-publish.yml)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

**Automatic PostgreSQL API Standalone Server** - The Ultimate Solution for PostgreSQL REST APIs

>
> Transform your PostgreSQL database into a **production-ready REST API server** instantly. NpgsqlRest automatically discovers your database schema and creates **blazing fast REST endpoints** with **zero configuration required**.
>

## Why Choose NpgsqlRest?

NpgsqlRest is the superior alternative to existing PostgreSQL REST API solutions, offering unmatched performance, flexibility, and ease of deployment.

### Standalone Server (Primary Solution)
- **Zero Installation Required**. Download and run the executable - no .NET runtime needed
- **Auto-Discovery**. Instantly creates REST endpoints from your PostgreSQL functions, procedures, and tables
- **Production-Ready**. Enterprise-grade server with authentication, SSL, monitoring, and performance optimization
- **High Performance**. Blazing fast native executable. See [Performance Benchmarks](https://github.com/vb-consulting/pg_function_load_tests)
- **Advanced Features**. Real-time streaming, file operations, code generation, and comprehensive security

### .NET Library Component (Secondary Option)
- **Middleware Integration**. Add PostgreSQL REST API capabilities to existing .NET applications
- **AOT-Ready**. Ahead-of-time compilation support for maximum performance
- **Flexible Configuration**. Programmatic control over API generation and behavior

### Key Features
- **Customizable Endpoints**. Configure HTTP methods, paths, and behavior using PostgreSQL comment annotations
- **Real-time Streaming**. Server-sent events with PostgreSQL `RAISE INFO` statements
- **Authentication & Authorization**. Role-based security with OAuth providers support
- **Code Generation**. Auto-generated HTTP files and TypeScript/JavaScript clients
- **CRUD Operations**. Automatic table/view endpoints with customizable patterns

## Get Started in Seconds

### Option 1: Standalone Server (Recommended)

1. **Download** the executable from [Releases](https://github.com/vb-consulting/NpgsqlRest/releases)
2. **Configure** your PostgreSQL connection in `appsettings.json`
3. **Run** the executable - your REST API is live!

**Enterprise Features Included:**
- **Authentication**: Cookie auth, Bearer tokens, OAuth (Google, LinkedIn, GitHub, Microsoft, Facebook)
- **Security**: SSL/TLS, CORS, antiforgery tokens, data protection with configurable encryption
- **File Operations**: Static file serving with template parsing, file uploads (filesystem, PostgreSQL Large Objects, CSV/Excel processing)
- **Performance**: Response compression, configurable Kestrel limits, thread pool tuning
- **Monitoring**: Comprehensive logging (console, file, PostgreSQL), request tracking, connection analytics
- **Code Generation**: Auto-generated HTTP files and TypeScript/JavaScript clients
- **CRUD Operations**: Automatic table/view endpoints with customizable URL patterns

### Option 2: .NET Library Integration

Add NpgsqlRest to your existing .NET application:

```console
dotnet add package NpgsqlRest
```

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();
var connectionStr = "Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres";
app.UseNpgsqlRest(new(connectionStr));
app.Run();
```

## Complete Example

#### 1) Your PostgreSQL Function

```sql
create function hello_world()                                    
returns text 
language sql
as $$
select 'Hello World'
$$;
```

#### 2) Start the Server

**Standalone Server:**
```bash
./NpgsqlRest
```

**Or integrate into .NET app:**
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

Configure individual endpoints with powerful and simple routine comment annotations. You can use any PostgreSQL administration tool or a simple script to customize HTTP methods, paths, content types, authentication, real-time streaming, and client code generation.

#### Quick Reference

| Annotation | Example | Purpose |
|------------|---------|---------|
| `HTTP GET /path` | Custom endpoint path and method |
| `Content-Type: text/html` | Response content type |
| `authorize role1, role2` | Role-based authorization |
| `info_path /events` | Enable event streaming |
| `tsclient = false` | Disable TypeScript client generation |

#### Basic Examples

**Custom HTTP Method and Path:**
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

**Authentication and Authorization:**
```sql
create function secure_data()
returns json
language sql
as $$
select '{"message": "Secret data"}'::json;
$$;

comment on function secure_data() is '
HTTP GET /api/secure
authorize admin, manager';
```

**Real-time Event Streaming:**
```sql
create function live_updates()
returns void
language plpgsql
as $$
begin
    raise info 'Processing started...';
    perform pg_sleep(2);
    raise info 'Step 1 completed';
    perform pg_sleep(2);
    raise info 'All done!';
end;
$$;

comment on function live_updates() is '
HTTP POST /api/live-updates
info_path /events
info_scope all';
```

**TypeScript Client Control:**
```sql
create function admin_function()
returns text
language sql
as $$
select 'Admin data';
$$;

comment on function admin_function() is '
HTTP GET /admin/data
authorize admin
tsclient_events = true
tsclient_status_code = true';
```

#### Parameter Format

You can also use the parameter format for complex configurations:

```sql
comment on function my_function() is '
method = GET
path = /custom/endpoint
content_type = application/json
authorize = admin, user
info_path = /stream
info_scope = matching
tsclient = true
tsclient_events = false';
```

#### Advanced Info Streaming

Control message scope per individual `RAISE INFO` statement:

```sql
create function detailed_process()
returns void
language plpgsql
as $$
begin
    raise info 'Starting process...' using hint = 'all';
    raise info 'Processing user data...' using hint = 'authorize admin';
    raise info 'Process completed' using hint = 'self';
end;
$$;
```

Response will have content type `text/html`:

```console
Connection: close                                                
Content-Type: text/html
Date: Thu, 18 Jan 2024 11:00:39 GMT
Server: Kestrel
Transfer-Encoding: chunked

<div>Hello World</div>
```

## Documentation & Advanced Configuration

### Standalone Server
Download the executable from [Releases](https://github.com/vb-consulting/NpgsqlRest/releases) and configure via JSON - no installation required.

### .NET Library Integration

**Installation:**
```console
dotnet add package NpgsqlRest --version 2.30.0
```
```xml
<PackageReference Include="NpgsqlRest" Version="2.30.0" />
```

**Configuration:**
```csharp
var app = builder.Build();
app.UseNpgsqlRest(new("Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"));
app.Run();
```

For comprehensive configuration options, see the **[options documentation](https://vb-consulting.github.io/npgsqlrest/options/).**

### System Requirements
- PostgreSQL >= 13
- .NET 9.0 (for library integration only)
- Npgsql 8.0.5

Note: PostgreSQL 13 minimal version is required to run the initial query to get the list of functions. The source code of this query can be found [here](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/RoutineQuery.cs). For versions prior to version 13, this query can be replaced with a custom query that can run on older versions.

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
