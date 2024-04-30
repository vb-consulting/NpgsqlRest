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

To crate a custom build follow these steps:
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
