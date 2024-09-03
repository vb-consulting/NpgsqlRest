using Microsoft.Extensions.Primitives;
using Npgsql;
using NpgsqlRest.Defaults;

namespace NpgsqlRest;

/// <summary>
/// Options for the NpgsqlRest middleware.
/// </summary>
public class NpgsqlRestOptions(
    string? connectionString,
    string? schemaSimilarTo = null,
    string? schemaNotSimilarTo = null,
    string[]? includeSchemas = null,
    string[]? excludeSchemas = null,
    string? nameSimilarTo = null,
    string? nameNotSimilarTo = null,
    string[]? includeNames = null,
    string[]? excludeNames = null,
    string? urlPathPrefix = "/api",
    Func<Routine, NpgsqlRestOptions, string>? urlPathBuilder = null,
    bool connectionFromServiceProvider = false,
    Func<Routine, RoutineEndpoint, RoutineEndpoint?>? endpointCreated = null,
    Func<string?, string?>? nameConverter = null,
    bool requiresAuthorization = false,
    ILogger? logger = null,
    string? loggerName = null,
    bool logEndpointCreatedInfo = true,
    bool logAnnotationSetInfo = true,
    bool logConnectionNoticeEvents = true,
    bool logCommands = false,
    bool logCommandParameters = false,
    int? commandTimeout = null,
    Method? defaultHttpMethod = null,
    RequestParamType? defaultRequestParamType = null,
    Action<ParameterValidationValues>? validateParameters = null,
    Func<ParameterValidationValues, Task>? validateParametersAsync = null,
    CommentsMode commentsMode = CommentsMode.OnlyWithHttpTag,
    RequestHeadersMode requestHeadersMode = RequestHeadersMode.Ignore,
    string requestHeadersParameterName = "headers",
    Action<(Routine routine, RoutineEndpoint endpoint)[]>? endpointsCreated = null,
    Func<(Routine routine, NpgsqlCommand command, HttpContext context), Task>? commandCallbackAsync = null,
    IEnumerable<IEndpointCreateHandler>? endpointCreateHandlers = null,
    Action<List<IRoutineSource>>? sourcesCreated = null,
    TextResponseNullHandling textResponseNullHandling = TextResponseNullHandling.EmptyString,
    QueryStringNullHandling queryStringNullHandling = QueryStringNullHandling.Ignore,
    ulong bufferRows = 25,
    NpgsqlRestAuthenticationOptions? authenticationOptions = null,
    bool returnNpgsqlExceptionMessage = true,
    Dictionary<string, int>? postgreSqlErrorCodeToHttpStatusCodeMapping = null,
    Action<NpgsqlConnection, Routine, RoutineEndpoint, HttpContext>? beforeConnectionOpen = null,
    Dictionary<string, StringValues>? customRequestHeaders = null)
{

    /// <summary>
    /// Options for the NpgsqlRest middleware.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    public NpgsqlRestOptions(string? connectionString) : this(connectionString, null)
    {
    }

    /// <summary>
    /// Options for the NpgsqlRest middleware.
    /// Connection string is set to null: 
    /// It either has to be set trough ConnectionString property or ConnectionFromServiceProvider has to be set to true.
    /// </summary>
    public NpgsqlRestOptions() : this(null)
    {
    }

    /// <summary>
    /// The connection string to the database. This is the optional value if the `ConnectionFromServiceProvider` option is set to true. Note: the connection string must run as a super user or have select permissions on `information_schema` and `pg_catalog` system tables. If the `ConnectionFromServiceProvider` option is false and `ConnectionString` is `null`, the middleware will raise an `ArgumentException` error.
    /// </summary>
    public string? ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Filter schema names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? SchemaSimilarTo { get; set; } = schemaSimilarTo;

    /// <summary>
    /// Filter schema names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? SchemaNotSimilarTo { get; set; } = schemaNotSimilarTo;

    /// <summary>
    /// List of schema names to be included or  `null` to ignore this parameter.
    /// </summary>
    public string[]? IncludeSchemas { get; set; } = includeSchemas;

    /// <summary>
    /// List of schema names to be excluded or  `null` to ignore this parameter. 
    /// </summary>
    public string[]? ExcludeSchemas { get; set; } = excludeSchemas;

    /// <summary>
    /// Filter names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? NameSimilarTo { get; set; } = nameSimilarTo;

    /// <summary>
    /// Filter names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? NameNotSimilarTo { get; set; } = nameNotSimilarTo;

    /// <summary>
    /// List of names to be included or `null` to ignore this parameter.
    /// </summary>
    public string[]? IncludeNames { get; set; } = includeNames;

    /// <summary>
    /// List of names to be excluded or `null` to ignore this parameter.
    /// </summary>
    public string[]? ExcludeNames { get; set; } = excludeNames;

    /// <summary>
    /// The URL prefix string for every URL created by the default URL builder or `null` to ignore the URL prefix.
    /// </summary>
    public string? UrlPathPrefix { get; set; } = urlPathPrefix;

    /// <summary>
    /// Custom function delegate that receives routine and options parameters and returns constructed URL path string for routine. Default the default URL builder that transforms snake case names to kebab case names.
    /// </summary>
    public Func<Routine, NpgsqlRestOptions, string> UrlPathBuilder { get; set; } = urlPathBuilder ?? DefaultUrlBuilder.CreateUrl;

    /// <summary>
    /// Use the `NpgsqlConnection` database connection from the service provider. If this option is true, middleware will attempt to require `NpgsqlConnection` from the services collection, which means it needs to be configured. This option provides an opportunity to implement custom database connection creation. If it is false, a new `NpgsqlConnection` will be created using the `ConnectionString` property. If this option is false and `ConnectionString` is `null`, the middleware will raise an `ArgumentException` error.
    /// </summary>
    public bool ConnectionFromServiceProvider { get; set; } = connectionFromServiceProvider;

    /// <summary>
    /// Callback function that is executed just after the new endpoint is created. Receives routine into and new endpoint info as parameters and it is expected to return the same endpoint or `null`. It offers an opportunity to modify the endpoint based on custom logic or disable endpoints by returning `null` based on some custom logic. Default is `null`, which means this callback is not defined.
    /// </summary>
    public Func<Routine, RoutineEndpoint, RoutineEndpoint?>? EndpointCreated { get; set; } = endpointCreated;

    /// <summary>
    /// Custom function callback that receives names from PostgreSQL (parameter names, column names, etc), and is expected to return the same or new name. It offers an opportunity to convert names based on certain conventions. The default converter converts snake case names into camel case names.
    /// </summary>
    public Func<string?, string?> NameConverter { get; set; } = nameConverter ?? DefaultNameConverter.ConvertToCamelCase;

    /// <summary>
    /// When set to true, it will force all created endpoints to require authorization. Authorization requirements for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public bool RequiresAuthorization { get; set; } = requiresAuthorization;

    /// <summary>
    /// Set this option to provide a custom logger implementation. The default `null` value will cause middleware to create a default logger named `NpgsqlRest` from the logger factory in the service collection.
    /// </summary>
    public ILogger? Logger { get; set; } = logger;

    /// <summary>
    ///  Change the logger name with this option. 
    /// </summary>
    public string? LoggerName { get; set; } = loggerName;

    /// <summary>
    /// Log endpoint created events.
    /// </summary>
    public bool LogEndpointCreatedInfo { get; set; } = logEndpointCreatedInfo;

    /// <summary>
    /// When this value is true, all changes in the endpoint properties that are set from the comment annotations will be logged as warnings.
    /// </summary>
    public bool LogAnnotationSetInfo { get; set; } = logAnnotationSetInfo;

    /// <summary>
    /// When this value is true, all connection events are logged (depending on the level). This is usually triggered by the PostgreSQL [`RAISE` statements](https://www.postgresql.org/docs/current/plpgsql-errors-and-messages.html). Set to false to disable logging these events.
    /// </summary>
    public bool LogConnectionNoticeEvents { get; set; } = logConnectionNoticeEvents;

    /// <summary>
    /// Set this option to true to log information for every executed command and query (including parameters and parameter values).
    /// </summary>
    public bool LogCommands { get; set; } = logCommands;

    /// <summary>
    /// Set this option to true to include parameter values when logging commands. This only applies when `LogCommands` is true.
    /// </summary>
    public bool LogCommandParameters { get; set; } = logCommandParameters;

    /// <summary>
    /// Sets the wait time (in seconds) on database commands, before terminating the attempt to execute a command and generating an error. This value when it is not null will override the `NpgsqlCommand` which is 30 seconds. Command timeout property for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public int? CommandTimeout { get; set; } = commandTimeout;

    /// <summary>
    /// When not null, forces a method type for all created endpoints. Method types are `GET`, `PUT`, `POST`, `DELETE`, `HEAD`, `OPTIONS`, `TRACE`, `PATCH` or `CONNECT`. When this value is null (default), the method type is always `GET` when the routine volatility option is not volatile or the routine name starts with, `get_`, contains `_get_` or ends with `_get` (case insensitive). Otherwise, it is `POST`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public Method? DefaultHttpMethod { get; set; } = defaultHttpMethod;

    /// <summary>
    /// When not null, sets the request parameter position (request parameter types) for all created endpoints. Values are `QueryString` (parameters are sent using query string) or `BodyJson` (paremeters are sent using JSON request body). When this value is null (default), request parameter type is `QueryString` for all `GET` and `DELETE` endpoints, otherwise, request parameter type is `BodyJson`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public RequestParamType? DefaultRequestParamType { get; set; } = defaultRequestParamType;

    /// <summary>
    /// Custom parameter validation method. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.
    /// </summary>
    public Action<ParameterValidationValues>? ValidateParameters { get; set; } = validateParameters;

    /// <summary>
    /// Custom parameter validation method, asynchrounous version. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.
    /// </summary>
    public Func<ParameterValidationValues, Task>? ValidateParametersAsync { get; set; } = validateParametersAsync;

    /// <summary>
    /// Configure how the comment annotations will behave. `Ignore` will create all endpoints and ignore comment annotations. `ParseAll` (default) will create all endpoints and parse comment annotations to alter the endpoint. `OnlyWithHttpTag` will only create endpoints that contain the `HTTP` tag in the comments and then parse comment annotations.
    /// </summary>
    public CommentsMode CommentsMode { get; set; } = commentsMode;

    /// <summary>
    /// Configure how to send request headers to PostgreSQL routines execution. `Ignore` (default) don't send any request headers to routines. `Context` sets a context variable for the current session `context.headers` containing JSON string with current request headers. This executes `set_config('context.headers', headers, false)` before any routine executions. `Parameter` sends request headers to the routine parameter defined with the `RequestHeadersParameterName` option. Paremeter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public RequestHeadersMode RequestHeadersMode { get; set; } = requestHeadersMode;

    /// <summary>
    /// Sets a parameter name that will receive a request headers JSON when the `Parameter` value is used in `RequestHeadersMode` options. A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public string RequestHeadersParameterName { get; set; } = requestHeadersParameterName;

    /// <summary>
    /// Callback, if defined will be executed after all endpoints are created and receive an array of routine info and endpoint info tuples `(Routine routine, RoutineEndpoint endpoint)`. Used mostly for code generation.
    /// </summary>
    public Action<(Routine routine, RoutineEndpoint endpoint)[]>? EndpointsCreated { get; set; } = endpointsCreated;

    /// <summary>
    /// Asynchronous callback function that will be called after every database command is created and before it has been executed. It receives a tuple parameter with routine info, created command and current HTTP context. Command instance and HTTP context offer the opportunity to execute the command and return a completely different, custom response format.
    /// </summary>
    public Func<(Routine routine, NpgsqlCommand command, HttpContext context), Task>? CommandCallbackAsync { get; set; } = commandCallbackAsync;

    /// <summary>
    /// List of `IEndpointCreateHandler` type handlers executed sequentially after endpoints are created. Used to add the different code generation plugins.
    /// </summary>
    public IEnumerable<IEndpointCreateHandler> EndpointCreateHandlers { get; set; } = endpointCreateHandlers ?? Array.Empty<IEndpointCreateHandler>();

    /// <summary>
    /// Action callback executed after routine sources are created and before they are processed into endpoints. Receives a parameter with the list of `IRoutineSource` instances. This list will always contain a single item - functions and procedures source. Use this callback to modify the routine source list and add new sources from plugins.
    /// </summary>
    public Action<List<IRoutineSource>> SourcesCreated { get; set; } = sourcesCreated ?? (s => { });

    /// <summary>
    /// Sets the default behavior of plain text responses when the execution returns the `NULL` value from the database. `EmptyString` (default) returns an empty string response with status code 200 OK. `NullLiteral` returns a string literal `NULL` with the status code 200 OK. `NoContent` returns status code 204 NO CONTENT. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public TextResponseNullHandling TextResponseNullHandling { get; set; } = textResponseNullHandling;

    /// <summary>
    /// Sets the default behavior on how to pass the `NULL` values with query strings. `EmptyString` empty string values are interpreted as `NULL` values. This limits sending empty strings via query strings. `NullLiteral` literal string values `NULL` (case insensitive) are interpreted as `NULL` values. `Ignore` (default) `NULL` values are ignored, query string receives only empty strings. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public QueryStringNullHandling QueryStringNullHandling { get; set; } = queryStringNullHandling;

    /// <summary>
    /// The number of rows to buffer in the string builder before sending the response. The default is 25.
    /// This applies to rows in JSON object array when returning records from the database.
    /// Set to 0 to disable buffering and write a response for each row.
    /// Set to 1 to buffer the entire array (all rows).
    /// Notes: 
    /// - Disabling buffering can have a slight negative impact on performance since buffering is far less expensive than writing to the response stream.
    /// - Setting higher values can have a negative impact on memory usage, especially when returning large datasets.
    /// </summary>
    public ulong BufferRows { get; set; } = bufferRows;

    /// <summary>
    /// Default Authentication Options
    /// </summary>
    public NpgsqlRestAuthenticationOptions AuthenticationOptions { get; set; } = authenticationOptions ?? new();

    /// <summary>
    /// Set to true to return message from NpgsqlException on response body. Default is true.
    /// </summary>
    public bool ReturnNpgsqlExceptionMessage { get; set; } = returnNpgsqlExceptionMessage;

    /// <summary>
    /// Map PostgreSql Error Codes (see https://www.postgresql.org/docs/current/errcodes-appendix.html) to HTTP Status Codes
    /// Default is 57014 query_canceled to 205 Reset Content.
    /// </summary>
    public Dictionary<string, int> PostgreSqlErrorCodeToHttpStatusCodeMapping { get; set; } = postgreSqlErrorCodeToHttpStatusCodeMapping ?? new()
    {
        { "57014", 205 }, //query_canceled -> 205 Reset Content
    };

    /// <summary>
    /// Callback executed immediately before connection is opened. Use this callback to adjust connection settings such as application name.
    /// </summary>
    public Action<NpgsqlConnection, Routine, RoutineEndpoint, HttpContext>? BeforeConnectionOpen { get; set; } = beforeConnectionOpen;

    /// <summary>
    /// Custom request headers dictionary that will be added to NpgsqlRest requests. 
    /// Note: these values are added to the request headers dictionary before they are sent as a context or parameter to the PostgreSQL routine and as such not visible to the browser debugger.
    /// </summary>
    public Dictionary<string, StringValues> CustomRequestHeaders { get; set; } = customRequestHeaders ?? [];

    internal List<IRoutineSource> RoutineSources { get; set; } = [new RoutineSource()];
}
