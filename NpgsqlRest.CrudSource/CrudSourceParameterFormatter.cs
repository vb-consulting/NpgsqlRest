namespace NpgsqlRest.CrudSource;

public class CrudSourceParameterFormatter : IRoutineSourceParameterFormatter
{
    public string AppendCommandParameter(ref NpgsqlRestParameter parameter, ref int index, ref int count)
    {
        if (index == 0)
        {
            return string.Concat(" where ", parameter.ActualName, " = $1");
        }
        return string.Concat(" and ", parameter.ActualName, " = $", (index+1).ToString());
    }

    public string? FormatEmpty() => "";
}
