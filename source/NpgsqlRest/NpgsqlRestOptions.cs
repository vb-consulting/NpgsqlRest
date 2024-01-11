using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using Npgsql;

namespace NpgsqlRest;

public enum Method { GET, PUT, POST, DELETE, HEAD, OPTIONS, TRACE, PATCH, CONNECT }
public enum EndpointParameters { QueryString, BodyJson }

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
    NpgsqlRestHttpFileOptions? httpFileOptions = null,
    Func<Routine, NpgsqlRestOptions, RoutineEndpointMeta, RoutineEndpointMeta?>? endpointMetaCallback = null,
    Func<string?, string?>? nameConverter = null,
    bool requiresAuthorization = false,
    LogLevel logLevel = LogLevel.Information,
    Func<StringValues, TypeDescriptor, NpgsqlParameter, NpgsqlParameter?>? queryStringParameterParserCallback = null,
    Func<JsonNode?, TypeDescriptor, NpgsqlParameter, NpgsqlParameter?>? jsonBodyParameterParserCallback = null,
    bool logConnectionNoticeEvents = true,
    int? commandTimeout = null,
    bool logParameterMismatchWarnings = true,
    Method? defaultHttpMethod = null,
    EndpointParameters? defaultParameters = null)
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
    public Func<Routine, NpgsqlRestOptions, string> UrlPathBuilder { get; set; } = urlPathBuilder ?? Defaults.DefaultUrlBuilder;
    /// <summary>
    /// Set to true to get the PostgreSQL connection from the service provider. Otherwise, it will be created from the connection string property.
    /// </summary>
    public bool ConnectionFromServiceProvider { get; set; } = connectionFromServiceProvider;
    /// <summary>
    /// Configure creation of the .http file on service build.
    /// </summary>
    public NpgsqlRestHttpFileOptions HttpFileOptions { get; set; } = httpFileOptions ?? new NpgsqlRestHttpFileOptions(HttpFileOption.Disabled);
    /// <summary>
    /// Callback, if not null, will be called after endpoint meta data is created.
    /// Use this to do custom configuration over routine endpoints. 
    /// Return null to disable endpoint.
    /// </summary>
    public Func<Routine, NpgsqlRestOptions, RoutineEndpointMeta, RoutineEndpointMeta?>? EndpointMetaCallback { get; set; } = endpointMetaCallback;
    /// <summary>
    /// Method that converts names for parameters and return fields. 
    /// By default it is a lower camel case.
    /// Use NameConverter = name => name to preserve original names.
    /// </summary>
    public Func<string?, string?> NameConverter { get; set; } = nameConverter ?? Defaults.CamelCaseNameConverter;
    /// <summary>
    /// Set to true to require authorization for all endpoints.
    /// </summary>
    public bool RequiresAuthorization { get; set;  } = requiresAuthorization;
    /// <summary>
    /// Set the the minimal level of log messages or LogLevel.None to disable logging.
    /// </summary>
    public LogLevel LogLevel { get; set; } = logLevel;
    /// <summary>
    /// Callback, if not null, will be called for every parameter in the query string to assign a database parameter value to NpgsqlParameter from a string.
    /// Return null to fallback to default parser behavior.
    /// </summary>
    public Func<StringValues, TypeDescriptor, NpgsqlParameter, NpgsqlParameter?>? QueryStringParameterParserCallback { get; set; } = 
        queryStringParameterParserCallback;
    /// <summary>
    /// Callback, if not null, will be called for every parameter in the json body to assign a database parameter value to NpgsqlParameter from a json value.
    /// Return null to fallback to default parser behavior.
    /// </summary>
    public Func<JsonNode?, TypeDescriptor, NpgsqlParameter, NpgsqlParameter?>? JsonBodyParameterParserCallback { get; set; } = 
        jsonBodyParameterParserCallback;
    /// <summary>
    /// Set to true to log connection notice events.
    /// </summary>
    public bool LogConnectionNoticeEvents { get; set; } = logConnectionNoticeEvents;
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
    /// Default HTTP method for all endpoints. 
    /// NULL is default behavior: if function name contains "get", it is GET, otherwise POST.
    /// </summary>
    public Method? DefaultHttpMethod { get; set; } = defaultHttpMethod;
    /// <summary>
    /// Default parameter position Query String or JSON Body.
    /// NULL is default behavior: if endpoint is not POST, use Query String, otherwise JSON Body.
    /// </summary>
    public EndpointParameters? DefaultParameters { get; set; } = defaultParameters;
}
