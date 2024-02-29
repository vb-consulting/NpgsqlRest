namespace NpgsqlRest;

public interface IEndpointCreateHandler
{
    /// <summary>
    /// Before creating endpoints.
    /// </summary>
    /// <param name="builder">current application builder</param>
    /// <param name="logger">configured application logger</param>
    void Setup(IApplicationBuilder builder, ILogger? logger) {  }
    /// <summary>
    /// After successful endpoint creation.
    /// </summary>
    void Handle(Routine routine, RoutineEndpoint endpoint);
    /// <summary>
    /// After all endpoints are created.
    /// </summary>
    void Cleanup() {  }
}

public interface IRoutineSourceParameterFormatter
{
    /// <summary>
    /// Return true to call FormatCommand and FormatEmpty.
    /// Return false to call AppendCommandParameter and AppendEmpty.
    /// </summary>
    bool IsFormattable { get; }
    /// <summary>
    /// Appends result to the command expression string.
    /// </summary>
    /// <param name="parameter">NpgsqlRestParameter extended parameter with actual name and type descriptor</param>
    /// <param name="index">index of the current parameter</param>
    /// <param name="count">total parameter count</param>
    /// <returns>string to append to expression or null to skip (404)</returns>
    string? AppendCommandParameter(ref NpgsqlRestParameter parameter, ref int index, ref int count) => null;
    /// <summary>
    /// Formats the command expression string.
    /// </summary>
    /// <param name="routine">Current routine metadata</param>
    /// <param name="parameters">Extended parameters list.</param>
    /// <returns>expression string or null to skip (404)</returns>
    string? FormatCommand(ref Routine routine, ref List<NpgsqlRestParameter> parameters) => null;
    /// <summary>
    /// Called when there are no parameters to append.
    /// </summary>
    /// <returns>string to append to expression or null to skip (404)</returns>
    string? AppendEmpty() => null;
    /// <summary>
    /// Called when there are no parameters to format.
    /// </summary>
    /// <param name="routine"></param>
    /// <returns>expression string or null to skip (404)</returns>
    string? FormatEmpty(ref Routine routine) => null;
}

public interface IRoutineSource
{
    /// <summary>
    /// Comments mode for the current routine source.
    /// </summary>
    CommentsMode? CommentsMode { get => null; }
    /// <summary>
    /// Yield all routines with the formatters from the current source.
    /// </summary>
    /// <param name="options">Current options</param>
    /// <returns></returns>
    IEnumerable<(Routine, IRoutineSourceParameterFormatter)> Read(NpgsqlRestOptions options);
}
