namespace NpgsqlRest;

public class RoutineSourceParameterFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = false;

    public string AppendCommandParameter(ref NpgsqlRestParameter parameter, ref int index, ref int count)
    {
        var suffix = parameter.TypeDescriptor.IsCastToText() ? $"::{parameter.TypeDescriptor.OriginalType}" : "";
        if (index == 0)
        {
            if (count == 1)
            {
                return parameter.ActualName is null ? 
                    string.Concat("$1", suffix, ")") : 
                    string.Concat(parameter.ActualName, "=>$1", suffix, ")");
            }
            return parameter.ActualName is null ? 
                string.Concat("$1", suffix) : 
                string.Concat(parameter.ActualName, "=>$1", suffix);
        }
        if (index == count - 1)
        {
            return parameter.ActualName is null ? 
                string.Concat(",", "$", (index + 1).ToString(), suffix, ")") : 
                string.Concat(",", parameter.ActualName, "=>$", (index + 1).ToString(), suffix, ")");
        }
        return parameter.ActualName is null ?
            string.Concat(",", "$", (index + 1).ToString(), suffix) : 
            string.Concat(",", parameter.ActualName, "=>$", (index + 1).ToString(), suffix);
    }

    public string? AppendEmpty() => ")";
}
