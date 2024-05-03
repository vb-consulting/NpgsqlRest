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
npgsqlrest                             Run with the default configuration files: appsettings.json (required) and appsettings.Development.json (optional).
npgsqlrest [files...]                  Run with the custom configuration files. All configuration files are required.
npgsqlrest [file1 -o file2...]         Use the -o switch to mark the next configuration file as optional. The first file after the -o switch is optional.
npgsqlrest [file1 --optional file2...] Use --optional switch to mark the next configuration file as optional. The first file after the --optional switch is optional.

npgsqlrest -v, --version               Show version information.
npgsqlrest -h, --help                  Show command line help.

Note:                                  Values in the later file will override the values in the previous one.

Example:                               npgsqlrest appsettings.json appsettings.Development.json
Example:                               npgsqlrest appsettings.json -o appsettings.Development.json
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

See the detailed change log here: [NpgsqlRest Changelog](https://vb-consulting.github.io/npgsqlrest/changelog/)

### 1.1.8

Changed the download target from `./node_modules/npgsqlrest/.bin/` to shared bin: `./node_modules/.bin/`.

The reason is that when using the `./node_modules/npgsqlrest/.bin/` directory, I have to use the node spawn process wrapper which slows down the startup time. When the executable is in the `./node_modules/.bin/` it can be invoked directly which is an extremely fast, almost instant startup (a couple of milliseconds).

But now, I have to use the uninstall script too, to ensure the proper cleanup on the install.

### 1.2.0

```console
Versions:
Client Build         1.2.0.0
Npgsql               2.8.0.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.7.0.0
```

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
Npgsql               2.7.1.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.6.0.0
```