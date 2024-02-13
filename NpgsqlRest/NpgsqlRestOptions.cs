using Npgsql;
using NpgsqlRest.Defaults;

namespace NpgsqlRest;

/// <summary>
/// Options for the NpgsqlRest middleware.
/// </summary>
public class NpgsqlRestOptions(
    string? connectionString,
    string? customRoutineCommand = null,
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
    int? commandTimeout = null,
    bool logParameterMismatchWarnings = true,
    Method? defaultHttpMethod = null,
    RequestParamType? defaultRequestParamType = null,
    Action<ParameterValidationValues>? validateParameters = null,
    Func<ParameterValidationValues, Task>? validateParametersAsync = null,
    CommentsMode commentsMode = CommentsMode.ParseAll,
    RequestHeadersMode requestHeadersMode = RequestHeadersMode.Ignore,
    string requestHeadersParameterName = "headers",
    Action<(Routine routine, RoutineEndpoint endpoint)[]>? endpointsCreated = null,
    Func<(Routine routine, NpgsqlCommand command, HttpContext context), Task>? commandCallbackAsync = null,
    IEnumerable<IEndpointCreateHandler>? endpointCreateHandlers = null,
    IList<IRoutineSource>? routineSources = null)
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
    /// The connection string to the database. 
    /// Note: must run as superuser or have select permissions on information_schema.routines, information_schema.parameters, pg_catalog.pg_proc, pg_catalog.pg_description, pg_catalog.pg_namespace
    /// </summary>
    public string? ConnectionString { get; set; } = connectionString;
    /// <summary>
    /// When not null, this is a command to use to get the routines.
    /// Note: If you need to replace a default query from RoutineQuery.cs module, for example with security definer function, set this property to a custom command.
    /// </summary>
    public string? CustomRoutineCommand { get; set; } = customRoutineCommand;
    /// <summary>
    /// Filter schema names similar to this parameters or null for all schemas.
    /// </summary>
    public string? SchemaSimilarTo { get; set; } = schemaSimilarTo;
    /// <summary>
    /// Filter schema names not similar to this parameters or null for all schemas.
    /// </summary>
    public string? SchemaNotSimilarTo { get; set; } = schemaNotSimilarTo;
    /// <summary>
    /// List of schema names to be included.
    /// </summary>
    public string[]? IncludeSchemas { get; set; } = includeSchemas;
    /// <summary>
    /// List of schema names to be excluded.
    /// </summary>
    public string[]? ExcludeSchemas { get; set; } = excludeSchemas;
    /// <summary>
    /// Filter routine names similar to this parameters or null for all routines.
    /// </summary>
    public string? NameSimilarTo { get; set; } = nameSimilarTo;
    /// <summary>
    /// Filter routine names not similar to this parameters or null for all routines.
    /// </summary>
    public string? NameNotSimilarTo { get; set; } = nameNotSimilarTo;
    /// <summary>
    /// List of routine names to be included.
    /// </summary>
    public string[]? IncludeNames { get; set; } = includeNames;
    /// <summary>
    /// List of routine names to be excluded.
    /// </summary>
    public string[]? ExcludeNames { get; set; } = excludeNames;
    /// <summary>
    /// Url prefix for every url created by the default url builder.
    /// </summary>
    public string? UrlPathPrefix { get; set; } = urlPathPrefix;
    /// <summary>
    /// A custom function delegate that returns a string that will be used as the url path for routine from the first parameter.
    /// </summary>
    public Func<Routine, NpgsqlRestOptions, string> UrlPathBuilder { get; set; } = urlPathBuilder ?? DefaultUrlBuilder.CreateUrl;
    /// <summary>
    /// Set to true to get the PostgreSQL connection from the service provider. Otherwise, it will be created from the connection string property.
    /// </summary>
    public bool ConnectionFromServiceProvider { get; set; } = connectionFromServiceProvider;
    /// <summary>
    /// Callback, if not null, will be called after endpoint meta data is created.
    /// Use this to do custom configuration over routine endpoints. 
    /// Return null to disable this endpoint.
    /// </summary>
    public Func<Routine, RoutineEndpoint, RoutineEndpoint?>? EndpointCreated { get; set; } = endpointCreated;
    /// <summary>
    /// Method that converts names for parameters and return fields. 
    /// By default it is a lower camel case.
    /// Use NameConverter = name => name to preserve original names.
    /// </summary>
    public Func<string?, string?> NameConverter { get; set; } = nameConverter ?? DefaultNameConverter.ConvertToCamelCase;
    /// <summary>
    /// Set to true to require authorization for all endpoints.
    /// </summary>
    public bool RequiresAuthorization { get; set;  } = requiresAuthorization;
    /// <summary>
    /// Use this logger instead of the default logger.
    /// </summary>
    public ILogger? Logger { get; set; } = logger;
    /// <summary>
    /// Default logger name. Set to null to use default logger name which is NpgsqlRest (default namespace).
    /// </summary>
    public string? LoggerName { get; set; } = loggerName;
    /// <summary>
    /// Log endpoint created events.
    /// </summary>
    public bool LogEndpointCreatedInfo { get; set; } = logEndpointCreatedInfo;
    /// <summary>
    /// Log annotation set events. When endpoint properties are set from comment annotations.
    /// </summary>
    public bool LogAnnotationSetInfo { get; set; } = logAnnotationSetInfo;
    /// <summary>
    /// Set to true to log connection notice events.
    /// </summary>
    public bool LogConnectionNoticeEvents { get; set; } = logConnectionNoticeEvents;
    /// <summary>
    /// Log commands executed on PostgreSQL.
    /// </summary>
    public bool LogCommands { get; set; } = logCommands;
    /// <summary>
    /// Sets the wait time (in seconds) before terminating the attempt  to execute a command and generating an error.
    /// Default value is 30 seconds.
    /// </summary>
    public int? CommandTimeout { get; set; } = commandTimeout;
    /// <summary>
    /// Set to true to log parameter mismatch warnings. These mismatches occur regularly when using functions with parameter overloads with different types.
    /// </summary>
    public bool LogParameterMismatchWarnings { get; set; } = logParameterMismatchWarnings;
    /// <summary>
    /// Default HTTP method for all endpoints. NULL is default behavior: 
    /// 
    /// The endpoint is always GET if volatility option is STABLE or IMMUTABLE or the routine name either:
    /// - Starts with `get_` (case insensitive).
    /// - Ends with `_get` (case insensitive).
    /// - Contains `_get_` (case insensitive).
    /// 
    /// Otherwise, the endpoint is POST (VOLATILE and doesn't contain `get`). 
    /// 
    /// </summary>
    public Method? DefaultHttpMethod { get; set; } = defaultHttpMethod;
    /// <summary>
    /// Default parameter position - Query String or JSON Body.
    /// NULL is default behavior: if endpoint is not POST, use Query String, otherwise JSON Body.
    /// </summary>
    public RequestParamType? DefaultRequestParamType { get; set; } = defaultRequestParamType;
    /// <summary>
    /// Parameters validation function callback. Set the HttpContext response status or start writing response body to cancel the request.
    /// </summary>
    public Action<ParameterValidationValues>? ValidateParameters { get; set; } = validateParameters;
    /// <summary>
    /// Parameters validation function async callback. Set the HttpContext response status or start writing response body to cancel the request.
    /// </summary>
    public Func<ParameterValidationValues, Task>? ValidateParametersAsync { get; set; } = validateParametersAsync;
    /// <summary>
    /// Configure how to parse comments:
    /// Ignore: Routine comments are ignored.
    /// ParseAll: Creates all endpoints and parses comments for to configure endpoint meta data (default).
    /// OnlyWithHttpTag: Creates only endpoints from routines containing a comment with HTTP tag and and configures endpoint meta data.
    /// </summary>
    public CommentsMode CommentsMode { get; set; } = commentsMode;
    /// <summary>
    /// Configure how to send request headers to PostgreSQL:
    /// Ignore: Ignore request headers, don't send them to PostgreSQL (default).
    /// AsContextConfig: Send all request headers as json object to PostgreSQL by executing set_config('context.headers', headers, false) before routine call.
    /// AsDefaultParameter: Send all request headers as json object to PostgreSQL as default routine parameter with name set by RequestHeadersParameterName option.
    /// </summary>
    public RequestHeadersMode RequestHeadersMode { get; set; } = requestHeadersMode;
    /// <summary>
    /// The name of the default routine parameter (text or json) to send request headers to PostgreSQL (parsed or unparsed).
    /// This is only used when RequestHeadersMode is set to AsDefaultParameter.
    /// </summary>
    public string RequestHeadersParameterName { get; set; } = requestHeadersParameterName;
    /// <summary>
    /// Callback, if not null, will be called after all endpoints are created.
    /// </summary>
    public Action<(Routine routine, RoutineEndpoint endpoint)[]>? EndpointsCreated { get; set; } = endpointsCreated;
    /// <summary>
    /// Command callback, if not null, will be called after every command is created and before it is executed.
    /// Setting the the HttpContext response status or start writing response body will the default command execution.
    /// </summary>
    public Func<(Routine routine, NpgsqlCommand command, HttpContext context), Task>? CommandCallbackAsync { get; set; } = commandCallbackAsync;
    /// <summary>
    /// Array of endpoint create handlers (such as HttpFiles or custom handlers)
    /// </summary>
    public IEnumerable<IEndpointCreateHandler> EndpointCreateHandlers { get; set; } = endpointCreateHandlers ?? Array.Empty<IEndpointCreateHandler>();
    /// <summary>
    /// List of routine sources to use to get the routines. Default is routines (functions and procedures) source.
    /// </summary>
    public IList<IRoutineSource> RoutineSources { get; set; } = routineSources ?? [new RoutineQuery()];
}
