using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using Npgsql;
using NpgsqlRest.Defaults;
using NpgsqlRest.UploadHandlers;

namespace NpgsqlRest;

/// <summary>
/// Options for the NpgsqlRest middleware.
/// </summary>
public class NpgsqlRestOptions
{
    /// <summary>
    /// Options for the NpgsqlRest middleware with default values.
    /// </summary>
    public NpgsqlRestOptions()
    {
        // Default values are set directly in property initializers
    }

    /// <summary>
    /// Options for the NpgsqlRest middleware.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    public NpgsqlRestOptions(string connectionString)
    {
        ConnectionString = connectionString;
        DataSource = null;
    }

    /// <summary>
    /// Options for the NpgsqlRest middleware with connection string and data source.
    /// </summary>
    /// <param name="dataSource">NpgsqlDataSource instance</param>
    public NpgsqlRestOptions(NpgsqlDataSource dataSource)
    {
        ConnectionString = null;
        DataSource = dataSource;
    }

    /// <summary>
    /// The connection string to the database. This is the optional value if the `DataSource` option is set. 
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The data source object that will be used to create a connection to the database. 
    /// If this option is set, the connection string will be ignored. 
    /// </summary>
    public NpgsqlDataSource? DataSource { get; set; }

    /// <summary>
    /// Dictionary of connection strings. The key is the connection name and the value is the connection string.
    /// This option is used when the RoutineEndpoint has a connection name defined.
    /// This allows the middleware to use different connection strings for different routines.
    /// For example, some routines might use the primary database connection string, while others might use a read-only connection string from the replica servers.
    /// </summary>
    public IDictionary<string, string>? ConnectionStrings { get; set; }

    /// <summary>
    /// Filter schema names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? SchemaSimilarTo { get; set; }

    /// <summary>
    /// Filter schema names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? SchemaNotSimilarTo { get; set; }

    /// <summary>
    /// List of schema names to be included or  `null` to ignore this parameter.
    /// </summary>
    public string[]? IncludeSchemas { get; set; }

    /// <summary>
    /// List of schema names to be excluded or  `null` to ignore this parameter. 
    /// </summary>
    public string[]? ExcludeSchemas { get; set; }

    /// <summary>
    /// Filter names [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? NameSimilarTo { get; set; }

    /// <summary>
    /// Filter names NOT [similar to](https://www.postgresql.org/docs/current/functions-matching.html#FUNCTIONS-SIMILARTO-REGEXP) this parameter or `null` to ignore this parameter.
    /// </summary>
    public string? NameNotSimilarTo { get; set; }

    /// <summary>
    /// List of names to be included or `null` to ignore this parameter.
    /// </summary>
    public string[]? IncludeNames { get; set; }

    /// <summary>
    /// List of names to be excluded or `null` to ignore this parameter.
    /// </summary>
    public string[]? ExcludeNames { get; set; }

    /// <summary>
    /// The URL prefix string for every URL created by the default URL builder or `null` to ignore the URL prefix.
    /// </summary>
    public string? UrlPathPrefix { get; set; } = "/api";

    /// <summary>
    /// Custom function delegate that receives routine and options parameters and returns constructed URL path string for routine. Default is the default URL builder that transforms snake case names to kebab case names.
    /// </summary>
    public Func<Routine, NpgsqlRestOptions, string> UrlPathBuilder { get; set; } = DefaultUrlBuilder.CreateUrl;

    /// <summary>
    /// Set to NpgsqlDataSource or NpgsqlConnection to use the connection objects from the service provider. Default is None.
    /// </summary>
    public ServiceProviderObject ServiceProviderMode { get; set; } = ServiceProviderObject.None;

    /// <summary>
    /// Callback function that is executed just after the new endpoint is created. Set the RoutineEndpoint to null to disable endpoint.
    /// </summary>
    public Action<RoutineEndpoint?>? EndpointCreated { get; set; } = null;

    /// <summary>
    /// Custom function callback that receives names from PostgreSQL (parameter names, column names, etc), and is expected to return the same or new name. It offers an opportunity to convert names based on certain conventions. The default converter converts snake case names into camel case names.
    /// </summary>
    public Func<string?, string?> NameConverter { get; set; } = DefaultNameConverter.ConvertToCamelCase;

    /// <summary>
    /// When set to true, it will force all created endpoints to require authorization. Authorization requirements for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public bool RequiresAuthorization { get; set; }

    /// <summary>
    /// Set this option to provide a custom logger implementation. The default `null` value will cause middleware to create a default logger named `NpgsqlRest` from the logger factory in the service collection.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    ///  Change the logger name with this option. 
    /// </summary>
    public string? LoggerName { get; set; }

    /// <summary>
    /// Log endpoint created events.
    /// </summary>
    public bool LogEndpointCreatedInfo { get; set; } = true;

    /// <summary>
    /// When this value is true, all changes in the endpoint properties that are set from the comment annotations will be logged as warnings.
    /// </summary>
    public bool LogAnnotationSetInfo { get; set; } = true;

    /// <summary>
    /// When this value is true, all connection events are logged (depending on the level). This is usually triggered by the PostgreSQL [`RAISE` statements](https://www.postgresql.org/docs/current/plpgsql-errors-and-messages.html). Set to false to disable logging these events.
    /// </summary>
    public bool LogConnectionNoticeEvents { get; set; } = true;

    /// <summary>
    /// MessageOnly - Log only connection messages.
    /// FirstStackFrameAndMessage - Log first stack frame and the message.
    /// FullStackAndMessage - Log full stack trace and message.
    /// </summary>
    public PostgresConnectionNoticeLoggingMode LogConnectionNoticeEventsMode { get; set; } = PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage;

    /// <summary>
    /// Set this option to true to log information for every executed command and query (including parameters and parameter values).
    /// </summary>
    public bool LogCommands { get; set; }

    /// <summary>
    /// Set this option to true to include parameter values when logging commands. This only applies when `LogCommands` is true.
    /// </summary>
    public bool LogCommandParameters { get; set; }

    /// <summary>
    /// Sets the wait time (in seconds) on database commands, before terminating the attempt to execute a command and generating an error. This value when it is not null will override the `NpgsqlCommand` which is 30 seconds. Command timeout property for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// When not null, forces a method type for all created endpoints. Method types are `GET`, `PUT`, `POST`, `DELETE`, `HEAD`, `OPTIONS`, `TRACE`, `PATCH` or `CONNECT`. When this value is null (default), the method type is always `GET` when the routine volatility option is not volatile or the routine name starts with, `get_`, contains `_get_` or ends with `_get` (case insensitive). Otherwise, it is `POST`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public Method? DefaultHttpMethod { get; set; }

    /// <summary>
    /// When not null, sets the request parameter position (request parameter types) for all created endpoints. Values are `QueryString` (parameters are sent using query string) or `BodyJson` (parameters are sent using JSON request body). When this value is null (default), request parameter type is `QueryString` for all `GET` and `DELETE` endpoints, otherwise, request parameter type is `BodyJson`. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public RequestParamType? DefaultRequestParamType { get; set; }

    /// <summary>
    /// Custom parameter validation method. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.
    /// </summary>
    public Action<ParameterValidationValues>? ValidateParameters { get; set; }

    /// <summary>
    /// Custom parameter validation method, asynchronous version. When this callback option is not null, it will be executed for every database parameter created. The input structure will contain a current HTTP context that offers the opportunity to alter the response and cancel the request: If the current HTTP response reference has started or the status code is different than 200 OK, command execution will be canceled and the response will be returned.
    /// </summary>
    public Func<ParameterValidationValues, Task>? ValidateParametersAsync { get; set; }

    /// <summary>
    /// Configure how the comment annotations will behave. `Ignore` will create all endpoints and ignore comment annotations. `ParseAll` (default) will create all endpoints and parse comment annotations to alter the endpoint. `OnlyWithHttpTag` will only create endpoints that contain the `HTTP` tag in the comments and then parse comment annotations.
    /// </summary>
    public CommentsMode CommentsMode { get; set; } = CommentsMode.OnlyWithHttpTag;

    /// <summary>
    /// Configure how to send request headers to PostgreSQL routines execution. `Ignore` (default) doesn't send any request headers to routines. `Context` sets a context variable for the current session `context.headers` containing JSON string with current request headers. This executes `set_config('context.headers', headers, false)` before any routine executions. `Parameter` sends request headers to the routine parameter defined with the `RequestHeadersParameterName` option. Parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public RequestHeadersMode RequestHeadersMode { get; set; } = RequestHeadersMode.Ignore;

    /// <summary>
    /// Name of the context variable that will receive the request headers when RequestHeadersMode is set to Context.
    /// </summary>
    public string RequestHeadersContextKey { get; set; } = "request.headers";

    /// <summary>
    /// Sets a parameter name that will receive a request headers JSON when the `Parameter` value is used in `RequestHeadersMode` options. A parameter with this name must exist, must be one of the JSON or text types and must have the default value defined. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public string RequestHeadersParameterName { get; set; } = "headers";

    /// <summary>
    /// Callback, if defined will be executed after all endpoints are created and receive an array of routine info and endpoint info tuples `(Routine routine, RoutineEndpoint endpoint)`. Used mostly for code generation.
    /// </summary>
    public Action<RoutineEndpoint[]>? EndpointsCreated { get; set; }

    /// <summary>
    /// Asynchronous callback function that will be called after every database command is created and before it has been executed. It receives a tuple parameter with routine info, created command and current HTTP context. Command instance and HTTP context offer the opportunity to execute the command and return a completely different, custom response format.
    /// </summary>
    public Func<RoutineEndpoint, NpgsqlCommand, HttpContext, Task>? CommandCallbackAsync { get; set; }

    /// <summary>
    /// List of `IEndpointCreateHandler` type handlers executed sequentially after endpoints are created. Used to add the different code generation plugins.
    /// </summary>
    public IEnumerable<IEndpointCreateHandler> EndpointCreateHandlers { get; set; } = Array.Empty<IEndpointCreateHandler>();

    /// <summary>
    /// Action callback executed after routine sources are created and before they are processed into endpoints. Receives a parameter with the list of `IRoutineSource` instances. This list will always contain a single item - functions and procedures source. Use this callback to modify the routine source list and add new sources from plugins.
    /// </summary>
    public Action<List<IRoutineSource>> SourcesCreated { get; set; } = s => { };

    /// <summary>
    /// Sets the default behavior of plain text responses when the execution returns the `NULL` value from the database. `EmptyString` (default) returns an empty string response with status code 200 OK. `NullLiteral` returns a string literal `NULL` with the status code 200 OK. `NoContent` returns status code 204 NO CONTENT. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public TextResponseNullHandling TextResponseNullHandling { get; set; } = TextResponseNullHandling.EmptyString;

    /// <summary>
    /// Sets the default behavior on how to pass the `NULL` values with query strings. `EmptyString` empty string values are interpreted as `NULL` values. This limits sending empty strings via query strings. `NullLiteral` literal string values `NULL` (case insensitive) are interpreted as `NULL` values. `Ignore` (default) `NULL` values are ignored, query string receives only empty strings. This option for individual endpoints can be changed with the `EndpointCreated` function callback, or by using comment annotations.
    /// </summary>
    public QueryStringNullHandling QueryStringNullHandling { get; set; } = QueryStringNullHandling.Ignore;

    /// <summary>
    /// The number of rows to buffer in the string builder before sending the response. The default is 25.
    /// This applies to rows in JSON object array when returning records from the database.
    /// Set to 0 to disable buffering and write a response for each row.
    /// Set to 1 to buffer the entire array (all rows).
    /// Notes: 
    /// - Disabling buffering can have a slight negative impact on performance since buffering is far less expensive than writing to the response stream.
    /// - Setting higher values can have a negative impact on memory usage, especially when returning large datasets.
    /// </summary>
    public ulong BufferRows { get; set; } = 25;

    /// <summary>
    /// Default Authentication Options
    /// </summary>
    public NpgsqlRestAuthenticationOptions AuthenticationOptions { get; set; } = new();

    /// <summary>
    /// Set to true to return message from NpgsqlException on response body. Default is true.
    /// </summary>
    public bool ReturnNpgsqlExceptionMessage { get; set; } = true;

    /// <summary>
    /// Map PostgreSql Error Codes (see https://www.postgresql.org/docs/current/errcodes-appendix.html) to HTTP Status Codes
    /// Default is 57014 query_canceled to 205 Reset Content.
    /// </summary>
    public Dictionary<string, int> PostgreSqlErrorCodeToHttpStatusCodeMapping { get; set; } = new()
    {
        { "57014", 205 }, //query_canceled -> 205 Reset Content
        { "P0001", 400 }, // raise_exception -> 400 Bad Request
        { "P0004", 400 }, // assert_failure -> 400 Bad Request
    };

    /// <summary>
    /// Callback executed immediately before connection is opened. Use this callback to adjust connection settings such as application name.
    /// </summary>
    public Action<NpgsqlConnection, RoutineEndpoint, HttpContext>? BeforeConnectionOpen { get; set; }

    /// <summary>
    /// Custom request headers dictionary that will be added to NpgsqlRest requests. 
    /// Note: these values are added to the request headers dictionary before they are sent as a context or parameter to the PostgreSQL routine and as such not visible to the browser debugger.
    /// </summary>
    public Dictionary<string, StringValues> CustomRequestHeaders { get; set; } = [];

    /// <summary>
    /// Routine sources default list.
    /// </summary>
    public List<IRoutineSource> RoutineSources { get; set; } = [new RoutineSource()];

    /// <summary>
    /// Enable refresh endpoint for Metadata.
    /// </summary>
    public bool RefreshEndpointEnabled { get; set; }

    /// <summary>
    /// Refresh endpoint method.
    /// </summary>
    public string RefreshMethod { get; set; } = "GET";

    /// <summary>
    /// Refresh endpoint path.
    /// </summary>
    public string RefreshPath { get; set; } = "/api/npgsqlrest/refresh";

    /// <summary>
    /// Default routine cache object. Inject custom cache object to override default cache.
    /// </summary>
    public IRoutineCache DefaultRoutineCache { get; set; } = new RoutineCache();

    /// <summary>
    /// When cache is enabled, this value sets the interval in minutes for cache pruning (removing expired entries). Default is 1 minute.
    /// </summary>
    public int CachePruneIntervalMin { get; set; } = 1;

    /// <summary>
    /// Default response parser object. Inject custom response parser object to add default response parser.
    /// </summary>
    public IResponseParser? DefaultResponseParser { get; set; }

    /// <summary>
    /// Default Upload Options
    /// </summary>
    public NpgsqlRestUploadOptions UploadOptions { get; set; } = new();
}

/// <summary>
/// Upload options for the NpgsqlRest middleware.
/// </summary>
public class NpgsqlRestUploadOptions
{
    public bool Enabled { get; set; } = true;
    public bool LogUploadEvent { get; set; } = true;
    public bool LogUploadParameters { get; set; } = false;

    /// <summary>
    /// Default upload handler name. This value is used when the upload handlers are not specified.
    /// </summary>
    public string DefaultUploadHandler { get; set; } = "large_object";

    /// <summary>
    /// Default upload handler options. 
    /// Set this option to null to disable upload handlers or use this to modify upload handler options.
    /// </summary>
    public UploadHandlerOptions DefaultUploadHandlerOptions { get; set; } = new UploadHandlerOptions();

    /// <summary>
    /// Upload handlers dictionary map. 
    /// When the endpoint has set Upload to true, this dictionary will be used to find the upload handlers for the current request. 
    /// Handler will be located by the key values from the endpoint UploadHandlers string array property if set or by the default upload handler (DefaultUploadHandler option).
    /// Set this option to null to use default upload handler from the UploadHandlerOptions property.
    /// Set this option to empty dictionary to disable upload handlers.
    /// Set this option to a dictionary with one or more upload handlers to enable your own custom upload handlers.
    /// </summary>
    public Dictionary<string, Func<ILogger?, IUploadHandler>>? UploadHandlers { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether the default upload metadata parameter should be used.
    /// </summary>
    public bool UseDefaultUploadMetadataParameter { get; set; } = false;

    /// <summary>
    /// Name of the default upload metadata parameter. 
    /// This parameter will be automatically assigned with the upload metadata JSON string when the upload is completed if UseDefaultUploadMetadataParameter is set to true.
    /// </summary>
    public string DefaultUploadMetadataParameterName { get; set; } = "_upload_metadata";

    /// <summary>
    /// Gets or sets a value indicating whether the default upload metadata context key should be used.
    /// </summary>
    public bool UseDefaultUploadMetadataContextKey { get; set; } = false;

    /// <summary>
    /// Name of the default upload metadata context key.
    /// This context key will be automatically assigned to context with the upload metadata JSON string when the upload is completed if UseDefaultUploadMetadataContextKey is set to true.
    /// </summary>
    public string DefaultUploadMetadataContextKey { get; set; } = "request.upload_metadata";
}


/// <summary>
/// Authentication options for the NpgsqlRest middleware.
/// </summary>
public class NpgsqlRestAuthenticationOptions
{
    /// <summary>
    /// Authentication type used with the Login endpoints to set the authentication type for the new `ClaimsIdentity` created by the login.
    ///
    /// This value must be set to non-null when using login endpoints, otherwise, the following error will raise: `SignInAsync when principal.Identity.IsAuthenticated is false is not allowed when AuthenticationOptions.RequireAuthenticatedSignIn is true.`
    /// 
    /// If the value is not set and the login endpoint is present, it will automatically get the database name from the connection string.
    /// </summary>
    public string? DefaultAuthenticationType { get; set; } = null;

    /// <summary>
    /// The default column name in the data reader which will be used to read the value to determine the success or failure of the login operation.
    /// 
    /// - If this column is not present, the success is when the endpoint returns any records.
    /// - If this column is present, it must be either a boolean to indicate success or a numeric value to indicate the HTTP Status Code to return.
    /// - If this column is present and retrieves a numeric value, that value is assigned to the HTTP Status Code and the login will authenticate only when this value is 200.
    /// </summary>
    public string? StatusColumnName { get; set; } = "status";

    /// <summary>
    /// The default column name in the data reader which will be used to read the value of the authentication scheme of the login process.
    /// 
    /// If this column is not present in the login response the default authentication scheme is used. Return new value to use a different authentication scheme with the login endpoint.
    /// </summary>
    public string? SchemeColumnName { get; set; } = "scheme";

    /// <summary>
    /// The default column name in the data reader which will return a text message with the login status.
    /// </summary>
    public string? MessageColumnName { get; set; } = "message";

    /// <summary>
    /// Any columns retrieved from the reader during login, which don't have a name in `StatusColumnName` or `SchemeColumnName` will be used to create a new identity  `Claim`:
    /// 
    /// Column name will be interpreted as the claim type and the associated reader value for that column will be the claim value.
    /// 
    /// When this value is set to true (default) column name will try to match the constant name in the [ClaimTypes class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0) to retrieve the value.
    /// 
    /// For example, column name `NameIdentifier` or `name_identifier` (when transformed by the default name transformer) will match the key `NameIdentifier` which translates to this: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
    /// </summary>
    public bool UseActiveDirectoryFederationServicesClaimTypes { get; set; } = true;

    /// <summary>
    /// Default claim type for user id.
    /// </summary>
    public string DefaultUserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    private string _defaultNameClaimType = ClaimTypes.Name;
    internal bool UsingDefaultNameClaimType = true;

    /// <summary>
    /// Default claim type for user name.
    /// </summary>
    public string DefaultNameClaimType
    {
        get => _defaultNameClaimType;
        set
        {
            _defaultNameClaimType = value;
            UsingDefaultNameClaimType = string.Equals(value, ClaimTypes.Name, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Default claim type for user roles.
    /// </summary>
    public string DefaultRoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>
    /// If true, return any response from auth endpoints (login and logout) if response hasn't been written by auth handler.
    /// For cookie auth, this will return full record to response as returned by the routine.
    /// For bearer token auth, this will be ignored because bearer token auth writes it's own response (with tokens).
    /// This option will also be ignored if message column is present (see MessageColumnName option).
    /// Default is false.
    /// </summary>
    public bool SerializeAuthEndpointsResponse { get; set; } = false;

    /// <summary>
    /// Don't write real parameter values when logging parameters from auth endpoints and obfuscate instead.
    /// This prevents user credentials including password to end up in application logs.
    /// Default is true.
    /// </summary>
    public bool ObfuscateAuthParameterLogValues { get; set; } = true;

    /// <summary>
    /// The default column name in the data reader which will be used to read the value of the hash of the password.
    /// If this column is present, the value will be used to verify the password from the password parameter.
    /// Password parameter is the first parameter which name contains the value of PasswordParameterNameContains.
    /// If verification fails, the login will fail and the HTTP Status Code will be set to 404 Not Found.
    /// </summary>
    public string HashColumnName { get; set; } = "hash";

    /// <summary>
    /// The default name of the password parameter. The first parameter which name contains this value will be used as the password parameter.
    /// This is used to verify the password from the password parameter when login endpoint returns a hash of the password (see HashColumnName).
    /// </summary>
    public string PasswordParameterNameContains { get; set; } = "pass";

    /// <summary>
    /// Default password hasher object. Inject custom password hasher object to add default password hasher.
    /// </summary>
    public IPasswordHasher? PasswordHasher { get; set; } = new PasswordHasher();

    /// <summary>
    /// Command that is executed when the password verification fails. There are three text parameters:
    ///     - authentication scheme used for the login (if exists)
    ///     - user id used for the login (if exists)
    ///     - user name used for the login (if exists)
    /// Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).
    /// </summary>
    public string? PasswordVerificationFailedCommand { get; set; } = null;

    /// <summary>
    /// Command that is executed when the password verification succeeds. There are three text parameters:
    ///     - authentication scheme used for the login (if exists)
    ///     - user id used for the login (if exists)
    ///     - user name used for the login (if exists)
    /// Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).
    /// </summary>
    public string? PasswordVerificationSucceededCommand { get; set; } = null;

    /// <summary>
    /// Set user context to true for all requests. 
    /// When this is set to true, user information (user id, user name and user roles) will be set to the context variables.
    /// You can set this individually for each request.
    /// </summary>
    public bool UseUserContext { get; set; } = false;

    /// <summary>
    /// User id context key that is used to set context variable for the user id.
    /// </summary>
    public string? UserIdContextKey { get; set; } = "request.user_id";

    /// <summary>
    /// User name context key that is used to set context variable for the user name.
    /// </summary>
    public string? UserNameContextKey { get; set; } = "request.user_name";

    /// <summary>
    /// User roles context key that is used to set context variable for the user roles.
    /// </summary>
    public string? UserRolesContextKey { get; set; } = "request.user_roles";

    /// <summary>
    /// IP address context key that is used to set context variable for the IP address.
    /// </summary>
    public string? IpAddressContextKey { get; set; } = "request.ip_address";

    /// <summary>
    /// When this value is set and user context is used, all user claims will be serialized to JSON value and set to the context variable with this name.
    /// </summary>
    public string? UserClaimsContextKey { get; set; } = null;

    /// <summary>
    /// Set user parameters to true for all requests. 
    /// When this is set to true, user information (user id, user name and user roles) will be set to the matching parameter names if available.
    /// You can set this individually for each request.
    /// </summary>
    public bool UseUserParameters { get; set; } = false;

    /// <summary>
    /// User id parameter name that is used to set parameter value for the user id.
    /// Parameter name can be original or converted and it has to be the text type.
    /// </summary>
    public string? UserIdParameterName { get; set; } = "_user_id";

    /// <summary>
    /// User name parameter name that is used to set parameter value for the user name.
    /// Parameter name can be original or converted and it has to be the text type.
    /// </summary>
    public string? UserNameParameterName { get; set; } = "_user_name";

    /// <summary>
    /// User roles parameter name that is used to set parameter value for the user roles.
    /// Parameter name can be original or converted and it has to be the text array type.
    /// </summary>
    public string? UserRolesParameterName { get; set; } = "_user_roles";

    /// <summary>
    /// IP address parameter name that is used to set parameter value for the IP address.
    /// Parameter name can be original or converted and it has to be the text type.
    /// </summary>
    public string? IpAddressParameterName { get; set; } = "_ip_address";

    /// <summary>
    /// All user claims will be serialized to JSON value and set to the parameter with this name.
    /// </summary>
    public string? UserClaimsParameterName { get; set; } = "_user_claims";
}