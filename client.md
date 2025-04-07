# Client Application

## Download

The client application is available for download from the [latest GitHub release page](https://github.com/vb-consulting/NpgsqlRest/releases).

There are three files available for download:

1) `appsettings.json`: The default configuration file. See the [default configuration file](https://vb-consulting.github.io/npgsqlrest/config/).
2) `npgsqlrest-linux64`: Linux64 build. This is a self-contained ahead-of-time (AOT) compiled to native code executable for the Linux 64x systems.
3) `npgsqlrest-win64.exe`: Windows64 build. This is a self-contained ahead-of-time (AOT) compiled to native code executable for the Windows 64x systems.

## NPM

Installation is possible via the NPM package manager:

```console
npm install npgsqlrest --save-dev
```

The NPM package will download on installation the appropriate executable binary for your target operating system. The command will be available with the NPX runner:

```console
$ npx npgsqlrest [arguments]
```

See [usage](#usage) for more info.

## Custom Builds

The client application was built from the [`NpgsqlRestClient` project directory](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRestClient/).

To create a custom build follow these steps:

1) Make sure that you have .NET8 SDK installed and ready.
2) Clone [NpgsqlRest repository](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRest)
3) Navigate to the [`NpgsqlRestClient` project directory](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRestClient/).
4) Make your desired customizations (or not).
5) Run publish command, for example, `dotnet publish -r win-x64 -c Release --output [target dir]`

Notes: `win-x64` is the designated target OS for the build. Adjust this parameter appropriately for the target OS. See [https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids). The project is already configured for the AOT builds, but you will need to run the publish command from the same flavor OS as the build target OS (Windows for Windows builds, Linux for Linux builds, etc).

## MacOS Builds

The Mac OS builds are missing because I don't have a Mac machine. If someone could help me out with this I'd be grateful.

## Features

- Create an Automatic REST API for the PostgreSQL Databases.
- Generate TypeScript Code and HTTP files for testing.
- Configure security for use the of either encrypted cookies or JWT Bearer tokens or both.
- Expose REST API endpoints for the PostgreSQL Databases as Login/Logout.
- Use external authentication providers such as Google, LinkedIn or GitHub.
- Server static content.
- Use and configure built-in Serilog structured logging.
- Configure Cross-origin resource sharing (CORS) access, SSL, Server Certificates and more, everything needed for modern Web development.

See the [default configuration file](https://vb-consulting.github.io/npgsqlrest/config/) with descriptions for more information.

## Usage

```console
❯ npgsqlrest --help
Usage:
npgsqlrest                               Run with the optional default configuration files: appsettings.json and
                                         appsettings.Development.json. If these file are not found, default
                                         configuration setting is used (see
                                         https://vb-consulting.github.io/npgsqlrest/config/).
npgsqlrest [files...]                    Run with the custom configuration files. All configuration files are required.
                                         Any configuration values will override default values in order of appearance.
npgsqlrest [file1 -o file2...]           Use the -o switch to mark the next configuration file as optional. The first
                                         file after the -o switch is optional.
npgsqlrest [file1 --optional file2...]   Use --optional switch to mark the next configuration file as optional. The
                                         first file after the --optional switch is optional.
Note:                                    Values in the later file will override the values in the previous one.

npgsqlrest [--key=value]                 Override the configuration with this key with a new value (case insensitive,
                                         use : to separate sections).

npgsqlrest -v, --version                 Show version information.
npgsqlrest -h, --help                    Show command line help.


Examples:
Example: use two config files            npgsqlrest appsettings.json appsettings.Development.json
Example: second config file optional     npgsqlrest appsettings.json -o appsettings.Development.json
Example: override ApplicationName config npgsqlrest --applicationname=Test
Example: override Auth:CookieName config npgsqlrest --auth:cookiename=Test

```

## Changelog

## 2.17.0

### Fixed CRUD Plugin

Includes new fixed version of CRUD plugin and fixed configuration and connection issues.

### New Static Files System

The entire section is redone. Now, default configuration looks like this:

```jsonc
{
  //
  // Static files settings 
  //
  "StaticFiles": {
    "Enabled": false,
    "RootPath": "wwwroot",
    "ParseContentOptions": {
      //
      // Enable or disable the parsing of the static files.
      // When enabled, the static files will be parsed and the tags will be replaced with the values from the claims collection.
      // 
      "Enabled": false,
      //
      // List of static file patterns that will parse the content and replace the tags with the values from the claims collection.
      // File paths are relative to the RootPath property and pattern matching is case-insensitive.
      // Pattern can include wildcards or question marks. For example: *.html, *.htm, *.txt, *.json, *.xml, *.css, *.js
      // 
      "FilePaths": [ "*.html" ],
      //
      // Tag name to be replaced with authenticated user id for example {userId}
      //
      "UserIdTag": "userId",
      //
      // Tag name to be replaced with authenticated user name
      //
      "UserNameTag": "userName",
      //
      // Tag name to be replaced with authenticated user roles
      //
      "UserRolesTag": "userRoles",
      //
      // Tag names to be replaced with the values from the claims collection
      //
      "CustomTagToClaimMappings": {}
    }
  }
}
```

Biggest changes:

- Keys `AnonymousPaths` and `LoginRedirectPath` are removed. They might return some day, but not today.
- New `ParseContentOptions` section. Implements high speed template parser to parse configured files with claim values from auth tokens. Use `{userId}` to replace with actual user id `"1213"` (JSON string value), user name `"john"` or array `["admin", "user"]` for user roles (JSON string array format).

### Versions

```
.NET                  9.0.3
Client Build          2.17.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.3.0
NpgsqlRest            2.22.0.0
NpgsqlRest.HttpFiles  1.3.0.0
NpgsqlRest.TsClient   1.18.0.0
NpgsqlRest.CrudSource 1.3.0.0
```

## 2.16.0

### New option `ConnectionSettings.MatchNpgsqlConnectionParameterNamesWithEnvVarNames`

```json
{
  "ConnectionSettings": {
    "MatchNpgsqlConnectionParameterNamesWithEnvVarNames": "NGPSQLREST_{0}_{1}"
  }
}
```

This option will search enviorment variable by provided pattern and add them to the connection string.

If this value is null it will be ignored.

If it has one string formatter `{0}` it will be replaced by the connection string parameter name (see all parameter names here https://www.npgsql.org/doc/connection-string-parameters.html) in upper case with blanks replaced by underlines.

If it has one string formatters: `{0}` and `{1}`, first one will be replaced by the connection name and second by the connection string parameter name, all in upper case with blanks replaced by underlines.

For example if have this value set to `NGPSQLREST_{0}_{1}`, when intializing the `Default` connection for the `SSL Password` parameter, this value will be `NGPSQLREST_DEFAULT_SSL_PASSWORD`.

If we have  `NGPSQLREST_{0}` it will be `NGPSQLREST_SSL_PASSWORD`.

Use this format string to set exact names for various services (Docker for example).

### New option `ConnectionSettings.TestConnectionStrings`

```json
{
  "ConnectionSettings": {
    "TestConnectionStrings": true
  }
}
```

Set to true to test connections before initializing the application and using it. The connection string is tested by opening and closing the connection. Default is true.

### New option `NpgsqlRest.UseMultipleConnections`

```json
{
  "NpgsqlRest": {
    "UseMultipleConnections": false
  }
}
```

Set to true to use multiple connections from the ConnectionStrings section. This sets options dictionary to enable the use of alternate connections to some routines. Routines that have the `ConnectionName` string property set to the existing key in this dictionary will use this connection.

Note: these connections are not used to build metadata. Therefore, the same routine must also exist on a primary connection to be able to build metadata for execution.

- Versions:

```
.NET                  9.0.2
Client Build          2.16.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.3.0
NpgsqlRest            2.21.0.0
NpgsqlRest.HttpFiles  1.3.0.0
NpgsqlRest.TsClient   1.18.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.15.0

- Versions:

```
.NET                  9.0.2
Client Build          2.15.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.3.0
NpgsqlRest            2.20.0.0
NpgsqlRest.HttpFiles  1.3.0.0
NpgsqlRest.TsClient   1.18.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.14.0

This fix contains a fix for edge cases of parse method when using multiple curly braces.

- Versions:

```
.NET                  9.0.2
Client Build          2.14.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.3.0
NpgsqlRest            2.20.0.0
NpgsqlRest.HttpFiles  1.3.0.0
NpgsqlRest.TsClient   1.18.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.13.0

### New option `NpgsqlRest.LogConnectionNoticeEventsMode`

```json
{
  "NpgsqlRest": {
    "LogConnectionNoticeEventsMode": "FirstStackFrameAndMessage"
  }
}
```

The default is the `FirstStackFrameAndMessage` that logs only the first stack frame and the message. In chained calls, the stack frame can be longer and obfuscate the log message. This option will show only the first (starting) stack frame along with the message.

Possible values:

- `MessageOnly`: Log only connection messages.
- `FirstStackFrameAndMessage`: Log last stack trace and message.
- `FullStackAndMessage`: Log full stack trace and message.

### New options in `NpgsqlRest.AuthenticationOptions` section

#### `NpgsqlRest.AuthenticationOptions.BindParameters`

```json
{
  "NpgsqlRest": {
    "AuthenticationOptions": {
      "BindParameters": true
    }
  }
}
```

When this parameter is true, all routine parameters that match values from this section will be automatically assigned. Those are:

- `UserIdParameterName`
- `UserNameParameterName`
- `UserRolesParameterName` 
- `IpAddressParameterName`
- `CustomParameterNameToClaimMappings` (dictionary)

Default is true, which is consistent with the behavior from the previos version. Now it can be set to false if only `ParseResponse` is used.

#### `NpgsqlRest.AuthenticationOptions.ParseResponse`

```json
{
  "NpgsqlRest": {
    "AuthenticationOptions": {
      "ParseResponse": false
    }
  }
}
```

When this parameter is true, content parser will parse every parsable response and replace a value in curly braces by using high perfomance parser. It applies to following values.

- `UserIdParameterName`
- `UserNameParameterName`
- `UserRolesParameterName` 
- `IpAddressParameterName`
- `CustomParameterNameToClaimMappings` (dictionary)


For example the following setup:

```json
{
  "NpgsqlRest": {
    "AuthenticationOptions": {
      "UserIdParameterName": "user_id",
      "ParseResponse": true
    }
  }
}
```

In this configuration, every response that contains this `{user_id}` will be replaced by the `NameIdentifier` claim which represents the standard user id (string).

Value will be parsed as JSON values, which means double quotes for string values and value for roles (`UserRolesParameterName`) is JSON array `["role1","role2"]`. And `null` when value is not present.

- Versions:

```
.NET                  9.0.2
Client Build          2.13.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.3.0
NpgsqlRest            2.20.0.0
NpgsqlRest.HttpFiles  1.3.0.0
NpgsqlRest.TsClient   1.18.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.12.0

...

## 2.11.0

- Upgrade System.Text.Json 9.0.2.
- New Log Options:
  - `"ConsoleMinimumLevel": "Verbose"` - minimum log level for console logging.
  - `"FileMinimumLevel": "Verbose"` - minimum log level for file logging.
  - `"ToPostgres": false` - log to PostgreSQL database, using default connection.
  - `"PostgresCommand": "call log($1,$2,$3,$4,$5)"` - command to execute when logging to PostgreSQL database.
  - `"PostgresMinimumLevel": "Verbose"` - minimum log level for file PostgreSQL database.

These are numerical represntations of log levels:

```csharp
public enum LogEventLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}
```

If we set minumal level to `Warning`, we will receive logs for `Warning`, `Error` and `Fatal` levels.

Default PostgreSQL log command is `call log($1,$2,$3,$4,$5)`, set as see needed. Parameters are always numerical, minumal 1 and maximum 5, where:
  - $1 - Log level in text format: `Verbose`, `Debug`, `Information`, `Warning`, `Error` or `Fatal`.
  - $2 - Log message (text).
  - $3 - Log timestamp UTC timezone.
  - $4 - Exceptin in tex format if any or `NULL` if no excpetion exists.
  - $5 - Source context.

- New option for `NpgsqlRest.AuthenticationOptions` - `IpAddressParameterName` - includes IP information for apramatares with name from this option (default is `null`, not used). Set this to actual parameter name which will always contain client IP value.

Versions:

```
.NET                  9.0.2
Client Build          2.11.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.2.0
NpgsqlRest            2.18.0.0
NpgsqlRest.HttpFiles  1.2.0.0
NpgsqlRest.TsClient   1.17.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.10.0

- Upgrade System.Text.Json 9.0.1.
- Update configuration logging to report missing JSON files.
- Add `CustomParameterMappings` configuration: Defines the custom parameter value mappings for the PostgreSQL routines. Use this to set default parameter values by parameter name.
- Fix `NpgsqlRest.TsClient` bug with short URL segments.

Versions:

```
.NET                  9.0.0
Client Build          2.10.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.2.0
NpgsqlRest            2.17.0.0
NpgsqlRest.HttpFiles  1.2.0.0
NpgsqlRest.TsClient   1.17.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.9.0

Fix CORS configuration. Now it works as expected.

Versions:

```console
.NET                  9.0.0
Client Build          2.9.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.2.0
NpgsqlRest            2.17.0.0
NpgsqlRest.HttpFiles  1.2.0.0
NpgsqlRest.TsClient   1.16.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.8.0

Improved bearer token authentaction mechanism:

Current bearer token authentaction is not JWT token authentaction but rather Microsoft proprietary format (instead of JWT format). In future, standard JWT might be added as an option.

Bearer token authentaction now can expose refresh token endpoint. For example:

POST {{host}}/api/token/refresh
Authorization: Bearer {{token}}
{
    "refresh": "{{refresh}}"
}

This means that we are sending refresh token as JSON body and the endpoint must be authorizaed with a token. That also means that refresh token expiration is meaningless and is removed, since expiration is same as the main token.

Current bearer token configuration looks like this:

```json
//
// Enable Microsoft Bearer Token Auth
//
"BearerTokenAuth": false,
"BearerTokenAuthScheme": null,
"BearerTokenExpireHours": 1,
// POST { "refresh": "{{refreshToken}}" }
"BearerTokenRefreshPath": "/api/token/refresh",
```

Versions:

```console
Versions:
.NET                  9.0.0
Client Build          2.8.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.2.0
NpgsqlRest            2.16.1.0
NpgsqlRest.HttpFiles  1.2.0.0
NpgsqlRest.TsClient   1.16.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.7.0

- NpgsqlRest 2.16 build.
- Added `NoResetOnClose = true` property ba default on all connections. This prevents automatically sending `DISCARD ALL` command when new conncetion is reused by the connection pool.
- Versions:

```console
Versions:
.NET                  9.0.0
Client Build          2.7.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.2.0
NpgsqlRest            2.16.0.0
NpgsqlRest.HttpFiles  1.2.0.0
NpgsqlRest.TsClient   1.16.0.0
NpgsqlRest.CrudSource 1.2.0.0
```


## 2.6.0

- NpgsqlRest 2.15 build.
- Added new NpgsqlRest settings:

```jsonc
{
  "NpgsqlRest": {
    // ...

    //
    // Options for refresh metadata endpoint
    //
    "RefreshOptions": {
      //
      // Refresh metadata endpoint enabled
      //
      "Enabled": false,
      //
      // Refresh metadata endpoint path
      //
      "Path": "/api/npgsqlrest/refresh",
      //
      // Refresh metadata endpoint HTTP method
      //
      "Method": "GET"
    },

    // ...
  }
```

- Added CrudSource in NpgsqlRest settings. CrudSource allows creating ednpoints from tables and views:
```jsonc
{
  "NpgsqlRest": {
    // ...

    //
    // CRUD endpoints for the PostgreSQL tables and views.
    //
    "CrudSource": {
      //
      // Enable or disable the creation of the endpoints for the PostgreSQL tables and views.
      //
      "Enabled": true,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#schemasimilarto
      //
      "SchemaSimilarTo": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#schemanotsimilarto
      //
      "SchemaNotSimilarTo": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#includeschemas
      //
      "IncludeSchemas": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#excludeschemas
      //
      "ExcludeSchemas": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#namesimilarto
      //
      "NameSimilarTo": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#namenotsimilarto
      //
      "NameNotSimilarTo": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#includenames
      //
      "IncludeNames": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#excludenames
      //
      "ExcludeNames": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#commentsmode
      //
      "CommentsMode": "OnlyWithHttpTag",
      //
      // Set of flags to enable or disable the creation of the CRUD endpoints for the specific types of the PostgreSQL tables and views. 
      //
      // Possible values are: 
      // Select, Update, UpdateReturning, Insert, InsertReturning, InsertOnConflictDoNothing, InsertOnConflictDoUpdate, InsertOnConflictDoNothingReturning, 
      // InsertOnConflictDoUpdateReturning, Delete, DeleteReturning, All
      //
      "CrudTypes": [
        "All"
      ]
    }

    // ...
  }
```

- Versions:

```console
Versions:
.NET                  9.0.0
Client Build          2.6.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.2.0
NpgsqlRest            2.15.0.0
NpgsqlRest.HttpFiles  1.2.0.0
NpgsqlRest.TsClient   1.16.0.0
NpgsqlRest.CrudSource 1.2.0.0
```

## 2.5.0

```console
Versions:
.NET                 9.0.0
Client Build         2.5.0.0
Serilog.AspNetCore   8.0.3.0
Npgsql               9.0.1.0
NpgsqlRest           2.14.0.0
NpgsqlRest.HttpFiles 1.1.0.0
NpgsqlRest.TsClient  1.15.0.0
```

Add version on startup.

Changes in configuration, section `ConnectionSettings`:

- `UseEnvironmentConnection` renamed to `UseEnvVars`.
- `UseEnvironmentConnectionWhenMissing` replaced with `EnvVarsOverride`
- `EnvVarsOverride` - When this option is disabled (false), connection parameters will be set from the environment variables only if the connection string parameter is not set.

## 2.4.0

```console
Versions:
.NET                 9.0.0
Client Build         2.4.0.0
Serilog.AspNetCore   8.0.3.0
Npgsql               9.0.1.0
NpgsqlRest           2.13.1.0
NpgsqlRest.HttpFiles 1.1.0.0
NpgsqlRest.TsClient  1.15.0.0
```

- New version Npgsql 9.0.1
- New "ClientCodeGen" "UniqueModels" option:

```jsonc
{
  //...

  "NpgsqlRest": {
    //
    // Enable or disable the generation of TypeScript/Javascript client source code files for NpgsqlRest endpoints.
    //
    "ClientCodeGen": {
      //...

      //
      // Keep TypeScript models unique, meaning, models will same fields and types will be merged into one model with name of the last model. Significantly reduces number of generated models. 
      //
      "UniqueModels": false
    }
}
```

- New ResponseCompression options:

```jsonc
{
  //...

  //
  // Response compression settings
  //
  "ResponseCompression": {
    "Enabled": false,
    "EnableForHttps": false,
    "UseBrotli": true,
    "UseGzipFallback": true,
    "CompressionLevel": "Optimal", // Optimal, Fastest, NoCompression, SmallestSize
    "IncludeMimeTypes": [
      "text/plain",
      "text/css",
      "application/javascript",
      "text/html",
      "application/xml",
      "text/xml",
      "application/json",
      "text/json",
      "image/svg+xml",
      "font/woff",
      "font/woff2",
      "application/font-woff",
      "application/font-woff2"
    ],
    "ExcludeMimeTypes": []
  },

  //...
```

## 2.3.0

```console
Versions:
.NET                 9.0.0
Client Build         2.3.0.0
Serilog.AspNetCore   8.0.3.0
Npgsql               8.0.5.0
NpgsqlRest           2.13.0.0
NpgsqlRest.HttpFiles 1.1.0.0
NpgsqlRest.TsClient  1.14.0.0
```

- Fixed SSL unnecessary redirection warnings when SSL is not used.

## 2.2.1

```console
Versions:
.NET                 8.0.10
Client Build         2.2.1.0
Serilog.AspNetCore   8.0.3.0
Npgsql               8.0.5.0
NpgsqlRest           2.12.1.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.13.0.0
```

Client changes:

Fixed issue in overrding Log config section MinimalLevels. Log MinimalLevels are now:
- System: Warning
- Microsoft: Warning

In previous versions this default wasn't initialized properly that could lead to over-logging.

## 2.2.0

```console
Versions:
.NET                 8.0.10
Client Build         2.2.0.0
Serilog.AspNetCore   8.0.3.0
Npgsql               8.0.5.0
NpgsqlRest           2.12.0.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.13.0.0
```

Client changes:

- New Configuration Section:

```jsonc
/*
  2.2.0.0
*/
{
   //
  // https://vb-consulting.github.io/npgsqlrest/
  //
  "NpgsqlRest": {
    //
    // ...
    //

    //
    // Options for handling PostgreSQL routines (functions and procedures)
    //
    "RoutineOptions": {
      //
      // Name separator for parameter names when using custom type parameters. 
      // Parameter names will be in the format: {ParameterName}{CustomTypeParameterSeparator}{CustomTypeFieldName}. When NULL, default underscore is used.
      //
      "CustomTypeParameterSeparator": null,
      //
      // List of PostgreSQL routine language names to include. If NULL, all languages are included. Names are case-insensitive.
      //
      "IncludeLanguagues": null,
      //
      // List of PostgreSQL routine language names to exclude. If NULL, "C" and "INTERNAL" are excluded by default. Names are case-insensitive.
      //
      "ExcludeLanguagues": null
    },
  }
}
```

- `TsClient` configuration section is renamed to `ClientCodeGen`.

Reason is the new configuration key in this section `"SkipTypes": false` that allows for generation of the pure JavaSCript modules by ommiting type declarations. And now this section can generate either TypeScript or JavaScript which is client code.


## 2.1.0

- NpgsqlRest version    2.11.0.0
- NpgsqlRest.TsClient   1.10.0.0
- Changed the default configuration value for the `CommentsMode`. For now on, default value for this option is more restrictive `OnlyWithHttpTag` instead of previously `ParseAll`.

## 2.0.0

Big changes:

- Removed `CrudSource` plugin from build. I'm not using it and I'm against this approach completely where you access tables directly. This plugin module still exists you can always create your own build if you need it, I don't. Consecvently, entire config section `CrudSource` ir removed, and `RoutinesSource` as well. `RoutinesSource` is configured in the main `NpgsqlRest` config section.

- Removed the default configuration file `appsettings.json` dependency. `appsettings.json` is now optional. All default values in that file are now hardcoded in the build. Use configuration file to override these values. See help prompt above.

- NpgsqlRest version 2.10.0.0

New routine options:

`RoutineEndpoint` option `public string? RawValueSeparator { get; set; } = null;` that maps to new comment annotation `separator`

Defines a standard separator between raw values.

`RoutineEndpoint` option `public string? RawNewLineSeparator { get; set; } = null;` that maps to new comment annotation `newline`

Defines a standard separator between raw value columns.

## 1.5.0

NpgsqlRest version 2.9.0 - support for RAW option and annotation.

## 1.4.0

See the [full diff here](https://github.com/vb-consulting/NpgsqlRest/compare/client-1.3.0...client-1.4.0)

- Added new configuration section: `ConnectionSettings` and moved `UseEnvironmentConnection`, `SetApplicationNameInConnection`, and `UseJsonApplicationName` from `NpgsqlRest` to `ConnectionSettings`.
- Added connections settings for customize connection parameters environment variable names (`HostEnvVar`, `PortEnvVar`, `DatabaseEnvVar`, `UserEnvVar` and `PasswordEnvVar`). Some Docker environments have different environment variable names.
- Added connections settings `"UseEnvironmentConnectionWhenMissing": false` to be able to override connection string with environment variable names and vice versa.
- Added support for overriding configuration settings from the command line. Command line configuration has to have this format: `--key=value`. See updated help for more info.
- Configuration value `ExposeAsEndpoint` is now set to NULL (disabled) as the default configuration. This may be enabled in the development environment.
- Fix: Default configuration files `appsettings.json` and optional `appsettings.Development.json` are now loaded from the same directory as all others (current directory as opposed to the exe location dir).

## 1.3.0

### Upgrade System.Text.Json to 8.0.4

### New option: NpgsqlRest.UseEnvironmentConnection

`NpgsqlRest.UseEnvironmentConnection`:

If the connection string is not found, empty, or missing host, port or database, the connection string is created from the environment variables.

See https://www.postgresql.org/docs/current/libpq-envars.html for the list of the environment variables.

Note: Npgsql will use the environment variables by default but only for the small set of the connection string parameters like username and password (see https://www.npgsql.org/doc/connection-string-parameters.html#environment-variables).

Set this option to true to use environment variables for host, port and database as well. 

When this option is enabled and these environment variables are set, connection string doesn't have to be defined at all and it will be created from the environment variables.

### New option: NpgsqlRest.TsClient.DefaultJsonType

`NpgsqlRest.TsClient.DefaultJsonType`:

Sets the default TypeScript type for JSON types when generating the TypeScript client.

### 1.2.8

- Upgrade NpgsqlRest to 2.8.5.

### Version 1.2.7

- Upgrade NpgsqlRest to 2.8.4.

- Version from 1.2.4 to 1.2.7 is to sync with the version of the NPM package.

- Improvements in external auth:
  - The optional third parameter receives JSON with parameters received from an external provider and query string parameters supplied to the original endpoint.
  - Login command number of parameters is optional i.e. it can be either `select * from auth($1)` or `select * from auth($1,$2)` or `select * from auth($1,$2,3)` (three is max).
  - Calls to external login/auth commands are logged by the same options, same rules, same format and the same logger as all other commands.

### Version 1.2.4

- Upgrade NpgsqlRest to 2.8.2.

### Version 1.2.3

New setting `NpgsqlRest.InstanceIdRequestHeaderName`:

```jsonc
{
    //
    //...
    //
    "NpgsqlRest": {
        "InstanceIdRequestHeaderName": "X-Instance-Id"
    }
    //
    //...
    //
}
```

The `NpgsqlRest.InstanceIdRequestHeaderName` setting allows you to specify the header name that will be used to identify the instance of the application. Header value will be used as the unique running instance ID.

This is useful when you have multiple instances of the application running behind a load balancer, and you want to identify which instance is handling the request.

The default is `null`` (not used).

### Version 1.2.2

New setting `NpgsqlRest.AuthenticationOptions.CustomParameterNameToClaimMappings`:

```jsonc 
{
    //
    //...
    //
    "NpgsqlRest": {
        "AuthenticationOptions": {
            "CustomParameterNameToClaimMappings": {
                "parameter_name": "claim_type_name"
            }
        }
    },
    //
    //...
    //
}
```

Maps a routine function name to a custom claim type. If the request parameter maps to the parameter name defined by the `NpgsqlRest.AuthenticationOptions.CustomParameterNameToClaimMappings` - it will return the matching value of the claim type, regardless of the parameter value.

If that parameter is an array, it will have all values in an array of that claim type. If it is a single value, it will have only the first value of that claim type.

### Version 1.2.1

- New option `UseHsts`: Adds middleware for using HSTS, which adds the Strict-Transport-Security header. See https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.

```jsonc
{
    //
    //...
    //
    "Ssl": {
        "HttpsRedirection": true,
        //
        // Adds middleware for using HSTS, which adds the Strict-Transport-Security header. See https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builderhstsbuilderextensions.usehsts?view=aspnetcore-2.1
        //
        "UseHsts": true
    }
    //
    //...
    //
}
```

### Version 1.2.0

- Bugfix with missing dictionary key when using external auth.

- New Config section:

```jsonc
"Config": {
    //
    // Expose current configuration to the endpoint for debugging and inspection. Note, the password in the connection string is not exposed.
    //
    "ExposeAsEndpoint": "/config",
    //
    // Add the environment variables to configuration first.
    //
    "AddEnvironmentVariables": false
}
```

- Support for the `CustomRequestHeaders` option.
- Support for the new options in TsClient.

### Version 1.1.0

- [Client application](https://vb-consulting.github.io/npgsqlrest/client/) new release with massive improvements.
- External auth logins implementation (Google, LinkedIn, GitHub)