using Npgsql;
namespace NpgsqlRest;

public interface IEndpointCreateHandler
{
    /// <summary>
    /// Before creating endpoints.
    /// </summary>
    /// <param name="builder">current application builder</param>
    /// <param name="logger">configured application logger</param>
    /// <param name="options">current NpgsqlRest options</param>
    void Setup(IApplicationBuilder builder, ILogger? logger, NpgsqlRestOptions options) {  }

    /// <summary>
    /// After successful endpoint creation.
    /// </summary>
    void Handle(RoutineEndpoint endpoint) { }

    /// <summary>
    /// After all endpoints are created.
    /// </summary>
    void Cleanup(RoutineEndpoint[] endpoints) {  }

    /// <summary>
    /// After all endpoints are created.
    /// </summary>
    void Cleanup() { }
}

public interface IRoutineSourceParameterFormatter
{
    /// <summary>
    /// Return true to call FormatCommand
    /// Return false to call AppendCommandParameter or AppendEmpty (when no parameters are present).
    /// </summary>
    bool IsFormattable { get; }

    /// <summary>
    /// Return true to call format methods with HttpContext reference.
    /// Return false to call format methods without HttpContext reference.
    /// </summary>
    bool RefContext { get => false; }

    /// <summary>
    /// Appends result to the command expression string.
    /// </summary>
    /// <param name="parameter">NpgsqlRestParameter extended parameter with actual name and type descriptor</param>
    /// <param name="index">index of the current parameter</param>
    /// <returns>string to append to expression or null to skip (404 if endpoint is not handled in the next handler)</returns>
    string? AppendCommandParameter(NpgsqlRestParameter parameter, int index) => null;

    /// <summary>
    /// Formats the command expression string.
    /// </summary>
    /// <param name="routine">Current routine data</param>
    /// <param name="parameters">Extended parameters list.</param>
    /// <returns>expression string or null to skip (404 if endpoint is not handled in the next handler)</returns>
    string? FormatCommand(Routine routine, NpgsqlParameterCollection parameters) => null;

    /// <summary>
    /// Called when there are no parameters to append.
    /// </summary>
    /// <returns>string to append to expression or null to skip (404 if endpoint is not handled in the next handler)</returns>
    string? AppendEmpty() => null;

    /// <summary>
    /// Appends result to the command expression string.
    /// </summary>
    /// <param name="parameter">NpgsqlRestParameter extended parameter with actual name and type descriptor</param>
    /// <param name="index">index of the current parameter</param>
    /// <param name="context">HTTP context reference</param>
    /// <returns>string to append to expression or null to skip (404 if endpoint is not handled in the next handler)</returns>
    string? AppendCommandParameter(NpgsqlRestParameter parameter, int index, HttpContext context) => null;

    /// <summary>
    /// Formats the command expression string.
    /// </summary>
    /// <param name="routine">Current routine data</param>
    /// <param name="parameters">Extended parameters list.</param>
    /// <param name="context">HTTP context reference</param>
    /// <returns>expression string or null to skip (404 if endpoint is not handled in the next handler)</returns>
    string? FormatCommand(Routine routine, NpgsqlParameterCollection parameters, HttpContext context) => null;

    /// <summary>
    /// Called when there are no parameters to append.
    /// </summary>
    /// <param name="context">HTTP context reference</param>
    /// <returns>string to append to expression or null to skip (404 if endpoint is not handled in the next handler)</returns>
    string? AppendEmpty(HttpContext context) => null;
}

public interface IRoutineSource
{
    /// <summary>
    /// Comments mode for the current routine source.
    /// </summary>
    CommentsMode? CommentsMode { get; set; }

    /// <summary>
    /// Yield all routines with the formatters from the current source.
    /// </summary>
    /// <param name="options">Current options</param>
    /// <param name="serviceProvider">Service provider</param> 
    /// <returns></returns>
    IEnumerable<(Routine, IRoutineSourceParameterFormatter)> Read(NpgsqlRestOptions options, IServiceProvider? serviceProvider);

    /// <summary>
    /// SQL Query that returns data source.
    /// When it doesn't contain any blanks, it is interpreted as a function name.
    /// Set to NULL to use default source query.
    /// </summary>
    string? Query { get; set; }
    
    /// <summary>
    /// Query parameter
    /// </summary>
    string? SchemaSimilarTo { get; set; }

    /// <summary>
    /// Query parameter
    /// </summary>
    string? SchemaNotSimilarTo { get; set; }

    /// <summary>
    /// Query parameter
    /// </summary>
    string[]? IncludeSchemas { get; set; }

    /// <summary>
    /// Query parameter
    /// </summary>
    string[]? ExcludeSchemas { get; set; }

    /// <summary>
    /// Query parameter
    /// </summary>
    string? NameSimilarTo { get; set; }

    /// <summary>
    /// Query parameter
    /// </summary>
    string? NameNotSimilarTo { get; set; }

    /// <summary>
    /// Query parameter
    /// </summary>
    string[]? IncludeNames { get; set; }

    /// <summary>
    /// Query parameter
    /// </summary>
    string[]? ExcludeNames { get; set; }
}
