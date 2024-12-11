namespace NpgsqlRest;

public class ParameterValidationValues(
    HttpContext context, 
    Routine routine,
    NpgsqlRestParameter parameter)
{
    /// <summary>
    /// Current HttpContext.
    /// </summary>
    public readonly HttpContext Context = context;
    /// <summary>
    /// Current Routine.
    /// </summary>
    public readonly Routine Routine = routine;
    /// <summary>
    /// Parameter to be validated. Note: if parameter is using default value and value not provided, parameter.Value is null.
    /// </summary>
    public readonly NpgsqlRestParameter Parameter = parameter;
}
