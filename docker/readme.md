# NpgsqlRest

**Automatic PostgreSQL Web Server**

>
> Transform your PostgreSQL database into a **production-ready, blazing-fast REST API Standalone Web Server**. 
>
> Generate code, build entire applications and more.
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
- **Encrypted Tokens**. Encrypted security tokens with advanced encryption key management and storage options (file, database, etc.).
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
- **High Performance**. Blazing fast native executable. See [Performance Benchmarks](https://github.com/NpgsqlRest/pg_function_load_tests).
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
- **Native Executables**. Native executable builds, including ARM versions, have zero dependencies and extremely fast startup times.
- **Containerization**. Docker-ready hub images.
- **NPM Package**. Additional distribution channel as NPM package.
- **Environment Configuration**. Flexible environment variable and configuration management.
- **Data Protection**. Advanced encryption key management and storage options.
- **Structured Logging**. Industry standard Serilog logger for Console, rolling file or PostgreSQL database logging.
- **Excel Processing**. Upload handler for Excel files that supports Excel content processing.

### Additional Features
- **Upload Handlers**. Multiple upload handlers implemented: File System, Large Objects, CSV/Excel, etc., with code generation. Make complex upload and processing pipelines in minutes. 
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

1. **Annotate some PostgreSQL Functions** to enable HTTP Endpoint.
2. **Prepare Server Executable** (download, install or pull).
3. **Configure** your PostgreSQL connection in `appsettings.json`
4. **Run** the executable - your REST API server is live!

## Complete Example

### 1) Annotate PostgreSQL Function

Let's create a simple Hello World function and add a simple comment annotation:

```sql
create function hello_world()
returns text
language sql
as $$
select 'Hello World'
$$;

comment on function hello_world() is '
HTTP GET /hello
authorize admin
';
```

This annotation will create an `HTTP GET /hello` endpoint that returns "Hello World" and will authorize only the admin role. 

We could also add any HTTP response header, like for example `Content-Type: text/plain`, but since this function returns text, the response will be `text/plain` anyhow. 

Note: Anything that is not a valid HTTP header or a comment annotation that alters behavior will be ignored and treated as a function comment.

### 2) Prepare Server Executable

You have a choice to do the best approach that suits you. Either one of these things:

#### Download Executable 

Download the appropriate executable for your target OS from [Releases](https://github.com/NpgsqlRest/NpgsqlRest/releases) page. You can use manual download, wget, or anything you want. Just remember to assign appropriate executable permissions after the download.

#### NPM Install

```console
~/dev
❯ npm i npgsqlrest

added 1 package in 31s
```

Note: NPM package will do the same thing on install automatically: Download the appropriate executable for your target OS from [Releases](https://github.com/NpgsqlRest/NpgsqlRest/releases) page.

#### Docker Pull

```console
~/dev
❯ docker pull vbilopav/npgsqlrest:latest
latest: Pulling from vbilopav/npgsqlrest
Digest: sha256:70b4057343457e019657dca303acbed8a1acd5f83075ea996b8e6ea20dac4b48
Status: Image is up to date for vbilopav/npgsqlrest:latest
docker.io/vbilopav/npgsqlrest:latest

~/dev
```

### 3) Add Minimal Configuration

Minimal Configuration is `appsettings.json` file with one connection string. You can do that with any editor or, using bash:

```console
~/dev
❯ cat > appsettings.json << EOF
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"
  }
}
EOF
```

### 4) Run

Type executable name:

```console
~/dev
❯ ./npgsqlrest
[11:33:35.440 INF] Started in 00:00:00.0940095, listening on ["http://localhost:8080"], version 2.26.0.0 [NpgsqlRest]
```

Or, run as NPX command for NPM distributions:

```console
~/dev
❯ npx npgsqlrest
[11:33:35.440 INF] Started in 00:00:00.0940095, listening on ["http://localhost:8080"], version 2.26.0.0 [NpgsqlRest]
```

Or, run the appropriate Docker command (expose the 8080 default port and bind the default configuration):

```bash
~/dev
❯ docker run -p 8080:8080 -v ./appsettings.json:/app/appsettings.json --network host vbilopav/npgsqlrest:latest
[11:33:35.440 INF] Started in 00:00:00.0940095, listening on ["http://localhost:8080"], version 2.26.0.0 [NpgsqlRest]
```

Congratulations, your High Speed Web Server is running with `/hello` endpoint exposed.  

### Next Steps

Now that we have our server up and running, we can add some more configuration to make things interesting:

- Configure the Debug Log level for our NpgsqlRest server.
- Enable `HttpFileOptions` for the `HttpFile` plugin that generates HTTP files for testing.
- Enable  `ClientCodeGen` for `TsClient` plugin that generates TypeScript code for us.

The configuration file should look like this:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"
  },
  "Log": {
    "MinimalLevels": {
      "NpgsqlRest": "Debug"
    }
  },
  "NpgsqlRest": {
    "HttpFileOptions": {
      "Enabled": true,
      "NamePattern": "./src/http/{0}_{1}"
    },
    "ClientCodeGen": {
      "Enabled": true,
      "FilePath": "./src/app/api/{0}Api.ts"
    }
  }
}
```

After running with this configuration, we will see much more information in the console:

```console
~/dev
❯ ./npgsqlrest
[12:46:05.120 DBG] ----> Starting with configuration(s): JsonConfigurationProvider for 'appsettings.json' (Optional), JsonConfigurationProvider for 'appsettings.Development.json' (Optional), CommandLineConfigurationProvider [NpgsqlRest]
[12:46:05.135 DBG] Using main connection string: Host=127.0.0.1;Database=my_db;Username=postgres;Password=******;Application Name=dev;Enlist=False;No Reset On Close=True [NpgsqlRest]
[12:46:05.149 DBG] Attempting to open PostgreSQL connection (attempt 1/7) [NpgsqlRest]
[12:46:05.194 DBG] Successfully opened PostgreSQL connection on attempt 1 [NpgsqlRest]
[12:46:05.199 DBG] Using Data Protection for application dev with default provider. Expiration in 90 days. [NpgsqlRest]
[12:46:05.214 DBG] Using RoutineSource PostgreSQL Source [NpgsqlRest]
[12:46:05.215 DBG] Using CrudSource PostgreSQL Source [NpgsqlRest]
[12:46:05.309 DBG] Function public.hello_world mapped to POST /api/hello-world has set HTTP by the comment annotation to GET /hello [NpgsqlRest]
[12:46:05.311 DBG] Created endpoint GET /hello [NpgsqlRest]
[12:46:05.332 DBG] Created HTTP file: ./src/http/todo_public.http [NpgsqlRest.HttpFiles]
[12:46:05.340 DBG] Created Typescript type file: ./src/app/api/publicApiTypes.d.ts [NpgsqlRest.TsClient]
[12:46:05.340 DBG] Created Typescript file: ./src/app/api/publicApi.ts [NpgsqlRest.TsClient]
[12:46:05.358 INF] Started in 00:00:00.2759846, listening on ["http://localhost:8080"], version 2.26.0.0 [NpgsqlRest]
```

Also, two more files will be generated on startup:

1) `todo_public.http`

HTTP file for testing, debugging, and development (VS Code requires rest-client plugin).

```console
@host=http://localhost:8080

// function public.hello_world()
// returns text
//
// comment on function public.hello_world is 'HTTP GET /hello
// authorize admin';
GET {{host}}/hello

###
```

2) `publicApi.ts`

TypeScript fetch module that you can import and use in your Frontend project immediately.

```ts
// autogenerated at 2025-08-17T11:06:58.6605710+02:00
const baseUrl = "http://localhost:8080";

export const publicHelloWorldUrl = () => baseUrl + "/hello";

/**
 * function public.hello_world()
 * returns text
 *
 * @remarks
 * comment on function public.hello_world is 'HTTP GET /hello
 * authorize admin';
 *
 * @returns {{status: number, response: string}}
 *
 * @see FUNCTION public.hello_world
 */
export async function publicHelloWorld() : Promise<{status: number, response: string}> {
    const response = await fetch(publicHelloWorldUrl(), {
        method: "GET",
    });
    return {
        status: response.status,
        response: await response.text()
    };
}
```
For a full list of configuration options, see the [default configuration file](https://github.com/NpgsqlRest/NpgsqlRest/blob/master/NpgsqlRestClient/appsettings.json). Any settings your configuration file will override these defaults.

Also, you can override these settings with console parameters. For example,e to enable Debug Level for NpgsqlRest run:

```console
~/dev
❯ ./npgsqlrest --log:minimallevels:npgsqlrest=debug
...
```

And finally, to see all command line options, use `-h` or `--help`:

```console
~/dev
❯ ./npgsqlrest --help
Usage:
npgsqlrest                               Run with the optional default configuration files: appsettings.json and appsettings.Development.json. If these file are not found, default configuration setting is used (see
                                         https://github.com/NpgsqlRest/NpgsqlRest/blob/master/NpgsqlRestClient/appsettings.json).
npgsqlrest [files...]                    Run with the custom configuration files. All configuration files are required. Any configuration values will override default values in order of appearance.
npgsqlrest [file1 -o file2...]           Use the -o switch to mark the next configuration file as optional. The first file after the -o switch is optional.
npgsqlrest [file1 --optional file2...]   Use --optional switch to mark the next configuration file as optional. The first file after the --optional switch is optional.
Note:                                    Values in the later file will override the values in the previous one.
                                          
npgsqlrest [--key=value]                 Override the configuration with this key with a new value (case insensitive, use : to separate sections). 
                                          
npgsqlrest -v, --version                 Show version information.
npgsqlrest -h, --help                    Show command line help.
                                          
                                          
Examples:                                 
Example: use two config files            npgsqlrest appsettings.json appsettings.Development.json
Example: second config file optional     npgsqlrest appsettings.json -o appsettings.Development.json
Example: override ApplicationName config npgsqlrest --applicationname=Test
Example: override Auth:CookieName config npgsqlrest --auth:cookiename=Test
...
```

## System Requirements
- PostgreSQL >= 13
- No runtime dependencies - native executable

## .NET Library Integration
For integrating into existing .NET applications:
```console
dotnet add package NpgsqlRest
```

Note: PostgreSQL 13 minimal version is required to run the initial query to get the list of functions. The source code of this query can be found [here](https://github.com/NpgsqlRest/NpgsqlRest/blob/master/NpgsqlRest/RoutineQuery.cs). For versions prior to version 13, this query can be replaced with a custom query that can run on older versions.

## Contributing

Contributions from the community are welcome.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
