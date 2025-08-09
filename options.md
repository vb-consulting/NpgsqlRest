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

- Type: `string`
- Default: `name`

Default claim type for user name.

### AuthenticationOptions.DefaultRoleClaimType

- Type: `string`
- Default: `roles`

Default claim type for user roles.

### AuthenticationOptions.DefaultUserIdClaimType

- Type: `string`
- Default: `id`

Default claim type for user id.

### AuthenticationOptions.HashColumnName

- Type: `string`
- Default: `"hash"`

The default column name in the data reader which will be used to read the value of the hash of the password. If this column is present, the value will be used to verify the password from the password parameter. Password parameter is the first parameter which name contains the value of PasswordParameterNameContains. If verification fails, the login will fail and the HTTP Status Code will be set to 404 Not Found.

### AuthenticationOptions.IpAddressContextKey

- Type: `string?`
- Default: `"request.ip_address"`

IP address context key that is used to set context variable for the IP address.

### AuthenticationOptions.IpAddressParameterName

- Type: `string?`
- Default: `"_ip_address"`

IP address parameter name that is used to set parameter value for the IP address. Parameter name can be original or converted and it has to be the text type.

### AuthenticationOptions.MessageColumnName

- Type: `string?`
- Default: `"message"`

The default column name in the data reader which will return a text message with the login status.

### AuthenticationOptions.ObfuscateAuthParameterLogValues

- Type: `bool`
- Default: `true`

Don't write real parameter values when logging parameters from auth endpoints and obfuscate instead. This prevents user credentials including password from ending up in application logs.

### AuthenticationOptions.PasswordHasher

- Type: `IPasswordHasher?`
- Default: `new PasswordHasher()`

Default password hasher object. Inject custom password hasher object to add default password hasher.

### AuthenticationOptions.PasswordParameterNameContains

- Type: `string`
- Default: `"pass"`

The default name of the password parameter. The first parameter which name contains this value will be used as the password parameter. This is used to verify the password from the password parameter when login endpoint returns a hash of the password (see HashColumnName).

### AuthenticationOptions.PasswordVerificationFailedCommand

- Type: `string?`
- Default: `null`

Command that is executed when the password verification fails. There are three text parameters:
- authentication scheme used for the login (if exists)
- user id used for the login (if exists)
- user name used for the login (if exists)

Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).

### AuthenticationOptions.PasswordVerificationSucceededCommand

- Type: `string?`
- Default: `null`

Command that is executed when the password verification succeeds. There are three text parameters:
- authentication scheme used for the login (if exists)
- user id used for the login (if exists)
- user name used for the login (if exists)

Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).

### AuthenticationOptions.SchemeColumnName

- Type: `string?`
- Default: `"scheme"`

The default column name in the data reader which will be used to read the value of the authentication scheme of the login process. If this column is not present in the login response the default authentication scheme is used. Return new value to use a different authentication scheme with the login endpoint.

### AuthenticationOptions.SerializeAuthEndpointsResponse

- Type: `bool`
- Default: `false`

If true, return any response from auth endpoints (login and logout) if response hasn't been written by auth handler. For cookie auth, this will return full record to response as returned by the routine. For bearer token auth, this will be ignored because bearer token auth writes its own response (with tokens). This option will also be ignored if message column is present (see MessageColumnName option).

### AuthenticationOptions.StatusColumnName

- Type: `string?`
- Default: `"status"`

The default column name in the data reader which will be used to read the value to determine the success or failure of the login operation.

- If this column is not present, the success is when the endpoint returns any records.
- If this column is not present, it must be either a boolean to indicate success or a numeric value to indicate the HTTP Status Code to return.
- If this column is present and retrieves a numeric value, that value is assigned to the HTTP Status Code and the login will authenticate only when this value is 200.

### AuthenticationOptions.UserClaimsContextKey

- Type: `string?`
- Default: `null`

When this value is set and user context is used, all user claims will be serialized to JSON value and set to the context variable with this name.

### AuthenticationOptions.UserClaimsParameterName

- Type: `string?`
- Default: `"_user_claims"`

All user claims will be serialized to JSON value and set to the parameter with this name.

### AuthenticationOptions.UserIdContextKey

- Type: `string?`
- Default: `"request.user_id"`

User id context key that is used to set context variable for the user id.

### AuthenticationOptions.UserIdParameterName

- Type: `string?`
- Default: `"_user_id"`

User id parameter name that is used to set parameter value for the user id. Parameter name can be original or converted and it has to be the text type.

### AuthenticationOptions.UserNameContextKey

- Type: `string?`
- Default: `"request.user_name"`

User name context key that is used to set context variable for the user name.

### AuthenticationOptions.UserNameParameterName

- Type: `string?`
- Default: `"_user_name"`

User name parameter name that is used to set parameter value for the user name. Parameter name can be original or converted and it has to be the text type.

### AuthenticationOptions.UserRolesContextKey

- Type: `string?`
- Default: `"request.user_roles"`

User roles context key that is used to set context variable for the user roles.

### AuthenticationOptions.UserRolesParameterName

- Type: `string?`
- Default: `"_user_roles"`

User roles parameter name that is used to set parameter value for the user roles. Parameter name can be original or converted and it has to be the text array type.

### AuthenticationOptions.UseUserContext

- Type: `bool`
- Default: `false`

Set user context to true for all requests. When this is set to true, user information (user id, user name and user roles) will be set to the context variables. You can set this individually for each request.

### AuthenticationOptions.UseUserParameters

- Type: `bool`
- Default: `false`

Set user parameters to true for all requests. When this is set to true, user information (user id, user name and user roles) will be set to the matching parameter names if available. You can set this individually for each request.

## BeforeConnectionOpen

- Type: `Action<NpgsqlConnection, RoutineEndpoint, HttpContext>?`
- Default: `null`

Callback executed immediately before connection is opened. Use this callback to adjust connection settings such as application name.

Example:

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

When cache is enabled, this value sets the interval in minutes for cache pruning (removing expired entries). Default is 1 minute.

## CommandCallbackAsync

- Type: `Func<RoutineEndpoint, NpgsqlCommand, HttpContext, Task>?`
- Default: `null`

Asynchronous callback function that will be called after every database command is created and before it has been executed. It receives a tuple parameter with routine info, created command and current HTTP context. Command instance and HTTP context offer the opportunity to execute the command and return a completely different, custom response format.

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
            await context
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

Sets the wait time (in seconds) on database commands, before terminating the attempt to execute a command and generating an error. This value when it is not null will override the `NpgsqlCommand` which is 30 seconds. Command timeout property for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## CommentsMode

- Type: `CommentsMode`
- Default: `CommentsMode.OnlyWithHttpTag`

Configure how the comment annotations will behave:
- `Ignore` will create all endpoints and ignore comment annotations.
- `ParseAll` will create all endpoints and parse comment annotations to alter the endpoint.
- `OnlyWithHttpTag` (default) will only create endpoints that contain the `HTTP` tag in the comments and then parse comment annotations.

## ConnectionString

- Type: `string?`
- Default: `null`

The connection string to the database. This is the optional value if the `DataSource` option is set.

## ConnectionStrings

- Type: `IDictionary<string, string>?`
- Default: `null`

Dictionary of connection strings. The key is the connection name and the value is the connection string. This option is used when the RoutineEndpoint has a connection name defined. This allows the middleware to use different connection strings for different routines. For example, some routines might use the primary database connection string, while others might use a read-only connection string from the replica servers.

## CustomRequestHeaders

- Type: `Dictionary<string, StringValues>`
- Default: `[]`

Custom request headers dictionary that will be added to NpgsqlRest requests. 
Note: these values are added to the request headers dictionary before they are sent as a context or parameter to the PostgreSQL routine and as such not visible to the browser debugger.

## DataSource

- Type: `NpgsqlDataSource?`
- Default: `null`

The data source object that will be used to create a connection to the database. If this option is set, the connection string will be ignored.

## DefaultHttpMethod

- Type: `Method?`
- Default: `null`

When not null, forces a method type for all created endpoints. Method types are `GET`, `PUT`, `POST`, `DELETE`, `HEAD`, `OPTIONS`, `TRACE`, `PATCH` or `CONNECT`. When this value is null (default), the method type is always `GET` when the routine volatility option is not volatile or the routine name starts with, `get_`, contains `_get_` or ends with `_get` (case insensitive). Otherwise, it is `POST`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## DefaultRequestParamType

- Type: `RequestParamType?`
- Default: `null`

When not null, sets the request parameter position (request parameter types) for all created endpoints. Values are `QueryString` (parameters are sent using query string) or `BodyJson` (parameters are sent using JSON request body). When this value is null (default), request parameter type is `QueryString` for all `GET` and `DELETE` endpoints, otherwise, request parameter type is `BodyJson`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## DefaultRoutineCache

- Type: `IRoutineCache`
- Default: `new RoutineCache()`

Default routine cache object. Inject custom cache object to override default cache.

## EndpointCreateHandlers

- Type: `IEnumerable<IEndpointCreateHandler>`
- Default: `Array.Empty<IEndpointCreateHandler>()`

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

Callback function that is executed just after the new endpoint is created. Set the RoutineEndpoint to null to disable endpoint.

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

Callback, if defined will be executed after all endpoints are created and receive an array of routine info and endpoint info tuples `(Routine routine, RoutineEndpoint endpoint)`. Used mostly for code generation.

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

List of names to be excluded or `null` to ignore this parameter.

## ExcludeSchemas

- Type: `string[]?`
- Default: `null`

List of schema names to be excluded or `null` to ignore this parameter.

## IncludeNames

- Type: `string[]?`
- Default: `null`

List of names to be included or `null` to ignore this parameter.

## IncludeSchemas

- Type: `string[]?`
- Default: `null`

List of schema names to be included or `null` to ignore this parameter.

## LogAnnotationSetInfo

- Type: `bool`
- Default: `true`

When this value is true, all changes in the endpoint properties that are set from the comment annotations will be logged as warnings.

## LogCommandParameters

- Type: `bool`
- Default: `false`

Set this option to true to include parameter values when logging commands. This only applies when `LogCommands` is true.

## LogCommands

- Type: `bool`
- Default: `false`

Set this option to true to log information for every executed command and query (including parameters and parameter values).

## LogConnectionNoticeEvents

- Type: `bool`
- Default: `true`

When this value is true, all connection events are logged (depending on the level). This is usually triggered by the PostgreSQL [`RAISE` statements](https://www.postgresql.org/docs/current/plpgsql-errors-and-messages.html). Set to false to disable logging these events.

## LogConnectionNoticeEventsMode

- Type: `PostgresConnectionNoticeLoggingMode`
- Default: `PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage`

MessageOnly - Log only connection messages.
FirstStackFrameAndMessage - Log first stack frame and the message.
FullStackAndMessage - Log full stack trace and message.

## LogEndpointCreatedInfo

- Type: `bool`
- Default: `true`

Log endpoint created events.

## Logger

- Type: `ILogger?`
- Default: `null`

Set this option to provide a custom logger implementation. The default `null` value will cause middleware to create a default logger named `NpgsqlRest` from the logger factory in the service collection.

## LoggerName

- Type: `string?`
- Default: `null`

Change the logger name with this option.

## NameConverter

- Type: `Func<string?, string?>`
- Default: `DefaultNameConverter.ConvertToCamelCase`

Custom function callback that receives names from PostgreSQL (parameter names, column names, etc), and is expected to return the same or new name. It offers an opportunity to convert names based on certain conventions. The default converter converts snake case names into camel case names.

## NameNotSimilarTo

- Type: `string?`
- Default: `null`

Filter names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## NameSimilarTo

- Type: `string?`
- Default: `null`

Filter names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## PostgreSqlErrorCodeToHttpStatusCodeMapping

- Type: `Dictionary<string, int>`
- Default: `{ { "57014", 205 }, { "P0001", 400 }, { "P0004", 400 } }`

Map PostgreSql Error Codes (see https://www.postgresql.org/docs/current/errcodes-appendix.html) to HTTP Status Codes.
Defaults:
- 57014 query_canceled → 205 Reset Content
- P0001 raise_exception → 400 Bad Request
- P0004 assert_failure → 400 Bad Request

## QueryStringNullHandling

- Type: `QueryStringNullHandling`
- Default: `QueryStringNullHandling.Ignore`

Sets the default behavior on how to pass the `NULL` values with query strings:
- `EmptyString` empty string values are interpreted as `NULL` values. This limits sending empty strings via query strings.
- `NullLiteral` literal string values `NULL` (case insensitive) are interpreted as `NULL` values.
- `Ignore` (default) `NULL` values are ignored, query string receives only empty strings.

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RefreshEndpointEnabled

- Type: `bool`
- Default: `false`

Enable refresh endpoint for Metadata.

## RefreshMethod

- Type: `string`
- Default: `"GET"`

Refresh endpoint method.

## RefreshPath

- Type: `string`
- Default: `"/api/npgsqlrest/refresh"`

Refresh endpoint path.

## RequiresAuthorization

- Type: `bool`
- Default: `false`

When set to true, it will force all created endpoints to require authorization. Authorization requirements for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RequestHeadersContextKey

- Type: `string`
- Default: `"request.headers"`

Name of the context variable that will receive the request headers when RequestHeadersMode is set to Context.

## RequestHeadersMode

- Type: `RequestHeadersMode`
- Default: `RequestHeadersMode.Ignore`

Configure how to send request headers to PostgreSQL routines execution:
- `Ignore` (default) don't send any request headers to routines.
- `Context` sets a context variable for the current session `context.headers` containing JSON string with current request headers. This executes `set_config('context.headers', headers, false)` before any routine executions.
- `Parameter` sends request headers to the routine parameter defined with the `RequestHeadersParameterName` option. Parameter with this name must exist, must be one of the JSON or text types and must have the default value defined.

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RequestHeadersParameterName

- Type: `string`
- Default: `"headers"`

Sets a parameter name that will receive a request headers JSON when the `Parameter` value is used in `RequestHeadersMode` options. A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## ReturnNpgsqlExceptionMessage

- Type: `bool`
- Default: `true`

Set to true to return message from NpgsqlException on response body. Default is true.

## RoutineSources

- Type: `List<IRoutineSource>`
- Default: `[new RoutineSource()]`

Routine sources default list.

## SchemaNotSimilarTo

- Type: `string?`
- Default: `null`

Filter schema names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## SchemaSimilarTo

- Type: `string?`
- Default: `null`

Filter schema names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## ServiceProviderMode

- Type: `ServiceProviderObject`
- Default: `ServiceProviderObject.None`

Set to NpgsqlDataSource or NpgsqlConnection to use the connection objects from the service provider. Default is None.

## SourcesCreated

- Type: `Action<List<IRoutineSource>>`
- Default: `s => { }`

Action callback executed after routine sources are created and before they are processed into endpoints. Receives a parameter with the list of `IRoutineSource` instances. This list will always contain a single item - functions and procedures source. Use this callback to modify the routine source list and add new sources from plugins.

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
- Default: `TextResponseNullHandling.EmptyString`

Sets the default behavior of plain text responses when the execution returns the `NULL` value from the database:
- `EmptyString` (default) returns an empty string response with status code 200 OK.
- `NullLiteral` returns a string literal `NULL` with the status code 200 OK.
- `NoContent` returns status code 204 NO CONTENT.

This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## UploadOptions

- Type: `NpgsqlRestUploadOptions`
- Default: `new NpgsqlRestUploadOptions()`

Default Upload Options for the NpgsqlRest middleware:

### UploadOptions.DefaultUploadHandler

- Type: `string`
- Default: `"large_object"`

Default upload handler name. This value is used when the upload handlers are not specified.

### UploadOptions.DefaultUploadHandlerOptions

- Type: `UploadHandlerOptions`
- Default: `new UploadHandlerOptions()`

Default upload handler options. Set this option to null to disable upload handlers or use this to modify upload handler options.

### UploadOptions.DefaultUploadMetadataContextKey

- Type: `string`
- Default: `"request.upload_metadata"`

Name of the default upload metadata context key. This context key will be automatically assigned to context with the upload metadata JSON string when the upload is completed if UseDefaultUploadMetadataContextKey is set to true.

### UploadOptions.DefaultUploadMetadataParameterName

- Type: `string`
- Default: `"_upload_metadata"`

Name of the default upload metadata parameter. This parameter will be automatically assigned with the upload metadata JSON string when the upload is completed if UseDefaultUploadMetadataParameter is set to true.

### UploadOptions.Enabled

- Type: `bool`
- Default: `true`

Enables upload functionality.

### UploadOptions.LogUploadEvent

- Type: `bool`
- Default: `true`

When true, logs upload events.

### UploadOptions.UploadHandlers

- Type: `Dictionary<string, Func<ILogger?, IUploadHandler>>?`
- Default: `null`

Upload handlers dictionary map. When the endpoint has set Upload to true, this dictionary will be used to find the upload handlers for the current request. Handler will be located by the key values from the endpoint UploadHandlers string array property if set or by the default upload handler (DefaultUploadHandler option).

- Set this option to null to use default upload handler from the UploadHandlerOptions property.
- Set this option to empty dictionary to disable upload handlers.
- Set this option to a dictionary with one or more upload handlers to enable your own custom upload handlers.

### UploadOptions.UseDefaultUploadMetadataContextKey

- Type: `bool`
- Default: `false`

Gets or sets a value indicating whether the default upload metadata context key should be used.

### UploadOptions.UseDefaultUploadMetadataParameter

- Type: `bool`
- Default: `false`

Gets or sets a value indicating whether the default upload metadata parameter should be used.

## UrlPathBuilder

- Type: `Func<Routine, NpgsqlRestOptions, string>`
- Default: `DefaultUrlBuilder.CreateUrl`

Custom function delegate that receives routine and options parameters and returns constructed URL path string for routine. Default the default URL builder that transforms snake case names to kebab case names.

## UrlPathPrefix

- Type: `string?`
- Default: `"/api"`

The URL prefix string for every URL created by the default URL builder or `null` to ignore the URL prefix.

## ValidateParameters

- Type: `Action<ParameterValidationValues>?`
- Default: `null`

Custom parameter validation method. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.

## ValidateParametersAsync

- Type: `Func<ParameterValidationValues, Task>?`
- Default: `null`

Custom parameter validation method, asynchronous version. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.