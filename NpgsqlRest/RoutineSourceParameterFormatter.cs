namespace NpgsqlRest;

public class RoutineSourceParameterFormatter : IRoutineSourceParameterFormatter
{
    public string AppendCommandParameter(ref NpgsqlRestParameter parameter, ref int index, ref int count)
    {
        var suffix = parameter.TypeDescriptor.IsCastToText() ? $"::{parameter.TypeDescriptor.OriginalType}" : "";
        if (index == 0)
        {
            if (count == 1)
            {
                return string.Concat("$1", suffix, ")");
            }
            return string.Concat("$1", suffix);
        }
        if (index == count - 1)
        {
            return string.Concat(",$", (index + 1).ToString(), suffix, ")");
        }
        return string.Concat(",$", (index + 1).ToString(), suffix);
    }

    public string? FormatEmpty() => ")";
}
