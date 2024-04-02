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

Authentication type used with the Login endpoints to set the authentication type for the new `ClaimsIdentity` created by the login.

This value must be set to non-null when using login endpoints, otherwise, the following error will raise: `SignInAsync when principal.Identity.IsAuthenticated is false is not allowed when AuthenticationOptions.RequireAuthenticatedSignIn is true.`

If the value is not set and the login endpoint is present, it will automatically get the database name from the connection string.

### AuthenticationOptions.DefaultNameClaimType

Claim type value used to retrieve the user name. 

The user name is exposed as the default name with the `Name` property on the user identity by searching claims collection with this claim type.

The default is the Active Directory Federation Services Claim Type Name property with value [`http://schemas.microsoft.com/ws/2008/06/identity/claims/name`(https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes.name?view=net-8.0#system-security-claims-claimtypes-name)

### AuthenticationOptions.DefaultRoleClaimType

Claim type value used to retrieve the roles collection for the roles-based security. 

The role key is used in the `bool IsInRole(string role)` method to search claims to determine does the current user identity belongs to roles.

The default is the Active Directory Federation Services Claim Type Role property with value [`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`(https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes.role?view=net-8.0#system-security-claims-claimtypes-role)

### AuthenticationOptions.SchemaColumnName

The default column name to in the data reader which will be used to read the value of the authentication scheme of the login process.

If this column is not present in the login response the default authentication scheme is used. Return new value to use a different authentication scheme with the login endpoint.

### AuthenticationOptions.StatusColumnName

The default column name to in the data reader which will be used to read the value to determine the success or failure of the login operation.

- If this column is not present, the success is when the endpoint returns any records.
- If this column is not present, it must be either a boolean to indicate success or a numeric value to indicate the HTTP Status Code to return.
- If this column is present and retrieves a numeric value, that value is assigned to the HTTP Status Code and the login will authenticate only when this value is 200.

### AuthenticationOptions.UseActiveDirectoryFederationServicesClaimTypes

Any columns retrieved from the reader during login, that don't have a name in `StatusColumnName` or `SchemeColumnName` will be used to create a new identity  `Claim`.

Column name will be interpreted as the claim type and the associated reader value for that column will be the claim value.

When this value is set to true (default) column name will try to match the constant name in the [ClaimTypes class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0) to retrieve the value.

For example, column name `NameIdentifier` or `name_identifier` (when transformed by the default name transformer) will match the key `NameIdentifier` which translates to this: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier

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

## ConnectionString

- Type: `string?`
- Default: `null`

The connection string to the database. This is the optional value if the `ConnectionFromServiceProvider` option is set to true. 

Note: the connection string must run as a super user or have select permissions on `information_schema` and `pg_catalog` system tables. If the `ConnectionFromServiceProvider` option is set to false and `ConnectionString` is null, the middleware will raise an `ArgumentException` error.

## SchemaSimilarTo

- Type: `string?`
- Default: `null`

Filter schema names by using a [`SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

## SchemaNotSimilarTo

- Type: `string?`
- Default: `null`

Filter schema names by using a [`NOT SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

## IncludeSchemas

- Type: `string[]?`
- Default: `null`

List of schema names to be included or null to ignore this parameter.

## ExcludeSchemas

- Type: `string[]?`
- Default: `null`

List of schema names to be excluded or null to ignore this parameter.

## NameSimilarTo

- Type: `string?`
- Default: `null`

Filter names by using a [`SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

Names are PostgreSQL object names like function or table, depending on the source.

## NameNotSimilarTo

- Type: `string?`
- Default: `null`

Filter names by using a [`NOT SIMILAR TO`](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) operator with this value, or set it to null to ignore this parameter.

Names are PostgreSQL object names like function or table, depending on the source.

## IncludeNames

- Type: `string[]?`
- Default: `null`

List of names to be included or null to ignore this parameter. Names are PostgreSQL object names like function or table, depending on the source.

## ExcludeNames

- Type: `string[]?`
- Default: `null`

List of names to be excluded or null to ignore this parameter. Names are PostgreSQL object names like function or table, depending on the source.

## UrlPathPrefix

- Type: `string?`
- Default: `"/api"`

The URL prefix string for every URL created by the default URL builder or null to ignore the URL prefix.

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

## ConnectionFromServiceProvider

- Type: `bool`
- Default: `false`

Use the `NpgsqlConnection` database connection from the service provider. 

If this option is true, middleware will attempt to require `NpgsqlConnection` from the services collection, which means it needs to be configured. This option provides an opportunity to implement custom database connection creation. If it is false, a new `NpgsqlConnection` will be created using the `ConnectionString` property. 

If this option is false and `ConnectionString` is `null`, the middleware will raise an `ArgumentException` error.

## EndpointCreated

- Type: `Func<NpgsqlRest.Routine, NpgsqlRest.RoutineEndpoint, NpgsqlRest.RoutineEndpoint?>?`
- Default: `null`

A callback function that, if defined, is executed just after the new endpoint is created. Receives routine into and new endpoint info as parameters and it is expected to return the same endpoint or null. 

It offers an opportunity to modify the endpoint based on custom logic or disable endpoints by returning null based on some custom logic.

Examples:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // always skip public routines
    EndpointCreated = (routine, endpoint) => routine.Schema == "public" ? null : endpoint
});
```

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // force all endpoints to have POST method
    EndpointCreated = (routine, endpoint) =>
    {
        endpoint.Method = Method.POST;
        return endpoint;
    }
});
```

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    // All public schema always require authorization
    EndpointCreated = (routine, endpoint) =>
    {
        if (routine.Schema == "public")
        {
            endpoint.RequiresAuthorization = true;
        }
        return endpoint;
    }
});
```

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

## RequiresAuthorization

- Type: `bool`
- Default: `false`

When set to true, it will force all created endpoints to require authorization. 

Authorization requirements for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

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

## LogEndpointCreatedInfo

- Type: `bool`
- Default: `true`

When this value is true, all created endpoint events will be logged as information with method and path. Set to false to disable logging this information.

Example log entry with default Microsoft logger:

```console
info: NpgsqlRest[0]
      Created endpoint POST /api/hello-world
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

## LogCommands

- Type: `bool`
- Default: `false`

Set this option to true to log information for every executed command and query (including parameters and parameter values).

When this option is true, the following log will be produced:

```console
info: NpgsqlRest[0] -- POST http://localhost:5000/api/return-text
      select public.return_text($1)
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

## CommandTimeout

- Type: `int?`
- Default: `null`

Sets the wait time (in seconds) on database commands, before terminating the attempt to execute a command and generating an error. This value when it is not null will override the `NpgsqlCommand` which is 30 seconds. 

Command timeout property for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

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

## CommentsMode

- Type: `CommentsMode`
- Default: `ParseAll`

Configure how the comment annotations will behave: 
- `Ignore` will create all endpoints and ignore comment annotations. 
- `ParseAll` (default) will create all endpoints and parse comment annotations to alter the endpoint.
- `OnlyWithHttpTag` will only create endpoints that contain the `HTTP` tag in the comments and then parse comment annotations.

## RequestHeadersMode

- Type: `RequestHeadersMode`
- Default: `Ignore`

Configure how to send request headers to PostgreSQL routines execution: 
- `Ignore`: (default) don't send any request headers to routines. 
- `Context`: sets a context variable for the current session `context.headers` containing JSON string with current request headers. This executes `set_config('context.headers', headers, false)` before any routine executions.
- `Parameter`: sends request headers to the routine parameter defined with the `RequestHeadersParameterName` option. A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RequestHeadersParameterName

- Type: `string`
- Default: `"headers"`

Sets a parameter name that will receive a request headers JSON when the `Parameter` value is used in `RequestHeadersMode` options. 

A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. 

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## EndpointsCreated

- Type: `Action<(Routine routine, RoutineEndpoint endpoint)[]>?`
- Default: `null`

Callback, if defined, will be executed after all endpoints are created. It receives an array of routine info and endpoint info tuples `(Routine routine, RoutineEndpoint endpoint)`. Used mostly for code generation.

Example:

```csharp
static void WriteFile(Routine routine, RoutineEndpoint endpoint)
{
    // write file here
}

app.UseNpgsqlRest(new NpgsqlRestOptions
{
    EndpointsCreated = endpoints => 
    {
        foreach(var (routine, endpoint) in endpoints)
        {
            WriteFile(routine, endpoint);
        }
    }
});
```

## CommandCallbackAsync

- Type: `Func<(Routine routine, NpgsqlCommand command, HttpContext context), Task>?`
- Default: `null`

Asynchronous callback function that, if defined, will be called after every database command is created and before it has been executed. 

It receives a tuple parameter with routine info, a newly created command and the current HTTP context. Command instance and HTTP context offer the opportunity to execute the command and return a completely different, custom response format.

If the current HTTP context is modified in any shape or form, it will be returned immediately, otherwise, it will fall back to a default behavior.

Example of returning a custom format in CSV rather than JSON:

```csharp
static async Task CommandCallbackAsync((Routine routine, NpgsqlCommand command, HttpContext context) p)
{
    if (p.routine.Name == "get_csv_data")
    {
        p.context.Response.ContentType = "text/csv";
        await using var reader = await p.command.ExecuteReaderAsync();
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

## QueryStringNullHandling

- Type: `QueryStringNullHandling`
- Default: `Ignore`

Sets the default behavior on how to pass the `NULL` values with query strings: 
- `EmptyString` empty string values are interpreted as `NULL` values. This limits sending empty strings via query strings. 
- `NullLiteral` literal string values `NULL` (case insensitive) are interpreted as `NULL` values. 
- `Ignore` (default) `NULL` values are ignored, query string receives only empty strings. 

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

