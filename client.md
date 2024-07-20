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
npgsqlrest                               Run with the default configuration files: appsettings.json (required) and
                                         appsettings.Development.json (optional).
npgsqlrest [files...]                    Run with the custom configuration files. All configuration files are required.
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