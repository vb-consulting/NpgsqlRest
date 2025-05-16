using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NpgsqlRest;

[JsonSerializable(typeof(string))]
internal partial class NpgsqlRestSerializerContext : JsonSerializerContext;

internal static partial class ParameterPattern
{
    [GeneratedRegex(@"\$\d+")]
    public static partial Regex PostgreSqlParameterPattern();
}

public static class PgConverters
{
    private static readonly JsonSerializerOptions PlainTextSerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new NpgsqlRestSerializerContext()
    };

    [UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    [UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamic",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    internal static string SerializeString(string value) => JsonSerializer.Serialize(value, PlainTextSerializerOptions);

    [UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode",
    Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    [UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamic",
    Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    internal static string SerializeObject(object? value) => JsonSerializer.Serialize(value, PlainTextSerializerOptions);

    [UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    [UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamic",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    internal static string SerializeString(ref ReadOnlySpan<char> value) => JsonSerializer.Serialize(value.ToString(), PlainTextSerializerOptions);

    internal static ReadOnlySpan<char> PgUnknownToJsonArray(ref ReadOnlySpan<char> value)
    {
        if (value[0] != Consts.OpenParenthesis || value[^1] != Consts.CloseParenthesis)
        {
            // should never happen
            return value;
        }

        var len = value.Length;
        var result = new StringBuilder(len * 2);
        result.Append(Consts.OpenBracket);
        var current = new StringBuilder();
        bool insideQuotes = false;
        bool first = true;

        for (int i = 1; i < len; i++)
        {
            char currentChar = value[i];

            if ((currentChar == Consts.Comma || (currentChar == Consts.CloseParenthesis && i == len - 1)) && !insideQuotes)
            {
                if (!first)
                {
                    result.Append(Consts.Comma);
                }
                else
                {
                    first = false;
                }
                if (current.Length == 0)
                {
                    result.Append(Consts.Null);
                }
                else
                {
                    var segment = current.ToString();
                    result.Append(SerializeString(segment));
                    current.Clear();
                }
            }
            else
            {
                if (currentChar == Consts.DoubleQuote && i < len - 2 && value[i + 1] == Consts.DoubleQuote)
                {
                    current.Append(currentChar);
                    i++;
                    continue;
                }
                if (currentChar == Consts.DoubleQuote)
                {
                    insideQuotes = !insideQuotes;
                }
                else
                {
                    if (currentChar == Consts.Backslash && i < len - 2 && value[i + 1] == Consts.Backslash)
                    {
                        i++;
                        current.Append(currentChar);
                    }
                    else
                    {
                        current.Append(currentChar);
                    }
                }
            }
        }

        result.Append(Consts.CloseBracket);
        return result.ToString();
    }

    internal static ReadOnlySpan<char> PgArrayToJsonArray(ReadOnlySpan<char> value, TypeDescriptor descriptor)
    {
        var len = value.Length;
        if (value.IsEmpty || len < 3 || value[0] != Consts.OpenBrace || value[^1] != Consts.CloseBrace)
        {
            if (descriptor.IsArray is true)
            {
                return Consts.EmptyArray.AsSpan();
            }
            return value;
        }

        var result = new StringBuilder(len * 2);
        result.Append(Consts.OpenBracket);
        var current = new StringBuilder();
        var quoted = !(descriptor.IsNumeric || descriptor.IsBoolean || descriptor.IsJson);
        bool insideQuotes = false;
        bool hasQuotes = false;

        bool IsNull()
        {
            if (current.Length == 4)
            {
                return
                    current[0] == 'N' &&
                    current[1] == 'U' &&
                    current[2] == 'L' &&
                    current[3] == 'L';
            }
            return false;
        }

        for (int i = 1; i < len; i++)
        {
            char currentChar = value[i];

            if (currentChar == Consts.DoubleQuote && value[i - 1] != Consts.Backslash)
            {
                insideQuotes = !insideQuotes;
                hasQuotes = true;
            }
            else if ((currentChar == Consts.Comma && !insideQuotes) || currentChar == Consts.CloseBrace)
            {
                var currentIsNull = IsNull() && !hasQuotes;
                if (quoted && !currentIsNull)
                {
                    result.Append(Consts.DoubleQuote);
                }

                if (currentIsNull)
                {
                    result.Append(Consts.Null);
                }
                else
                {
                    result.Append(current);
                }

                if (quoted && !currentIsNull)
                {
                    result.Append(Consts.DoubleQuote);
                }
                if (currentChar != Consts.CloseBrace)
                {
                    result.Append(Consts.Comma);
                }
                current.Clear();
                hasQuotes = false;
            }
            else
            {
                if (descriptor.IsBoolean)
                {
                    if (currentChar == 't')
                    {
                        current.Append(Consts.True);
                    }
                    else if (currentChar == 'f')
                    {
                        current.Append(Consts.False);
                    }
                    else
                    {
                        current.Append(currentChar);
                    }
                }
                else if (descriptor.IsDateTime)
                {
                    //json time requires T between date and time
                    current.Append(currentChar == Consts.Space ? 'T' : currentChar);
                }
                else 
                {
                    if (currentChar == '\n')
                    {
                        current.Append("\\n");
                    }
                    else if (currentChar == '\t')
                    {
                        current.Append("\\r");
                    }
                    else if (currentChar == '\r')
                    {
                        current.Append("\\r");
                    }
                    else
                    {
                        current.Append(currentChar);
                    }

                }
            }
        }

        result.Append(Consts.CloseBracket);
        return result.ToString().AsSpan();
    }

    internal static string QuoteText(ReadOnlySpan<char> value)
    {
        int newLength = value.Length + 2;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == Consts.DoubleQuote)
            {
                newLength++;
            }
        }
        Span<char> result = stackalloc char[newLength];
        result[0] = Consts.DoubleQuote;
        int currentPos = 1;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == Consts.DoubleQuote)
            {
                result[currentPos++] = Consts.DoubleQuote;
                result[currentPos++] = Consts.DoubleQuote;
            }
            else
            {
                result[currentPos++] = value[i];
            }
        }
        result[currentPos] = Consts.DoubleQuote;
        return new string(result);
    }

    internal static string QuoteDateTime(ref ReadOnlySpan<char> value)
    {
        int newLength = value.Length + 2;
        Span<char> result = stackalloc char[newLength];
        result[0] = Consts.DoubleQuote;
        int currentPos = 1;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == Consts.Space)
            {
                result[currentPos++] = 'T';
            }
            else
            {
                result[currentPos++] = value[i];
            }
        }
        result[currentPos] = Consts.DoubleQuote;
        return new string(result);
    }

    internal static string Quote(ref ReadOnlySpan<char> value)
    {
        int newLength = value.Length + 2;
        Span<char> result = stackalloc char[newLength];
        result[0] = Consts.DoubleQuote;
        int currentPos = 1;
        for (int i = 0; i < value.Length; i++)
        {
            result[currentPos++] = value[i];
        }
        result[currentPos] = Consts.DoubleQuote;
        return new string(result);
    }

    public static int PgCountParams(this string sql)
    {
        return ParameterPattern.PostgreSqlParameterPattern().Matches(sql).Count;
    }

    public static string SerializeDatbaseObject(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return Consts.Null;
        }
        if (value is string stringValue)
        {
            return SerializeObject(stringValue);
        }
        else if (value is int or long or double or decimal or float or short or byte)
        {
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
        }
        else if (value is bool boolValue)
        {
            return boolValue.ToString().ToLowerInvariant();
        }
        else if (value is DateTime dateTime)
        {
            return string.Concat("\"", dateTime.ToString("o"), "\"");
        }
        else if (value is Array array)
        {
            return FormatArray(array);
        }
        else
        {
            return string.Concat("\"", value.ToString(), "\"");
        }
    }

    private static string FormatArray(Array array)
    {
        var elements = new List<string>();

        for (int i = 0; i < array.Length; i++)
        {
            var item = array.GetValue(i);
            elements.Add(SerializeDatbaseObject(item));
        }

        return $"[{string.Join(",", elements)}]";
    }
}