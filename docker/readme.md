# NpgsqlRect Docker 

To use the NpgsqlRect Docker, pull it from the registry:

```console
$ docker pull vbilopav/npgsqlrest
```

# Features

- Create an Automatic REST API for the PostgreSQL Databases.
- Generate TypeScript Code and HTTP files for testing.
- Configure security for use the of either encrypted cookies or JWT Bearer tokens or both.
- Expose REST API endpoints for the PostgreSQL Databases as Login/Logout.
- Use external authentication providers such as Google, LinkedIn or GitHub.
- Server static content.
- Use and configure built-in Serilog structured logging.
- Configure Cross-origin resource sharing (CORS) access, SSL, Server Certificates and more, everything needed for modern Web development.

See the [default configuration file](https://vb-consulting.github.io/npgsqlrest/config/) with descriptions for more information.

# Running Containers

```console
$ docker run --rm -i -t vbilopav/npgsqlrest --version
```

Outputs:

```
Versions:
.NET                  9.0.7
Client Build          2.25.0.0
System.Text.Json      9.0.0.0
ExcelDataReader       3.7.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.3.0
NpgsqlRest            2.30.0.0
NpgsqlRest.HttpFiles  1.3.0.0
NpgsqlRest.TsClient   1.21.0.0
NpgsqlRest.CrudSource 1.3.2.0

CurrentDirectory     /app

```

```console
$ docker run --rm -i -t vbilopav/npgsqlrest --help
```

Outputs:

```
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

Running in the current directory with two configuration files and exposing port 5000:

```
docker run --rm -i -t -v $(pwd):/home --expose 5000 vbilopav/npgsqlrest /home/appsettings.json /home/appsettings.Development.json
```

Note: To learn more about exposing ports and binding volumes, see the Docker documentation. To see more details on the NpgslRest configuration, see the [default configuration file](https://vb-consulting.github.io/npgsqlrest/config/).

