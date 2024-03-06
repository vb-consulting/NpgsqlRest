# Npgsql Options

Options are passed as a parameter of the `UseNpgsqlRest` extension:

```csharp
var app = builder.Build();
app.UseNpgsqlRest(new NpgsqlRestOptions());
app.Run();
```

Here is the full list of available options in the last version:

## ConnectionString

- Type: `string?`
- Default: `null`
- Description: The connection string to the database. This is the optional value if the `ConnectionFromServiceProvider` option is set to true. Note: the connection string must run as a super user or have select permissions on `information_schema` and `pg_catalog` system tables. If the `ConnectionFromServiceProvider` option is false and `ConnectionString` is `null`, the middleware will raise an `ArgumentException` error.

## SchemaSimilarTo

- Type: `string?`
- Default: `null`
- Description: Filter schema names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## SchemaNotSimilarTo

- Type: `string?`
- Default: `null`
- Description: Filter schema names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## IncludeSchemas

- Type: `string[]?`
- Default: `null`
- Description: List of schema names to be included or `null` to ignore this parameter.

## ExcludeSchemas

- Type: `string[]?`
- Default: `null`
- Description: List of schema names to be excluded or `null` to ignore this parameter.

## NameSimilarTo

- Type: `string?`
- Default: `null`
- Description: Filter names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## NameNotSimilarTo

- Type: `string?`
- Default: `null`
- Description: Filter names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.

## IncludeNames

- Type: `string[]?`
- Default: `null`
- Description: List of names to be included or `null` to ignore this parameter.

## ExcludeNames

- Type: `string[]?`
- Default: `null`
- Description: List of names to be excluded or `null` to ignore this parameter.

## UrlPathPrefix

- Type: `string?`
- Default: `"/api"`
- Description: The URL prefix string for every URL created by the default URL builder or `null` to ignore the URL prefix.

## UrlPathBuilder

- Type: `Func<Routine, NpgsqlRestOptions, string>`
- Default: `DefaultUrlBuilder.CreateUrl`
- Description: Custom function delegate that receives routine and options parameters and returns constructed URL path string for routine. Default the default URL builder that transforms snake case names to kebab case names.

## ConnectionFromServiceProvider

- Type: `bool`
- Default: `false`
- Description: Use the `NpgsqlConnection` database connection from the service provider. If this option is true, middleware will attempt to require `NpgsqlConnection` from the services collection, which means it needs to be configured. This option provides an opportunity to implement custom database connection creation. If it is false, a new `NpgsqlConnection` will be created using the `ConnectionString` property. If this option is false and `ConnectionString` is `null`, the middleware will raise an `ArgumentException` error.

## EndpointCreated

- Type: `Func<Routine, RoutineEndpoint, RoutineEndpoint?>?`
- Default: `null`
- Description: A callback function that is executed just after the new endpoint is created. Receives routine into and new endpoint info as parameters and it is expected to return the same endpoint or `null`. It offers an opportunity to modify the endpoint based on custom logic or disable endpoints by returning `null` based on some custom logic. The default is `null``, which means this callback is not defined.

## NameConverter

- Type: `Func<string?, string?>`
- Default: `DefaultNameConverter.ConvertToCamelCase`
- Description: Custom function callback that receives names from PostgreSQL (parameter names, column names, etc), and is expected to return the same or new name. It offers an opportunity to convert names based on certain conventions. The default converter converts snake case names into camel case names.

## RequiresAuthorization

- Type: `bool`
- Default: `false`
- Description: When set to true, it will force all created endpoints to require authorization. Authorization requirements for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## Logger

- Type: `Microsoft.Extensions.Logging.ILogger?`
- Default: `null`
- Description: Set this option to provide a custom logger implementation. The default `null` value will cause middleware to create a default logger named `NpgsqlRest` from the logger factory in the service collection.

## LoggerName

- Type: `string?`
- Default: `null`
- Description: Change the logger name with this option.

## LogEndpointCreatedInfo

- Type: `bool`
- Default: `true`
- Description: When this value is true, all created endpoint events will be logged as information with method and path. Set to false to disable logging this information.

## LogConnectionNoticeEvents

- Type: `bool`
- Default: `true`
- Description: When this value is true, all connection events are logged (depending on the level). This is usually triggered by the PostgreSQL [`RAISE` statements](https://www.postgresql.org/docs/current/plpgsql-errors-and-messages.html). Set to false to disable logging these events.

## LogCommands

- Type: `bool`
- Default: `false`
- Description: Set this option to true to log information for every executed command and query (including parameters and parameter values).

## CommandTimeout

- Type: `int?`
- Default: `null`
- Description: Sets the wait time (in seconds) on database commands, before terminating the attempt to execute a command and generating an error. This value when it is not null will override the `NpgsqlCommand` which is 30 seconds. Command timeout property for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## DefaultHttpMethod

- Type: `Method?`
- Default: `null`
- Description: When not null, force a method type for all created endpoints. Method types are `GET`, `PUT`, `POST`, `DELETE`, `HEAD`, `OPTIONS`, `TRACE`, `PATCH` or `CONNECT`. When this value is null (default), the method type is always `GET` when the routine volatility option is not volatile or the routine name starts with, `get_`, contains `_get_` or ends with `_get` (case insensitive). Otherwise, it is `POST`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## DefaultRequestParamType

- Type: `RequestParamType?`
- Default: `null`
- Description: When not null, sets the request parameter position (request parameter types) for all created endpoints. Values are `QueryString` (parameters are sent using query string) or `BodyJson` (parameters are sent using JSON request body). When this value is null (default), the request parameter type is `QueryString` for all `GET` and `DELETE` endpoints, otherwise, the request parameter type is `BodyJson`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## ValidateParameters

- Type: `Action<ParameterValidationValues>?`
- Default: `null`
- Description: Custom parameter validation method. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.

## ValidateParametersAsync

- Type: `Func<ParameterValidationValues, Task>?`
- Default: `null`
- Description: Custom parameter validation method, asynchronous version. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.

## CommentsMode

- Type: `CommentsMode`
- Default: `ParseAll`
- Description: Configure how the comment annotations will behave. `Ignore` will create all endpoints and ignore comment annotations. `ParseAll` (default) will create all endpoints and parse comment annotations to alter the endpoint. `OnlyWithHttpTag` will only create endpoints that contain the `HTTP` tag in the comments and then parse comment annotations.

## RequestHeadersMode

- Type: `RequestHeadersMode`
- Default: `Ignore`
- Description: Configure how to send request headers to PostgreSQL routines execution. `Ignore` (default) don't send any request headers to routines. `Context` sets a context variable for the current session `context.headers` containing JSON string with current request headers. This executes `set_config('context.headers', headers, false)` before any routine executions. `Parameter` sends request headers to the routine parameter defined with the `RequestHeadersParameterName` option. A paremeter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## RequestHeadersParameterName

- Type: `string`
- Default: `"headers"`
- Description: Sets a parameter name that will receive a request headers JSON when the `Parameter` value is used in `RequestHeadersMode` options. A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## EndpointsCreated

- Type: `Action<(Routine routine, RoutineEndpoint endpoint)[]>?`
- Default: `null`
- Description: Callback, if defined will be executed after all endpoints are created and receive an array of routine info and endpoint info tuples `(Routine routine, RoutineEndpoint endpoint)`. Used mostly for code generation.

## CommandCallbackAsync

- Type: `Func<(Routine routine, NpgsqlCommand command, HttpContext context), Task>?`
- Default: `null`
- Description: Asynchronous callback function that will be called after every database command is created and before it has been executed. It receives a tuple parameter with routine info, created command and current HTTP context. Command instance and HTTP context offer the opportunity to execute the command and return a completely different, custom response format.

## EndpointCreateHandlers

- Type: `IEnumerable<IEndpointCreateHandler>`
- Default: `[]`
- Description: List of `IEndpointCreateHandler` type handlers executed sequentially after endpoints are created. Used to add the different code generation plugins.

## SourcesCreated

- Type: `Action<List<IRoutineSource>>`
- Default: `source => {}`
- Description: Action callback executed after routine sources are created and before they are processed into endpoints. Receives a parameter with the list of `IRoutineSource` instances. This list will always contain a single item - functions and procedures source. Use this callback to modify the routine source list and add new sources from plugins.

## TextResponseNullHandling

- Type: `TextResponseNullHandling`
- Default: `EmptyString`
- Description: Sets the default behavior of plain text responses when the execution returns the `NULL` value from the database. `EmptyString` (default) returns an empty string response with status code 200 OK. `NullLiteral` returns a string literal `NULL` with the status code 200 OK. `NoContent` returns status code 204 NO CONTENT. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.

## QueryStringNullHandling

- Type: `QueryStringNullHandling`
- Default: `Ignore`
- Description: Sets the default behavior on how to pass the `NULL` values with query strings. `EmptyString` empty string values are interpreted as `NULL` values. This limits sending empty strings via query strings. `NullLiteral` literal string values `NULL` (case insensitive) are interpreted as `NULL` values. `Ignore` (default) `NULL` values are ignored, query string receives only empty strings. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
