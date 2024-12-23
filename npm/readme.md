# npgsqlrest

![npm version](https://badge.fury.io/js/npgsqlrest.svg)
![build-test-publish](https://github.com/vb-consulting/NpgsqlRest/workflows/build-test-publish/badge.svg)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

## Description

The `npgsqlrest` is an NPM distribution of the self-contained ahead-of-time (AOT) compiled to native code executables of the [NpgsqlRest Client Web App](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRestClient). 

NpgsqlRest is an Automatic REST API for PostgreSQL Database as the .NET8 Middleware. See the [GitHub Readme](https://github.com/vb-consulting/NpgsqlRest) for more info. 

NpgsqlRest Client Web App is a command line utility that runs as a configurable Kestrel web server that can:

- Create an Automatic REST API for the PostgreSQL Databases.
- Generate TypeScript Code and HTTP files for testing.
- Configure security for use the of either encrypted cookies or JWT Bearer tokens or both.
- Expose REST API endpoints for the PostgreSQL Databases as Login/Logout.
- Use external authentication providers such as Google, LinkedIn or GitHub.
- Server static content.
- Use and configure built-in Serilog structured logging.
- Configure Cross-origin resource sharing (CORS) access, SSL, Server Certificates and more, everything needed for modern Web development.

See the [default configuration file](https://vb-consulting.github.io/npgsqlrest/config/) with descriptions for more information.

## Notes Before Installation

This package will download an executable file for the target OS on installation (see the postinstall.js script) from the [GitHub release page](https://github.com/vb-consulting/NpgsqlRest/releases/).

Currently, only the Windows-64 and Linux-64 builds are supported.

The Mac OS builds are missing because I don't have a Mac machine. If someone could help me out with this I'd be grateful. 

If you try to install this package on MacOS, or any other unsupported OS, installation will report: `Unsupported OS detected: [OS Type]`.

To see how you can create your own custom build follow these instructions:

Steps:

1) Make sure that you have .NET8 SDK installed and ready.
2) Clone [NpgsqlRest repository](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRest)
3) Navigate to the [`NpgsqlRestClient` project directory](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRestClient/).
4) Make your desired customizations (or not).
5) Run publish command, for example, `dotnet publish -r win-x64 -c Release --output [target dir]`

Notes: `win-x64` is the designated target OS for the build. Adjust this parameter appropriately for the target OS. See [https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids). The project is already configured for the AOT builds, but you will need to run the publish command from the same flavor OS as the build target OS (Windows for Windows builds, Linux for Linux builds, etc).

## Installation

Install `npgsqlrest` using npm:

```console
npm install npgsqlrest --save-dev
```

## Usage

```console
$ npx npgsqlrest --help
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

- Use the `npgsqlrest-config-copy` command to copy the default config to the current directory (or, optionally, directory from the first argument).

```console
$ npx npgsqlrest-config-copy
Copied appsettings.json to /home/vbilopav/npgsqlrest-npm-test/appsettings.json
```

- Running with supplied configuration:

```console
$ npx npgsqlrest appsettings.json project-config.json 
[11:29:06.551 INF] ----> Starting with configuration(s): ["EnvironmentVariablesConfigurationProvider", "JsonConfigurationProvider for 'appsettings.json' (Required)", "JsonConfigurationProvider for 'project-config.json' (Required)"] [Program]
[11:29:06.552 INF] Using connection: Host=127.0.0.1;Port=5432;Database=test;Username=postgres;Application Name=MyProject [Program]
[11:29:06.553 INF] Using Cookie Authentication with scheme Cookies. Cookie expires in 14 days. [Program]
[11:29:06.553 INF] Using Bearer Token Authentication with scheme BearerToken. Token expires in 1 hours and refresh token expires in 14 days. [Program]
[11:29:06.560 INF] Serving static files from /home/vbilopav/npgsqlrest-npm-test/wwwroot [Program]
[11:29:07.083 INF] Created endpoint POST /api/case-return-long-table1 [NpgsqlRest]
[11:29:07.083 INF] Created HTTP file: /home/vbilopav/npgsqlrest-npm-test/test_public.http [NpgsqlRest.HttpFiles]
[11:29:07.100 INF] Started in 00:00:00.5527040 [Program]
[11:29:07.100 INF] Listening on ["http://localhost:5001"] [Program]
```

## Changelog

See the detailed change log: 
- [NpgsqlRest Changelog](https://vb-consulting.github.io/npgsqlrest/changelog/)
- [NpgsqlRest Client Changelog](https://vb-consulting.github.io/npgsqlrest/client/#changelog)

## 2.6.0

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

```console
Versions:
.NET                 8.0.8
Client Build         2.1.0.0
Serilog.AspNetCore   8.0.2.0
Npgsql               8.0.3.0
NpgsqlRest           2.11.0.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.10.0.0
```

## 2.0.0

```console
Versions:
.NET                 8.0.7
Client Build         2.0.0.0
Serilog.AspNetCore   8.0.2.0
Npgsql               8.0.3.0
NpgsqlRest           2.10.0.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.9.1.0
```

## 1.5.0

```console
Versions:
Client Build         1.5.0.0
Npgsql               8.0.3.0
NpgsqlRest           2.9.0.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.9.1.0
```

## 1.4.0

See the [full diff here](https://github.com/vb-consulting/NpgsqlRest/compare/client-1.3.0...client-1.4.0)

- Added new configuration section: `ConnectionSettings` and moved `UseEnvironmentConnection`, `SetApplicationNameInConnection`, and `UseJsonApplicationName` from `NpgsqlRest` to `ConnectionSettings`.
- Added connections settings for customize connection parameters environment variable names (`HostEnvVar`, `PortEnvVar`, `DatabaseEnvVar`, `UserEnvVar` and `PasswordEnvVar`). Some Docker environments have different environment variable names.
- Added connections settings `"UseEnvironmentConnectionWhenMissing": false` to be able to override connection string with environment variable names and vice versa.
- Added support for overriding configuration settings from the command line. Command line configuration has to have this format: `--key=value`. See updated help for more info.
- Configuration value `ExposeAsEndpoint` is now set to NULL (disabled) as the default configuration. This may be enabled in the development environment.
- Fix: Default configuration files `appsettings.json` and optional `appsettings.Development.json` are now loaded from the same directory as all others (current directory as opposed to the exe location dir).

```console
Versions:
Client Build         1.4.0.0
Npgsql               8.0.3.0
NpgsqlRest           2.8.5.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.9.0.0
```

## 1.3.0

```console
Versions:
Client Build         1.3.0.0
Npgsql               8.0.3.0
NpgsqlRest           2.8.5.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.9.0.0
```

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

```console
Versions:
Client Build         1.2.8.0
Npgsql               8.0.3.0
NpgsqlRest           2.8.5.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.8.1.0
```

### 1.2.7

```console
Versions:
Client Build         1.2.7.0
NpgsqlRest           2.8.4.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.8.1.0
```

### 1.2.6

```console
Versions:
Client Build         1.2.5.0
NpgsqlRest           2.8.3.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.8.1.0
```

### 1.2.5

```console
Versions:
Client Build         1.2.4.0
NpgsqlRest           2.8.2.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.8.0.0
```

### 1.2.4

```console
Versions:
Client Build         1.2.3.0
NpgsqlRest           2.8.1.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.7.0.0
```

### 1.2.3

```console
Versions:
Client Build         1.2.2.0
NpgsqlRest           2.8.1.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.7.0.0
```

### 1.2.2

```console
Versions:
Client Build         1.2.1.0
NpgsqlRest           2.8.1.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.7.0.0
```

### 1.2.1

Fix readme

### 1.2.0

```console
Versions:
Client Build         1.2.0.0
NpgsqlRest           2.8.0.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.7.0.0
```

### 1.1.8

Changed the download target from `./node_modules/npgsqlrest/.bin/` to shared bin: `./node_modules/.bin/`.

The reason is that when using the `./node_modules/npgsqlrest/.bin/` directory, I have to use the node spawn process wrapper which slows down the startup time. When the executable is in the `./node_modules/.bin/` it can be invoked directly which is an extremely fast, almost instant startup (a couple of milliseconds).

But now, I have to use the uninstall script too, to ensure the proper cleanup on the install.

### 1.1.7

Update readme.

### 1.1.6
### 1.1.5
### 1.1.4
### 1.1.3
### 1.1.2

Fixing the issue with the local .bin directory.

### 1.1.1

- Move the download bin directory from share `/node_modules/.bin/` to package local `/node_modules/npgsqlrest/.bin/`
- Added copy default configuration command that, well, copies the default configuration `npx npgsqlrest-config-copy [optional dir]`

### 1.1.0

New build versions:

```console
Client Build         1.1.0.0
NpgsqlRest           2.7.1.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.6.0.0
```