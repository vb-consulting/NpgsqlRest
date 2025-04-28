using System.Text.RegularExpressions;

namespace NpgsqlRest.Auth;

public partial class PostgreSqlParameterCounter
{
    [GeneratedRegex(@"\$\d+")]
    public static partial Regex PostgreSqlParameterPattern();

    public static int CountParameters(string sql)
    {
        return PostgreSqlParameterPattern().Matches(sql).Count;
    }
}
