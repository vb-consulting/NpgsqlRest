using System.Text;

namespace NpgsqlRest;

public class RoutineSourceParameterFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = false;

    public string AppendCommandParameter(NpgsqlRestParameter parameter, int index, int count)
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

public class RoutineSourceCustomTypesParameterFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;

    public string FormatCommand(Routine routine, List<NpgsqlRestParameter> parameters)
    {
        var sb = new StringBuilder(routine.Expression);
        var count = parameters.Count;
        for (var i = 0; i < count; i++)
        {
            var parameter = parameters[i];
            var suffix = parameter.TypeDescriptor.IsCastToText() ? $"::{parameter.TypeDescriptor.OriginalType}" : "";
            if (i > 0)
            {
                sb.Append(Consts.Comma);
            }
            if (parameter.TypeDescriptor.CustomType is null)
            {
                sb.Append(parameter.ActualName is null ?
                    string.Concat("$", (i + 1).ToString(), suffix) :
                    string.Concat(parameter.ActualName, "=>$", (i + 1).ToString()));
            }
            else
            {
                if (parameter.TypeDescriptor.CustomTypePosition == 1)
                {
                    sb.Append(parameter.TypeDescriptor.OriginalParameterName);
                    sb.Append("=>row(");
                }
                sb.Append(string.Concat("$", (i + 1).ToString(), suffix));
                if (i == count - 1 || parameter.TypeDescriptor.CustomTypePosition != parameters[i + 1].TypeDescriptor.CustomTypePosition - 1)
                {
                    sb.Append(string.Concat(")::", parameters[i].TypeDescriptor.CustomType));
                }
            }
        }
        sb.Append(Consts.CloseParenthesis);
        return sb.ToString();
        /*
        var sb = new StringBuilder(routine.Expression);
        var count = parameters.Count;
        var sorted = parameters.Select((param, index) => (param, index)).OrderBy(p => p.param.Ordinal).ToArray();
        for (var i = 0; i < count; i++)
        {
            var parameter = sorted[i].param;
            var index = sorted[i].index;
            var suffix = parameter.TypeDescriptor.IsCastToText() ? $"::{parameter.TypeDescriptor.OriginalType}" : "";
            if (i > 0)
            {
                sb.Append(Consts.Comma);
            }
            if (parameter.TypeDescriptor.CustomType is null)
            {
                sb.Append(parameter.ActualName is null ? 
                    string.Concat("$", (i + 1).ToString(), suffix) : 
                    string.Concat(parameter.ActualName, "=>$", (index + 1).ToString()));
            }
            else
            {
                if (parameter.TypeDescriptor.CustomTypePosition == 1)
                {
                    sb.Append(parameter.TypeDescriptor.OriginalParameterName);
                    sb.Append("=>row(");
                }
                sb.Append(string.Concat("$", (index + 1).ToString(), suffix));
                if (i == count - 1 || parameter.TypeDescriptor.CustomTypePosition != sorted[i + 1].param.TypeDescriptor.CustomTypePosition - 1)
                {
                    sb.Append(string.Concat(")::", parameter.TypeDescriptor.CustomType));
                }
            }
        }
        sb.Append(Consts.CloseParenthesis);
        return sb.ToString();
        */
    }
}