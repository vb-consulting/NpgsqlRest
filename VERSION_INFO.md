```console
❯ npgsqlrest -v
Versions:
.NET                  9.0.8
Client Build          2.27.1.0
System.Text.Json      9.0.0.0
ExcelDataReader       3.7.0.0
Serilog.AspNetCore    9.0.0.0
Npgsql                9.0.3.0
NpgsqlRest            2.32.0.0
NpgsqlRest.HttpFiles  1.3.2.0
NpgsqlRest.TsClient   1.21.1.0
NpgsqlRest.CrudSource 1.3.3.0
                       
CurrentDirectory      /Users/vedranbilopavlovic/dev/github/NpgsqlRest/NpgsqlRestClient
```
```console
❯ npgsqlrest -h
Usage:
npgsqlrest                                            Run with the optional default configuration files: appsettings.json and appsettings.Development.json. If these file are not found, default configuration setting is used
                                                      (see https://github.com/NpgsqlRest/NpgsqlRest/blob/master/NpgsqlRestClient/appsettings.json).
npgsqlrest [files...]                                 Run with the custom configuration files. All configuration files are required. Any configuration values will override default values in order of appearance.
npgsqlrest [file1 -o file2...]                        Use the -o switch to mark the next configuration file as optional. The first file after the -o switch is optional.
npgsqlrest [file1 --optional file2...]                Use --optional switch to mark the next configuration file as optional. The first file after the --optional switch is optional.
Note:                                                 Values in the later file will override the values in the previous one.
                                                       
npgsqlrest [--key=value]                              Override the configuration with this key with a new value (case insensitive, use : to separate sections). 
                                                       
npgsqlrest -v, --version                              Show version information.
npgsqlrest -h, --help                                 Show command line help.
npgsqlrest hash [value]                               Hash value with default hasher and print to console.
npgsqlrest basic_auth [username] [password]           Print out basic basic auth header value in format 'Authorization: Basic base64(username:password)'.
npgsqlrest encrypt [value]                            Encrypt string using default data protection and print to console.
npgsqlrest encrypted_basic_auth [username] [password] Print out basic basic auth header value in format 'Authorization: Basic base64(username:password)' where password is encrypted with default data protection.
                                                       
                                                       
Examples:                                              
Example: use two config files                         npgsqlrest appsettings.json appsettings.Development.json
Example: second config file optional                  npgsqlrest appsettings.json -o appsettings.Development.json
Example: override ApplicationName config              npgsqlrest --applicationname=Test
Example: override Auth:CookieName config              npgsqlrest --auth:cookiename=Test
```