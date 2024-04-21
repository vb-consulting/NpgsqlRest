# npgsqlrest

![npm version](https://badge.fury.io/js/npgsqlrest.svg)
![build-test-publish](https://github.com/vb-consulting/NpgsqlRest/workflows/build-test-publish/badge.svg)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Stars](https://img.shields.io/github/stars/vb-consulting/NpgsqlRest?style=social)
![GitHub Forks](https://img.shields.io/github/forks/vb-consulting/NpgsqlRest?style=social)

## Description

The `npgsqlrest` is an NPM distribution of AOT (ahead-of-time) native client build of the `NpgsqlRest` standalone client web application.

- Currently, only **Windows-64** and **Linux-64** builds are supported.
- The source code for this build can be found on this location: [NpgsqlRestTestWebApi](https://github.com/vb-consulting/NpgsqlRest/tree/master/NpgsqlRestTestWebApi).
- Executable files are distributed from the [release download page for the latest version](https://github.com/vb-consulting/NpgsqlRest/releases).
- NPM post-install script will download the appropriate build for the target OS (Windows-64v or Linux-64, sorry Mac bros) and the [default configuration file](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRestTestWebApi/appsettings.json).
- Executable will be available through NPX interface after install:

```console
vbilopav@DESKTOP-O3A6QK2:~/npgsqlrest-npm-test$ npx npgsqlrest --help
Usages
1: npgsqlrest-[os]
2: npgsqlrest-[os] [path to one or more configuration file(s)]
3: npgsqlrest-[os] [-v | --version | -h | --help]

Where
npgsqlrest-[os]  is executable for the specific OS (like npgsqlrest-win64 or npgsqlrest-linux64)
1:               run executable with default configuration files: appsettings.json (required) and appsettings.Development.json (optional).
2:               run executable with optional configuration files from argument list.
3:               show this screen.

Versions
Build                1.0.0.0
Npgsql               2.7.0.0
NpgsqlRest.HttpFiles 1.0.2.0
NpgsqlRest.TsClient  1.6.0.0

vbilopav@DESKTOP-O3A6QK2:~/npgsqlrest-npm-test$
```

- The command expects a list of configuration files in the argument list.
- If no argument is provided, the command will try to load `appsettings.json` from the current location and optionally `appsettings.Development.json`
- When using multiple configuration files, the later configuration will override values from the previous one.
- The [default configuration file](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRestTestWebApi/appsettings.json) can be located in `node_modules` -> `/node_modules/npgsqlrest/appsettings.json`.
- The default configuration will try to connect the following database: `"Host=127.0.0.1;Port=5432;Database=test;Username=postgres;Password=postgres"`
- Recommended use: 
1) Copy the default configuration into the project root and adjust it to project needs.
2) - OR - use the default configuration and override it with project configuration from the root.

Example, of new override config:

```json
{
  "ApplicationName": "MyProject",
  "EnvironmentName": "Production",
  "Urls": "http://localhost:5001",

  "ConnectionStrings": {
    "Default": "Host=127.0.0.1;Port=5432;Database=test;Username=postgres;Password=postgres"
  },

  "Auth": {
    "CookieAuth": true,
    "BearerTokenAuth": true
  },

  "Log": {
    "ToConsole": true,
    "ToFile": true
  },

  "StaticFiles": {
    "Enabled": true
  },

  "NpgsqlRest": {
    "HttpFileOptions": {
      "Enabled": true
    },

    "TsClient": {
      "Enabled": true
    },

    "CrudSource": {
      "Enabled": false
    }
  }
}
```

Command: 

```console
vbilopav@DESKTOP-O3A6QK2:~/npgsqlrest-npm-test$ npx npgsqlrest ./node_modules/npgsqlrest/appsettings.json project-config.json
[15:04:49.409 INF] ----> Starting with configuration(s): ["EnvironmentVariablesConfigurationProvider", "JsonConfigurationProvider for 'appsettings.json' (Required)", "JsonConfigurationProvider for 'project-config.json' (Required)"] [Program]
[15:04:49.409 INF] Using Cookie Authentication with scheme Cookies. Cookie expires in 14 days. [Program]
[15:04:49.410 INF] Using Bearer Token Authentication with scheme BearerToken. Token expires in 1 hours and refresh token expires in 14 days. [Program]
[15:04:49.410 INF] Using connection: Host=127.0.0.1;Port=5432;Database=test;Username=postgres;Application Name=MyProject [Program]
[15:04:49.412 INF] Serving static files from /home/vbilopav/npgsqlrest-npm-test/wwwroot [Program]
[15:04:49.574 INF] Created endpoint POST /api/case-return-long-table1 [NpgsqlRest]
[15:04:49.574 INF] Created HTTP file: /home/vbilopav/npgsqlrest-npm-test/test_public.http [NpgsqlRest.HttpFiles]
[15:04:49.582 INF] Started in 00:00:00.1752748 [Program]
[15:04:49.582 INF] Listening on ["http://localhost:5001"] [Program]
```

## Features

- Automatic **generation of the HTTP REST endpoints** from PostgreSQL functions, procedures, tables or views.
- **Customization** of endpoints with comment annotations. You can easily configure any endpoint by adding comment annotation labels to [PostgreSQL Comments](https://www.postgresql.org/docs/current/sql-comment.html). 
  - **High performance** with or without native AOT, up to 6 times higher throughput than similar solutions.
- Authentication out of the box, encrypted cookies, or, bearer token or both.
- Logging to console or rolling files (Serilog implementation) with fine tuning.
- Serving static files.
- CORS configuration.
- HTTP file automatic generartion.
- TypeScript client automatic generation.
- And more.

## Installation

Install `npgsqlrest` using npm:

```console
npm install npgsqlrest --save-dev
```