# Npgsql Options

Options are passed as a parameter of the `UseNpgsqlRest` extension:

```csharp
var app = builder.Build();
app.UseNpgsqlRest(new NpgsqlRestOptions());
app.Run();
```

The full list of available options in the latest version is below.

## AuthenticationOptions

This is the group of options used for authentication:

### AuthenticationOptions.DefaultAuthenticationType

- Type: `string?`
- Default: `null`

Authentication type used with the Login endpoints to set the authentication type for the new `ClaimsIdentity` created by the login.

This value must be set to non-null when using login endpoints, otherwise, the following error will raise: `SignInAsync when principal.Identity.IsAuthenticated is false is not allowed when AuthenticationOptions.RequireAuthenticatedSignIn is true.`

If the value is not set and the login endpoint is present, it will automatically get the database name from the connection string.

### AuthenticationOptions.DefaultNameClaimType

- Type: `string?`
- Default: `"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"`

Claim type value used to retrieve the user name. 

The user name is exposed as the default name with the `Name` property on the user identity by searching claims collection with this claim type.

The default is the Active Directory Federation Services Claim Type Name property with value [`http://schemas.microsoft.com/ws/2008/06/identity/claims/name`](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes.name?view=net-8.0#system-security-claims-claimtypes-name)

### AuthenticationOptions.DefaultRoleClaimType

- Type: `string?`
- Default: `"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role"`

Claim type value used to retrieve the roles collection for the roles-based security. 

The role key is used in the `bool IsInRole(string role)` method to search claims to determine does the current user identity belongs to roles.

The default is the Active Directory Federation Services Claim Type Role property with value [`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes.role?view=net-8.0#system-security-claims-claimtypes-role)

### AuthenticationOptions.HashColumnName

- Type: `string`
- Default: `"hash"`

The default column name in the data reader that will contain password hash. If this column is present, value ill be verfified with the default hasher against password parameter.

### AuthenticationOptions.MessageColumnName

- Type: `string?`
- Default: `"message"`

This is the textual message that is returned in the response body.

Note: this message is only returned in a case when the configured authentication scheme doesn't write anything into the response body on a sign-in operation.

For example, the Cookie authentication scheme doesn't write anything into the body and this message is safely written to the response body. On the other hand, the Bearer Token schemes will always write the response body and this message will not be written to the response body.

### AuthenticationOptions.PasswordParameterNameContains

- Type: `string`
- Default: `"pass"`

When the login endpoint tries to verify supplied hash with the password parameter, this value will be used to locate the password parameter from the parameter collection. That will be the first parameter that cotnains this string in parameter name (either original or translated). 

### AuthenticationOptions.SchemeColumnName

- Type: `string?`
- Default: `"scheme"`

The default column name in the data reader will be used to read the value of the authentication scheme of the login process.

The textual value of this field will set the authentication scheme name for the sign-in operation.

This is useful when using multiple authentication schemes. Returning any value from the logout routine will cause a logout from only that scheme. So for example, if you have the Cookie scheme and the Bearer Token scheme configured, you can handle them separately for login and logout.

The default is `"scheme"`.

### AuthenticationOptions.StatusColumnName

- Type: `string?`
- Default: `"status"`

The default column name in the data reader will be used to read the value to determine the success or failure of the login operation.

This column can only be a boolean or numeric type. If it is neither boolean nor numeric, the endpoint will return status `500 InternalServerError` and you'll have to check logs.

When this field is boolean, and it is true, the login process will continue with security claims set by other fields (which usually ends up in `200 OK` if authentication is configured). If it is false, the endpoint will return `404 NotFound` and the login attempt will not continue.

When this field is numeric, and it is 200, the login process will continue with security claims set by other fields (which usually ends up in `200 OK` if authentication is configured). If it is not 200, the endpoint will return the status code the same as the value of this field, and the login attempt will not continue.

### AuthenticationOptions.UseActiveDirectoryFederationServicesClaimTypes

- Type: `bool`
- Default: `true`

By default, if the column name interpreted as the security claim type, matches one of the Active Directory Federation Services Claim Types names, it will use that AD Federation Services Claim Type URI. The table can be seen here: [ClaimTypes Class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0#fields).
  
That means that if the column name is either of these `NameIdentifier` - or the `nameidentifier` (case is ignored), or the `name_identifier` (for the camel case converted names, which is the default), the actual security claim type is `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` according to this table. 
  
If the name is not found in this table, security is the column name as-is but parsed by the default name converter. 

In this example column name `name_identifier` will be the security claim type the `nameIdentifier`. 

This behavior that uses the AD Federation Services Claim Type can be turned off with this option.

## BeforeConnectionOpen

- Type: `Action<NpgsqlConnection, RoutineEndpoint, HttpContext>?`
- Default: `null`

Callback option: `Action<NpgsqlConnection, RoutineEndpoint, HttpContext>? BeforeConnectionOpen`.

This is used to set the application name parameter (for example) without having to use the service provider. It executes before the new connection is open for the request. For example:

```csharp
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    BeforeConnectionOpen = (NpgsqlConnection connection, RoutineEndpoint endpoint, HttpContext context) =>
    {
        var username = context.User.Identity?.Name;
        connection.ConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = string.Concat(
                    "{\"app\":\"",
                    builder.Environment.ApplicationName,
                    username is null ? "\",\"user\":null}" : string.Concat("\",\"user\":\"", username, "\"}"))
        }.ConnectionString;
    }
}
```

## BufferRows

- Type: `ulong`
- Default: `25`

The number of rows to buffer in the string builder before sending the response. The default is 25.
This applies to rows in JSON object array when returning records from the database.

Set to 0 to disable buffering and write a response for each row.

Set to 1 to buffer the entire array (all rows).

Notes: 
- Disabling buffering can have a slight negative impact on performance since buffering is far less expensive than writing to the response stream.
- Setting higher values can have a negative impact on memory usage, especially when returning large datasets.

## CachePruneIntervalMin

- Type: `int`
- Default: `1`

Defines interval in minutes when they system ill attempt to remove expired cache items.

## CommandCallbackAsync

- Type: `Func<RoutineEndpoint, NpgsqlCommand, HttpContext, Task>?`
- Default: `null`

Asynchronous callback function that, if defined, will be called after every database command is created and before it has been executed. 

It receives a tuple parameter with routine info, a newly created command and the current HTTP context. Command instance and HTTP context offer the opportunity to execute the command and return a completely different, custom response format.

If the current HTTP context is modified in any shape or form, it will be returned immediately, otherwise, it will fall back to a default behavior.

Example of returning a custom format in CSV rather than JSON:

```csharp
static async Task CommandCallbackAsync(RoutineEndpoint endpoint, NpgsqlCommand command, HttpContext context)
{
    if (endpoint.Routine.Name == "get_csv_data")
    {
        context.Response.ContentType = "text/csv";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            await p
                .context
                .Response
                .WriteAsync($"{reader[0]},{reader[1]},{reader.GetDateTime(2):s},{reader.GetBoolean(3).ToString().ToLowerInvariant()}\n");
        }
    }
}

app.UseNpgsqlRest(new NpgsqlRestOptions
{
    CommandCallbackAsync = CommandCallbackAsync
});
```

## CommandTimeout

- Type: `int?`
- Default: `null`

Sets the wait time (in seconds) on database commands, before terminating the attempt to execute a command and generating an error. This value when it is not null will override the `NpgsqlCommand` which is 30 seconds. 

Command timeout property for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## CommentsMode

- Type: `CommentsMode`
- Default: `OnlyWithHttpTag`

Configure how the comment annotations will behave: 
- `Ignore` will create all endpoints and ignore comment annotations. 
- `ParseAll` (default) will create all endpoints and parse comment annotations to alter the endpoint.
- `OnlyWithHttpTag` will only create endpoints that contain the `HTTP` tag in the comments and then parse comment annotations.

## ConnectionString

- Type: `string?`
- Default: `null`

The connection string to the database. This is the optional value if the `DataSource` is set. 

## ConnectionStrings

- Type: `IDictionary<string, string>?`
- Default: `null`

Set this dictionary to enable the use of alternate connections to some routines. Routines that have the `ConnectionName` string property set to the existing key in this dictionary will use this connection.

Note: these connections are not used to build metadata. Therefore, the same routine must also exist on a primary connection to be able to build metadata for execution.

## CustomRequestHeaders

- Type: `Dictionary<string, StringValues>`
- Default: `[]`

Custom request headers dictionary that will be added to NpgsqlRest requests. 

Note: these values are added to the request headers dictionary before they are sent as a context or parameter to the PostgreSQL routine and as such not visible to the browser debugger.

## DataSource

- Type: `NpgsqlDataSource?`
- Default: `null`

Default `NpgsqlDataSource` defines a data source from which to create connection objects. If set, `ConnectionString` option is ignored.

## DefaultHttpMethod

- Type: `Method?`
- Default: `null`

When not null, force a method type for all created endpoints. Method types are `GET`, `PUT`, `POST`, `DELETE`, `HEAD`, `OPTIONS`, `TRACE`, `PATCH` or `CONNECT`.  When this value is null (default), default logic will apply to determine individual endpoint methods, depending on the routine source.

For example:

- For function and procedures, routines with volatility option `VOLATILE` are always `POST`, unless the name starts with, `get_`, contains `_get_` or ends with `_get`, the the method is `GET`. If the volatility option is not `VOLATILE`, method is always `GET`.
- For tables and views, the method depends on the type of CRUD operation. Create is `PUT`. Read is `GET`. The update is `POST`. And, delete is `DELETE`.

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## DefaultRequestParamType

- Type: `RequestParamType?`
- Default: `null`

When not null, set the request parameter position (request parameter types) for all created endpoints. Values are: 
- `QueryString`: parameters are sent using the query string. 
- `BodyJson`: parameters are sent using JSON request body. 

When this value is null (default), the request parameter type is `QueryString` for all `GET` and `DELETE` endpoints, otherwise, the request parameter type is `BodyJson`. 

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## DefaultResponseParser

- Type: `IResponseParser?`
- Default: `null`

Default response parser. The system doesn't define a default response parser. Use this property to inject one.

## DefaultRoutineCache

- Type: `IRoutineCache`
- Default: `new RoutineCache()`

Default caching mechanism. See [RoutineCache source code](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/RoutineCache.cs)

## DefaultUploadHandler

- Type: `string`
- Default: `large_object`

Default upload handler name. This value is used when upload handlers are not explicitly specified.

## DefaultUploadHandlerOptions

- Type: `UploadHandlerOptions`
- Default: `new UploadHandlerOptions()`

Default upload handler options. Set this option to null to disable upload handlers or use this to modify upload handler options.

## EndpointCreateHandlers

- Type: `IEnumerable<IEndpointCreateHandler>`
- Default: `[]`

List of `IEndpointCreateHandler` type handlers executed sequentially after endpoints are created. Used to add the different code generation plugins.

Example:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // create HTTP file and the Typescript Client from plugins
    EndpointCreateHandlers = [
        new HttpFile(), 
        new TsClient("../Frontend/src/api.ts")]
});
```

## EndpointCreated

- Type: `Action<RoutineEndpoint?>?`
- Default: `null`

A callback that, if defined, is executed just after the new endpoint is created. Receives endoint instance and it offers an opportunity to modify the endpoint based on the custom logic. To disable this endpoint, set instance to NULL.

Examples:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // always skip public routines
    EndpointCreated = endpoint =>
    {
        if (endpoint.Routine.Schema == "public")
        {
            endpoint = null;
        }
    }
});
```

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // force all endpoints to have POST method
    EndpointCreated = endpoint =>
    {
        endpoint.Method = Method.POST;
    }
});
```

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // All public schema always require authorization
    EndpointCreated = endpoint =>
    {
        if (endpoint.Routine.Schema == "public")
        {
            endpoint.RequiresAuthorization = true;
        }
    }
});
```

## EndpointsCreated

- Type: `Action<RoutineEndpoint[]>?`
- Default: `null`

Callback, if defined, will be executed after all endpoints are created. It receives an array of routine endpoints `RoutineEndpoint` objects. Used mostly for code generation.

Example:

```csharp
static void WriteFile(RoutineEndpoint endpoint)
{
    // write file here
}

app.UseNpgsqlRest(new NpgsqlRestOptions
{
    EndpointsCreated = endpoints => 
    {
        foreach(var endpoint in endpoints)
        {
            WriteFile(endpoint);
        }
    }
});
```

## ExcludeNames

- Type: `string[]?`
- Default: `null`

List of names to be excluded or null to ignore this parameter. Names are PostgreSQL object names like function or table, depending on the source.

## ExcludeSchemas

- Type: `string[]?`
- Default: `null`

List of schema names to be excluded or null to ignore this parameter.

## IncludeNames

- Type: `string[]?`
- Default: `null`

List of names to be included or null to ignore this parameter. Names are PostgreSQL object names like function or table, depending on the source.

## IncludeSchemas

- Type: `string[]?`
- Default: `null`

List of schema names to be included or null to ignore this parameter.

## LogAnnotationSetInfo

- Type: `bool`
- Default: `true`

When this value is true, all changes in the endpoint properties that are set from the comment annotations will be logged as information:

```console
info: NpgsqlRest[0]
      Function auth.get_user_details has set REQUIRED AUTHORIZATION by the comment annotation.
```

## LogCommandParameters

- Type: `bool`
- Default: `false`

Set this option to true to include parameter values when logging commands. This only applies when `LogCommands` is true.

Execution for parameter value "ABC" will produce the following log:

```console
info: NpgsqlRest[0] -- POST http://localhost:5000/api/return-text
      -- $1 text = 'ABC'
      select public.return_text($1)
```

## LogCommands

- Type: `bool`
- Default: `false`

Set this option to true to log information for every executed command and query (including parameters and parameter values).

When this option is true, the following log will be produced:

```console
info: NpgsqlRest[0] -- POST http://localhost:5000/api/return-text
      select public.return_text($1)
```

## LogConnectionNoticeEvents

- Type: `bool`
- Default: `true`

When this value is true, all connection events are logged (depending on the level). This is usually triggered by the PostgreSQL [`RAISE` statements](https://www.postgresql.org/docs/current/plpgsql-errors-and-messages.html). Set to false to disable logging these events.

Example function with RAISE statement:

```sql
create function return_text(_t text) 
returns text 
language plpgsql
as 
$$
begin
    raise info '_t = %', _t;
    return _t;
end;
$$;
```

Execution for parameter value "ABC" will produce the following log:

```console
info: NpgsqlRest[0]
      PL/pgSQL function return_text(text) line 3 at RAISE:
      _t = ABC
```

## LogConnectionNoticeEventsMode

- Type: `PostgresConnectionNoticeLoggingMode`
- Default: `LastStackAndMessage`

Describe how will PostgreSQL connection notice messages will be logged when `LogConnectionNoticeEvents` is set to true. Options are:
- `MessageOnly`: Log only notice message.
- `FirstStackFrameAndMessage` (default): the first stack frame and the message. In chained calls stack frame can be longer and obfuscate log message. This option will show only the first (starting) stack frame along with message.
- `FullStackAndMessage`: Log the enire stack frame and the notice message.

## LogEndpointCreatedInfo

- Type: `bool`
- Default: `true`

When this value is true, all created endpoint events will be logged as information with method and path. Set to false to disable logging this information.

Example log entry with default Microsoft logger:

```console
info: NpgsqlRest[0]
      Created endpoint POST /api/hello-world
```

## Logger

- Type: `Microsoft.Extensions.Logging.ILogger?`
- Default: `null`

Set this option to provide a custom logger implementation. The default null value will cause middleware to create a default logger named `NpgsqlRest` from the default logger factory in the service collection.

Example:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // use empty logger
    Logger = new EmptyLogger()
});

public class EmptyLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
}
```

## LoggerName

- Type: `string?`
- Default: `null`

Change the logger name with this option.

## NameConverter

- Type: `Func<string?, string?>`
- Default: `NpgsqlRest.Defaults.DefaultNameConverter.ConvertToCamelCase`

Custom function callback that receives names from PostgreSQL (parameter names, column names, etc), and is expected to return the same or new name. It offers an opportunity to convert names based on certain conventions. The default converter converts snake case names into camel case names.

Example:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    //
    // Use the original name for parameters and JSON field names
    //
    NameConverter = name => name
});
```

## NameNotSimilarTo

- Type: `string?`
- Default: `null`

Filter names by using a [`NOT SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

Names are PostgreSQL object names like function or table, depending on the source.

## NameSimilarTo

- Type: `string?`
- Default: `null`

Filter names by using a [`SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

Names are PostgreSQL object names like function or table, depending on the source.

## PasswordHasher

- Type: `IPasswordHasher`
- Default: `new PasswordHasher()`

Default password hasher object. Inject a custom password hasher object to add default password hashing functionality.

## PostgreSqlErrorCodeToHttpStatusCodeMapping

- Type: `Dictionary<string, int>`
- Default: `{ { "57014", 205 } }`

Dictionary setting that maps the PostgreSQL Error Codes (see the [errcodes-appendix](https://www.postgresql.org/docs/current/errcodes-appendix.html) to HTTP Status Codes. 

Default is `{ "57014", 205 }` which maps PostgreSQL `query_canceled` error to HTTP `205 Reset Content`. If the mapping doesn't exist, the standard HTTP  `500 Internal Server Error` is returned.

## QueryStringNullHandling

- Type: `QueryStringNullHandling`
- Default: `Ignore`

Sets the default behavior on how to pass the `NULL` values with query strings: 
- `EmptyString` empty string values are interpreted as `NULL` values. This limits sending empty strings via query strings. 
- `NullLiteral` literal string values `NULL` (case insensitive) are interpreted as `NULL` values. 
- `Ignore` (default) `NULL` values are ignored, query string receives only empty strings. 

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RefreshEndpointEnabled

- Type: `bool`
- Default: `false`

Enables or disables refresh endpoint. When refresh endpoint is invoked, the entire metadata for NpgsqlRest endpoints is refreshed. When metadata is refreshed, endpoint returns status 200.

## RefreshMethod

- Type: `string`
- Default: `"GET"`

## RefreshPath

- Type: `string`
- Default: `"/api/npgsqlrest/refresh"`

## RequiresAuthorization

- Type: `bool`
- Default: `false`

When set to true, it will force all created endpoints to require authorization. 

Authorization requirements for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RequestHeadersMode

- Type: `RequestHeadersMode`
- Default: `Ignore`

Configure how to send request headers to PostgreSQL routines execution: 
- `Ignore`: (default) Don't send any request headers to routines. 
- `Context`: sets a context variable for the current session `context.headers` containing JSON string with current request headers. This executes `set_config('context.headers', headers, false)` before any routine executions.
- `Parameter`: sends request headers to the routine parameter defined with the `RequestHeadersParameterName` option. A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RequestHeadersParameterName

- Type: `string`
- Default: `"headers"`

Sets a parameter name that will receive a request headers JSON when the `Parameter` value is used in `RequestHeadersMode` options. 

A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. 

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## ReturnNpgsqlExceptionMessage

- Type: `bool`
- Default: `true`

- Set to true to return message property on exception from the `NpgsqlException` object on response body. The default is true. 

## SchemaNotSimilarTo

- Type: `string?`
- Default: `null`

Filter schema names by using a [`NOT SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

## SchemaSimilarTo

- Type: `string?`
- Default: `null`

Filter schema names by using a [`SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

## ServiceProviderMode

- Type: `ServiceProviderObject`
- Default: `None`

Configure how the comment annotations will behave: 

- `None` - Connection is not provided in service provider. Connection is supplied either by ConnectionString or by DataSource option.
- `NpgsqlDataSource` - NpgsqlRest attempts to get `NpgsqlDataSource` from service provider (assuming one is provided).
- `NpgsqlConnection` - NpgsqlRest attempts to get `NpgsqlConnection` from service provider (assuming one is provided).

## SourcesCreated

- Type: `Action<List<IRoutineSource>>`
- Default: `source => {}`

Action callback that, if defined, is executed after routine sources are created and before they are processed into endpoints. 

Receives a parameter with the list of `IRoutineSource` instances. This list will always contain a single item - functions and procedures source. Use this callback to modify the routine source list and add new sources from plugins.

Example:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // add tables and views CRUD source from plugin
    SourcesCreated = sources => sources.Add(new CrudSource())
});
```

## TextResponseNullHandling

- Type: `TextResponseNullHandling`
- Default: `EmptyString`

Sets the default behavior of plain text responses when the execution returns the `NULL` value from the database: 
- `EmptyString` (default) returns an empty string response with status code 200 OK. 
- `NullLiteral` returns a string literal `NULL` with the status code 200 OK. 
- `NoContent` returns status code 204 NO CONTENT. 

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## UploadHandlers

- Type: `Dictionary<string, Func<IUploadHandler>>?`
- Default: `null`

Upload handlers dictionary map. 

When the endpoint has Upload set to true, this dictionary will be used to find the upload handlers for the current request. The handler will be located by the key values from the endpoint's UploadHandlers string array property (if set) or by the default upload handler (DefaultUploadHandler option).

- Set this option to null to use the default upload handler from the UploadHandlerOptions property.
- Set this option to an empty dictionary to disable upload handlers.
- Set this option to a dictionary with one or more upload handlers to enable your own custom upload handlers.

## UrlPathBuilder

- Type: `Func<Routine, NpgsqlRestOptions, string>`
- Default: `DefaultUrlBuilder.CreateUrl`

Custom function delegate that receives routine and options parameters and returns constructed URL path string for routine. Default the default URL builder that transforms snake case names to kebab case names.

Example:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    //
    // URL path is equal to the routine name without schema name
    //
    UrlPathBuilder = (routine, options) => routine.Name
});
```

## UrlPathPrefix

- Type: `string?`
- Default: `"/api"`

The URL prefix string for every URL created by the default URL builder or null to ignore the URL prefix.

## ValidateParameters

- Type: `Action<ParameterValidationValues>?`
- Default: `null`

Custom parameter validation method. When this callback option is not null, it will be executed for every database parameter created. 

The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.

Example:

```csharp
static void Validate(ParameterValidationValues p)
{
    if (p.Routine.Name == "my_function" && p.Parameter.Value?.ToString() == "XXX")
    {
        p.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
}

app.UseNpgsqlRest(new NpgsqlRestOptions
{
    ValidateParameters = Validate
});
```

## ValidateParametersAsync

- Type: `Func<ParameterValidationValues, Task>?`
- Default: `null`

Custom parameter validation method, the asynchronous version. When this callback option is not null, it will be executed for every database parameter created. 

The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.

Example:

```csharp
static async Task ValidateAsync(ParameterValidationValues p)
{
    if (p.Routine.Name == "my_function" && p.Parameter.Value?.ToString() == "XXX")
    {
        p.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await p.Context.Response.WriteAsync($"Paramater {p.ParamName} is not valid.");
    }
}

app.UseNpgsqlRest(new NpgsqlRestOptions
{
    ValidateParametersAsync = ValidateAsync
});
```