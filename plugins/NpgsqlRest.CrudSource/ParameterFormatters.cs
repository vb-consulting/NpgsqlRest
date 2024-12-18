using Npgsql;

namespace NpgsqlRest.CrudSource;

public class SelectParameterFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = false;

    public string AppendCommandParameter(NpgsqlRestParameter parameter, int index, int count)
    {
        if (index == 0)
        {
            return string.Concat("where ", Environment.NewLine, "    ", parameter.ActualName, " = $1");
        }
        return string.Concat(" and ", parameter.ActualName, " = $", (index+1).ToString());
    }

    public string? AppendEmpty() => "";
}


public class UpdateParameterFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;

    public string? FormatCommand(Routine routine, NpgsqlParameterCollection parameters)
    {
        int setCount = 0;
        int whereCount = 0;

        string set = "set";
        string where = "where";

        for (int i = 0; i < parameters.Count; i++)
        {;
            NpgsqlRestParameter param = (NpgsqlRestParameter)parameters[i];

            if (param.TypeDescriptor.IsPk)
            {
                if (whereCount == 0)
                {
                    where = string.Concat(where, Environment.NewLine, "    ", param.ActualName, " = $", (i+1).ToString());
                }
                else
                {
                    where = string.Concat(where, Environment.NewLine, "    and ", param.ActualName, " = $", (i+1).ToString());
                }
                whereCount++;
            }
            else
            {
                if (setCount == 0)
                {
                    set = string.Concat(set, Environment.NewLine, "    ", param.ActualName, " = $", (i+1).ToString());
                }
                else
                {
                    set = string.Concat(set, ",", Environment.NewLine, "    ", param.ActualName, " = $", (i+1).ToString());
                }
                setCount++;
            }
        }

        if (whereCount == 0 || setCount == 0)
        {
            return null;
        } 

        return string.Format(routine.Expression, set, where);
    }
}

public class InsertParameterFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;

    public string? FormatCommand(Routine routine, NpgsqlParameterCollection parameters)
    {
        if (parameters.Count == 0)
        {
            return null;
        }

        string f1 = "";
        string f2 = "";
        string f3 = "";

        for (int i = 0; i < parameters.Count; i++)
        {
            NpgsqlRestParameter param = (NpgsqlRestParameter)parameters[i];
            if (i == 0)
            {
                f1 = string.Concat(f1, param.ActualName);
                f3 = string.Concat(f3, "$", (i+1).ToString());
            }
            else
            {
                f1 = string.Concat(f1, ", ", param.ActualName);
                f3 = string.Concat(f3, ", ", "$", (i+1).ToString());
            }
            if (param.TypeDescriptor.IsIdentity && string.IsNullOrEmpty(f2))
            {
                f2 = string.Concat(Environment.NewLine, "overriding system value");
            }
        }

        return string.Format(routine.Expression, f1, f2, f3);
    }
}

public class DeleteParameterFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;

    public string? FormatCommand(Routine routine, NpgsqlParameterCollection parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Format(routine.Expression, "");
        }

        string f1 = "where";

        for (int i = 0; i < parameters.Count; i++)
        {
            NpgsqlRestParameter param = (NpgsqlRestParameter)parameters[i];
            if (i == 0)
            {
                f1 = string.Concat(f1, Environment.NewLine, "    ", param.ActualName, " = $1");
            }
            else
            {
                f1 = string.Concat(f1, Environment.NewLine, "    and ", param.ActualName, " = $", (i+1).ToString());
            }
        }

        return string.Format(routine.Expression, f1);
    }
}