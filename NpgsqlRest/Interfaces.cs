﻿namespace NpgsqlRest;

public interface IEndpointCreateHandler
{
    void Setup(IApplicationBuilder builder, ILogger? logger) {  }
    void Handle(Routine routine, RoutineEndpoint endpoint);
    void Cleanup() {  }
}

public interface IRoutineSourceParameterFormatter
{
    bool IsFormattable { get; }
    string? AppendCommandParameter(ref NpgsqlRestParameter parameter, ref int index, ref int count) => null;
    string? FormatCommand(ref Routine routine, ref List<NpgsqlRestParameter> parameters) => null;
    string? FormatEmpty() => null;
}

public interface IRoutineSource
{
    IEnumerable<(Routine, IRoutineSourceParameterFormatter)> Read(NpgsqlRestOptions options);
}