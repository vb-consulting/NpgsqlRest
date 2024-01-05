using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;

namespace NpgsqlRest;

internal class CommandParameters
{
    internal static bool TryCreateCmdParameter(
        ref string? value, 
        ref TypeDescriptor descriptor, 
        ref NpgsqlRestOptions options, 
        out NpgsqlParameter result)
    {
        result = new NpgsqlParameter
        {
            NpgsqlDbType = descriptor.DbType
        };

        if (options.QueryStringParameterParserCallback is not null)
        {
            var callback = options.QueryStringParameterParserCallback(value, descriptor, result);
            if (callback is not null)
            {
                result = callback;
                return true;
            }
        }

        if (value is null)
        {
            result.Value = DBNull.Value;
            return true;
        }

        if (descriptor.DbType == NpgsqlDbType.Smallint)
        {
            if (short.TryParse(value, out var shortValue))
            {
                result.Value = shortValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Integer)
        {
            if (int.TryParse(value, out var intValue))
            {
                result.Value = intValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Bigint)
        {
            if (long.TryParse(value, out var bigValue))
            {
                result.Value = bigValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Double)
        {
            if (double.TryParse(value, out var doubleValue))
            {
                result.Value = doubleValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Real)
        {
            if (float.TryParse(value, out var floatValue))
            {
                result.Value = floatValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Numeric || descriptor.DbType == NpgsqlDbType.Money)
        {
            if (decimal.TryParse(value, out var decimalValue))
            {
                result.Value = decimalValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Boolean)
        {
            if (bool.TryParse(value, out var boolValue))
            {
                result.Value = boolValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Timestamp || descriptor.DbType == NpgsqlDbType.TimestampTz)
        {
            if (DateTime.TryParse(value, out var dateTimeValue))
            {
                result.Value = dateTimeValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Date)
        {
            if (DateOnly.TryParse(value, out var dateTimeValue))
            {
                result.Value = dateTimeValue;
                return true;
            }
            else
            {
                return false;
            }
        }


        else if (descriptor.DbType == NpgsqlDbType.Time || descriptor.DbType == NpgsqlDbType.TimeTz)
        {
            if (TimeOnly.TryParse(value, out var dateTimeValue))
            {
                result.Value = dateTimeValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Uuid)
        {
            if (Guid.TryParse(value, out var guidValue))
            {
                result.Value = guidValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.IsText)
        {
            result.Value = value;
            return true;
        }

        // for all other cases, use raw string
        result.Value = value;
        return true;
    }

    internal static bool TryCreateCmdParameter(
        ref JsonNode? value, 
        ref TypeDescriptor descriptor, 
        ref NpgsqlRestOptions options, 
        out NpgsqlParameter result)
    {
        result = new NpgsqlParameter
        {
            NpgsqlDbType = descriptor.DbType
        };

        if (options.JsonBodyParameterParserCallback is not null)
        {
            var callback = options.JsonBodyParameterParserCallback(value, descriptor, result);
            if (callback is not null)
            {
                result = callback;
                return true;
            }
        }

        if (value is null)
        {
            result.Value = DBNull.Value;
            return true;
        }

        var kind = value.GetValueKind();
        if (kind == JsonValueKind.Null)
        {
            result.Value = DBNull.Value;
            return true;
        }

        try
        {
            if (kind == JsonValueKind.Number)
            {
                if (descriptor.DbType == NpgsqlDbType.Smallint)
                {
                    result.Value = value.GetValue<short>();
                    return true;
                }
                else if (descriptor.DbType == NpgsqlDbType.Integer)
                {
                    result.Value = value.GetValue<int>();
                    return true;
                }
                else if (descriptor.DbType == NpgsqlDbType.Bigint)
                {
                    result.Value = value.GetValue<long>();
                    return true;
                }
                else if (descriptor.DbType == NpgsqlDbType.Double)
                {
                    result.Value = value.GetValue<double>();
                    return true;
                }
                else if (descriptor.DbType == NpgsqlDbType.Real)
                {
                    result.Value = value.GetValue<float>();
                    return true;
                }
                else if (descriptor.DbType == NpgsqlDbType.Numeric || descriptor.DbType == NpgsqlDbType.Money)
                {
                    result.Value = value.GetValue<decimal>();
                    return true;
                }
                return false;
            }
            if ((kind == JsonValueKind.True || kind == JsonValueKind.False) && descriptor.DbType == NpgsqlDbType.Boolean)
            {
                result.Value = value.GetValue<bool>();
                return true;
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException)
        {
            return false;
        }

        var content = value.ToString();

        if (descriptor.DbType == NpgsqlDbType.Timestamp || descriptor.DbType == NpgsqlDbType.TimestampTz)
        {
            if (DateTime.TryParse(content, out var dateTimeValue))
            {
                result.Value = dateTimeValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Date)
        {
            if (DateOnly.TryParse(content, out var dateTimeValue))
            {
                result.Value = dateTimeValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Time || descriptor.DbType == NpgsqlDbType.TimeTz)
        {
            if (TimeOnly.TryParse(content, out var dateTimeValue))
            {
                result.Value = dateTimeValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (descriptor.DbType == NpgsqlDbType.Uuid)
        {
            if (Guid.TryParse(content, out var guidValue))
            {
                result.Value = guidValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        result.Value = content;
        return true;
    }
}
