using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace NpgsqlRest;

[JsonSerializable(typeof(string))]
internal partial class NpgsqlRestSerializerContext : JsonSerializerContext { }

internal static class PgConverters
{
    private static readonly JsonSerializerOptions plainTextSerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new NpgsqlRestSerializerContext()
    };

    [UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    [UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamic",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
    public static string SerializeString(ref string value) => JsonSerializer.Serialize(value, plainTextSerializerOptions);

    public static string PgUnknownToJsonArray(ref string value)
    {
        if (value[0] != '(' || value[^1] != ')')
        {
            // should never happen
            return value;
        }

        var len = value.Length;
        var result = new StringBuilder(len * 2);
        result.Append('[');
        var current = new StringBuilder();
        bool insideQuotes = false;
        bool first = true;

        for (int i = 1; i < len; i++)
        {
            char currentChar = value[i];

            if ((currentChar == ',' || (currentChar == ')' && i == len - 1)) && !insideQuotes)
            {
                if (!first)
                {
                    result.Append(',');
                }
                else
                {
                    first = false;
                }
                if (current.Length == 0)
                {
                    result.Append("null");
                }
                else
                {
                    var segment = current.ToString();
                    result.Append(SerializeString(ref segment));
                    current.Clear();
                }
            }
            else
            {
                if (currentChar == '"' && i < len - 2 && value[i + 1] == '"')
                {
                    current.Append(currentChar);
                    i++;
                    continue;
                }
                if (currentChar == '"')
                {
                    insideQuotes = !insideQuotes;
                }
                else
                {
                    if (currentChar == '\\' && i < len - 2 && value[i + 1] == '\\')
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

        result.Append(']');
        return result.ToString();
    }

    public static string PgArrayToJsonArray(ref string value, ref TypeDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 3 || value[0] != '{' || value[^1] != '}')
        {
            return value;
        }

        var result = new StringBuilder(value.Length * 2);
        result.Append('[');
        var current = new StringBuilder();
        var quoted = !(descriptor.IsNumeric || descriptor.IsBoolean || descriptor.IsJson);
        bool insideQuotes = false;
        bool hasQuotes = false;

        bool isNull()
        {
            if (current?.Length == 4)
            {
                return
                    current[0] == 'N' &&
                    current[1] == 'U' &&
                    current[2] == 'L' &&
                    current[3] == 'L';
            }
            return false;
        }

        for (int i = 1; i < value.Length; i++)
        {
            char currentChar = value[i];

            if (currentChar == '"' && value[i - 1] != '\\')
            {
                insideQuotes = !insideQuotes;
                hasQuotes = true;
            }
            else if ((currentChar == ',' && !insideQuotes) || currentChar == '}')
            {
                var currentIsNull = isNull() && !hasQuotes;
                if (quoted && !currentIsNull)
                {
                    result.Append('"');
                }

                if (currentIsNull)
                {
                    result.Append("null");
                }
                else
                {
                    result.Append(current);
                }

                if (quoted && !currentIsNull)
                {
                    result.Append('"');
                }
                if (currentChar != '}')
                {
                    result.Append(',');
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
                        current.Append("true");
                    }
                    else if (currentChar == 'f')
                    {
                        current.Append("false");
                    }
                    else
                    {
                        current.Append(currentChar);
                    }
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
        result.Append(']');
        return result.ToString();
    }
}