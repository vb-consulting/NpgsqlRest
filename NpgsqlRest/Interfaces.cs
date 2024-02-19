namespace NpgsqlRest;

public interface IEndpointCreateHandler
{
    void Setup(IApplicationBuilder builder, ILogger? logger) {  }
    void Handle(Routine routine, RoutineEndpoint endpoint);
    void Cleanup() {  }
}

public interface IRoutineSourceParameterFormatter
{
    string Format(ref Routine routine, ref NpgsqlRestParameter parameter, ref int index, ref int count);
    string? FormatEmpty() => null;
}

public interface IRoutineSource
{
    string Query { get; set; }
    IEnumerable<Routine> Read(NpgsqlRestOptions options);
    IRoutineSourceParameterFormatter GetRoutineSourceParameterFormatter();
}
