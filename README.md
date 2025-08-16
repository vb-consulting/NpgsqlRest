# NpgsqlRest

[![Build, Test, Publish and Release](https://github.com/vb-consulting/NpgsqlRest/actions/workflows/build-test-publish.yml/badge.svg)](https://github.com/vb-consulting/NpgsqlRest/actions/workflows/build-test-publish.yml)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

**Automatic PostgreSQL API Standalone Server**

>
> Transform your PostgreSQL database into a **production-ready, blazing-fast REST API Standalone Web Server**. Generate code, build entire applications and more.
>

## Enterprise-Grade PostgreSQL REST API Server

NpgsqlRest is the superior alternative to existing automatic PostgreSQL REST API solutions, offering unmatched performance and advanced enterprise features.

**Download, configure, and run** - your REST API server is live in seconds with comprehensive enterprise features built-in.

## Core Features

### Endpoints & Configuration 
- **Instant API Generation**. Automatically creates REST endpoints from PostgreSQL functions, procedures, tables, and views.
- **Minimal Configuration**. Works out-of-the-box with any PostgreSQL database with minimal configuration file. You only need connection info to get started.
- **Comment Annotations**. Control and configure endpoint behavior from your database using **declarative comment annotations system.**
- **Declarative Configuration**. Declare to database how your endpoint should behave. Focus on end-results of your system, not on how it will be implemented.
- **HTTP Customization**. Set methods, paths, content types, and response headers directly in your database declarations.
- **Authentication Control**. Configure authorization, roles, and security per endpoint in your database declarations.
- **Real-Time Streaming**. Enable server-sent events and fine control event scoping (user, roles, etc.) directly in your database declarations.
- **Response Formatting**. Control output formats, caching, timeouts, and raw responses in your database declarations, and more.

### Code Generation
- **JavaScript**. Generate automatically fetch modules for all endpoints in development mode. Slash development time dramatically and reduce bugs.
- **TypeScript**. Generate type-safe interfaces and types for generated fetch modules. Bring static type checking for your PostgreSQL database.
- **HTTP Files**. Auto-generated REST client files, for all generated endpoints, for testing, development and auto-discovery.

### Authentication & Security
- **Multiple Auth Methods**. Cookie authentication, Bearer tokens, and external OAuth providers.
- **Encrypted Tokens**. Encrypted security tokens with advanced encryption key management and storage options (file, database, etc).
- **CORS Support**. Cross-origin resource sharing configuration for Bearer token access.
- **Built-in Password Validation**. Built-in extendable and secure password hashing and validation. PBKDF2-SHA256 with 600,000 iterations aligned with OWASP's 2023+ recommendations.
- **OAuth Integration**. Google, LinkedIn, GitHub, Microsoft and Facebook support built-in.
- **Claims-based security**. User assertions cached in encrypted security token.
- **Role-Based Authorization**. Fine-grained access control with PostgreSQL role integration.
- **Claim or Role Parameter Mapping**. Automatically map user claims or roles to parameters.
- **Claim or Role Context Mapping**. Automatically map user claims or roles to PostgreSQL connection context.
- **CSRF Protection**. Antiforgery token support for secure uploads and form submissions.
- **SSL/TLS**. Full HTTPS support with certificate management.
- **PostgreSQL Security and Encryption**. Database connection security features courtesy of [Npgsql](https://www.npgsql.org/doc/connection-string-parameters.html#security-and-encryption). Includes SSL, Certificates, Kerberos and more.

### Performance & Scalability
- **High Performance**. Blazing fast native executable. See [Performance Benchmarks](https://github.com/vb-consulting/pg_function_load_tests).
- **Connection Pooling**. Built-in connection pooler, courtesy of [Npgsql](https://www.npgsql.org/doc/connection-string-parameters.html#pooling).
- **KeepAlive, Auto-prepare, Buffer Size**. Other performance tweaks and settings courtesy of [Npgsql](https://www.npgsql.org/doc/connection-string-parameters.html#performance).
- **Failover, Load Balancing**. Set multiple hosts in connection string for failover and balancing.
- **Multiple Connections**. Define multiple connections and set specific connections (read-only, write-only) per endpoint in your database declarations.
- **Connection Retry**. Robust and configurable built-in connection retry mechanism.
- **Thread Pool Optimization**. Configurable thread pool settings for maximum throughput.
- **Request Optimization**. Kestrel server tuning with configurable limits.
- **Response Compression**. Brotli and Gzip compression with configurable levels.
- **HTTP Caching**. Define endpoint caching per endpoint in your database declarations.
- **Server Caching**. Define endpoint in-memory server caching per endpoint in your database declarations.

### Real-Time & Streaming
- **Server-Sent Events**. Innovative real-time streaming with PostgreSQL `RAISE INFO` statements. No database locking.
- **Live Notifications**. Push updates to clients in real-time.
- **Event Sources**. Auto-generated client code for streaming connections.
- **Custom Scopes**. Define Server-Sent Event Scope (specific user, groups of users or roles, etc.) per endpoint or per event in your database declarations.

### Enterprise Features
- **Containerization**. Docker-ready hub images.
- **NPM Package**. Additional distribution channel as NPM package.
- **Environment Configuration**. Flexible environment variable and configuration management.
- **Data Protection**. Advanced encryption key management and storage options.
- **Structured Logging**. Industry standard Serilog logger for Console, rolling file or PostgreSQL database logging.
- **Excel Processing**. Upload handler for Excel files that supports Excel content processing.

### Additional Features
- **Upload Handlers**. Multiple upload handlers implemented: File System, Large Objects, CSV/Excel, etc, with code generation. Make complex upload and processing pipelines in minutes. 
- **Static Files**. Built-in serving of static content with high speed template parser for user claims and authorization features.
- **Request Tracking**. Detailed request analytics and connection monitoring.
- **Performance Metrics**. Built-in performance monitoring and diagnostics.
- **Error Handling**. Advanced PostgreSQL error code mapping to HTTP status codes.
- **Custom Headers**. Configurable request/response header management in your database declarations.
- **IP Tracking**. Client IP address parameter or PostgreSQL connection context for tracking.
- **.NET Library Integration**. Version with core features implemented as .NET Nuget library for .NET project integration.

And more!

## Get Started in Seconds

Starting is easy:

1. Add NPM package or **download** the executable.
2. **Configure** your PostgreSQL connection in `appsettings.json`
3. **Run** the executable - your REST API server is live!

- Add NPM package:

```bash
❯ npm i npgsqlrest

added 1 package in 31s
```

- Add minimal configuration:

```bash
❯ cat > appsettings.json << EOF
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"
  }
}
EOF
```

- Run the server executable. You will see the logs like this:

```bash
❯ npx npgsqlrest
[11:33:35.348 INF] ----> Starting with configuration(s): JsonConfigurationProvider for 'appsettings.json' (Optional), JsonConfigurationProvider for 'appsettings.Development.json' (Missing), CommandLineConfigurationProvider [NpgsqlRest]
[11:33:35.351 INF] Using main connection string: Host=127.0.0.1;Database=todo;Username=todo_app;Password=******;Application Name=dev;Enlist=False;No Reset On Close=True [NpgsqlRest]
[11:33:35.354 INF] Using RoutineSource PostgreSQL Source [NpgsqlRest]
[11:33:35.440 INF] Started in 00:00:00.0940095, listening on ["http://localhost:8080"], version 2.26.0.0 [NpgsqlRest]
```

Note: you can use `-v` or `--version` to dump all versions (including libraries used) or `-h` or `--help` to dump additional help information.

Alternatively to this, you can download the appropriate executable for your target OS from [Releases](https://github.com/vb-consulting/NpgsqlRest/releases) page. Just remember to assign appropriate executable permissions.

Similarly, you can also use Docker version if you prefer. Just make sure you bind configuration, use appropriate ports and appropriate network where your database is located:

```bash
~/dev
❯ docker pull vbilopav/npgsqlrest:latest
latest: Pulling from vbilopav/npgsqlrest
Digest: sha256:70b4057343457e019657dca303acbed8a1acd5f83075ea996b8e6ea20dac4b48
Status: Image is up to date for vbilopav/npgsqlrest:latest
docker.io/vbilopav/npgsqlrest:latest

~/dev
❯ docker run -p 8080:8080 -v ./appsettings.json:/app/appsettings.json --network host vbilopav/npgsqlrest:latest
[11:33:35.348 INF] ----> Starting with configuration(s): JsonConfigurationProvider for 'appsettings.json' (Optional), JsonConfigurationProvider for 'appsettings.Development.json' (Missing), CommandLineConfigurationProvider [NpgsqlRest]
[11:33:35.351 INF] Using main connection string: Host=127.0.0.1;Database=todo;Username=todo_app;Password=******;Application Name=dev;Enlist=False;No Reset On Close=True [NpgsqlRest]
[11:33:35.354 INF] Using RoutineSource PostgreSQL Source [NpgsqlRest]
[11:33:35.440 INF] Started in 00:00:00.0940095, listening on ["http://localhost:8080"], version 2.26.0.0 [NpgsqlRest]
```

**That's it!** Your PostgreSQL database is now a full-featured REST API server.

For more configuration options, see the [default configuration file](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRestClient/appsettings.json)

## Complete Example

#### 1) Your PostgreSQL Function

```sql
create function hello_world()                                    
returns text 
language sql
as $$
select 'Hello World'
$$;

comment on function hello_world() is '
HTTP GET /hello
Content-Type: text/plain
authorize admin';
```

> The simple comment above transforms the endpoint to use GET method, custom path `/hello`, plain text response, and requires admin authorization - all configured with just a few lines of PostgreSQL comments!

#### 2) Start the Server

Depending on distribution used, run the executable, NPX command or Docker command as described above. 

#### 3) Auto-Generated HTTP File

```console
@host=http://localhost:8080                                      

// function public.hello_world()
// returns text
GET {{host}}/hello
```

#### 4) Auto-Generated Typescript Client Module

```ts
const _baseUrl = "http://localhost:8080";                        


/**
* function public.hello_world()
* returns text
* 
* @remarks
* GET /hello
* 
* @see FUNCTION public.hello_world
*/
export async function getHelloWorld() : Promise<string> {
    const response = await fetch(_baseUrl + "/hello", {
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

## Documentation & Configuration

### System Requirements
- PostgreSQL >= 13
- No runtime dependencies - native executable

### Configuration
All server features are configured via `appsettings.json`. For comprehensive configuration options, see the **[options documentation](https://vb-consulting.github.io/npgsqlrest/options/).**

### .NET Library Integration
For integrating into existing .NET applications:
```console
dotnet add package NpgsqlRest
```

Note: PostgreSQL 13 minimal version is required to run the initial query to get the list of functions. The source code of this query can be found [here](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/RoutineQuery.cs). For versions prior to version 13, this query can be replaced with a custom query that can run on older versions.

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
