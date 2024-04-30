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

## Installation

Install `npgsqlrest` using npm:

```console
npm install npgsqlrest --save-dev
```
