using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace NpgsqlRest;

internal static class ParameterParser
{
    internal static bool TryParseParameter(
        ref StringValues values, 
        ref TypeDescriptor descriptor,
        ref NpgsqlRestParameter parameter)
    {
        if (descriptor.IsArray == false)
        {
            if (values.Count == 0)
            {
                parameter.Value = DBNull.Value;
                return true;
            }
            if (values.Count == 1)
            {
                var value = values[0];
                if (TryGetValue(ref descriptor, ref value, out var resultValue))
                {
                    parameter.Value = resultValue;
                    return true;
                }
                return false;
            }
            else
            {
                StringBuilder sb = new();
                for (var i = 0; i < values.Count; i++)
                {
                    sb.Append(values[i]);
                }
                var value = sb.ToString();
                if (TryGetValue(ref descriptor, ref value, out var resultValue))
                {
                    parameter.Value = resultValue;
                    return true;
                }
                return false;
            }
        }

        if (values.Count == 0)
        {
            parameter.Value = DBNull.Value;
            return true;
        }

        var list = new List<object?>(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (TryGetValue(ref descriptor, ref value, out var resultValue))
            {
                list.Add(resultValue);
            }
            else
            {
                return false;
            }
        }
        parameter.Value = list;
        return true;

        static bool TryGetValue(
            ref TypeDescriptor descriptor, 
            ref string? value, 
            out object? resultValue)
        {
            if (!descriptor.IsText && string.IsNullOrEmpty(value))
            {
                resultValue = DBNull.Value;
                return true;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Smallint)
            {
                if (short.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var shortValue))
                {
                    resultValue = shortValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Integer)
            {
                if (int.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var intValue))
                {
                    resultValue = intValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Bigint)
            {
                if (long.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var bigValue))
                {
                    resultValue = bigValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Double)
            {
                if (double.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var doubleValue))
                {
                    resultValue = doubleValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Real)
            {
                if (float.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var floatValue))
                {
                    resultValue = floatValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Numeric || descriptor.BaseDbType == NpgsqlDbType.Money)
            {
                if (decimal.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var decimalValue))
                {
                    resultValue = decimalValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Boolean)
            {
                if (bool.TryParse(value, out var boolValue))
                {
                    resultValue = boolValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Timestamp || descriptor.BaseDbType == NpgsqlDbType.TimestampTz)
            {
                if (DateTime.TryParse(value, out var dateTimeValue))
                {
                    resultValue = descriptor.BaseDbType == NpgsqlDbType.TimestampTz ? DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc) : dateTimeValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Date)
            {
                if (DateOnly.TryParse(value, out var dateTimeValue))
                {
                    resultValue = dateTimeValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }

            if (descriptor.BaseDbType == NpgsqlDbType.Time || descriptor.BaseDbType == NpgsqlDbType.TimeTz)
            {
                if (DateTime.TryParse(value, out var dateTimeValue))
                {
                    if (descriptor.BaseDbType == NpgsqlDbType.TimeTz)
                    {
                        resultValue = new DateTimeOffset(DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc));
                    }
                    else
                    {
                        resultValue = TimeOnly.FromDateTime(dateTimeValue);
                    }
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.BaseDbType == NpgsqlDbType.Uuid)
            {
                if (Guid.TryParse(value, out var guidValue))
                {
                    resultValue = guidValue;
                    return true;
                }
                resultValue = null!;
                return false;
            }
            if (descriptor.IsText)
            {
                resultValue = value;
                return true;
            }

            // for all other cases, use raw string
            resultValue = value;
            return true;
        }
    }

    internal static bool TryParseParameter(
        ref JsonNode? value, 
        ref TypeDescriptor descriptor,
        ref NpgsqlRestParameter parameter)
    {
        if (value is null)
        {
            parameter.Value = DBNull.Value;
            return true;
        }

        JsonValueKind kind = value.GetValueKind();
        if (kind == JsonValueKind.Null)
        {
            parameter.Value = DBNull.Value;
            return true;
        }

        if (TryGetNonStringValue(ref descriptor, ref value, ref kind, out var nonStringValue))
        {
            parameter.Value = nonStringValue;
            return true;
        }

        if (kind == JsonValueKind.Array)
        {
            var list = new List<object?>();
            JsonArray array = value.AsArray();
            for (var i = 0; i < array.Count; i++)
            {
                var arrayItem = array[i];
                if (arrayItem is null)
                {
                    list.Add(null);
                    continue;
                }
                var arrayItemKind = arrayItem.GetValueKind();
                if (arrayItemKind == JsonValueKind.Null)
                {
                    list.Add(null);
                    continue;
                }
                if (TryGetNonStringValue(ref descriptor, ref arrayItem, ref arrayItemKind, out object arrayValue))
                {
                    list.Add(arrayValue);
                    continue;
                }

                var arrayItemContent = arrayItem.ToString();
                if (TryGetNonStringValueFromString(ref descriptor, ref arrayItemContent, out arrayValue))
                {
                    list.Add(arrayValue);
                    continue;
                }
                list.Add(arrayItemContent);
            }
            parameter.Value = list;
            return true;
        }

        var content = value.ToString();
        if (TryGetNonStringValueFromString(ref descriptor, ref content, out nonStringValue))
        {
            parameter.Value = nonStringValue;
            return true;
        }

        parameter.Value = content;
        return true;

        bool TryGetNonStringValue(
            ref TypeDescriptor descriptor, 
            ref JsonNode value, 
            ref JsonValueKind valueKind, 
            out object result)
        {
            try
            {
                if (valueKind == JsonValueKind.Number)
                {
                    if (descriptor.BaseDbType == NpgsqlDbType.Smallint)
                    {
                        result = value.GetValue<short>();
                        return true;
                    }

                    if (descriptor.BaseDbType == NpgsqlDbType.Integer)
                    {
                        result = value.GetValue<int>();
                        return true;
                    }

                    if (descriptor.BaseDbType == NpgsqlDbType.Bigint)
                    {
                        result = value.GetValue<long>();
                        return true;
                    }

                    if (descriptor.BaseDbType == NpgsqlDbType.Double)
                    {
                        result = value.GetValue<double>();
                        return true;
                    }

                    if (descriptor.BaseDbType == NpgsqlDbType.Real)
                    {
                        result = value.GetValue<float>();
                        return true;
                    }

                    if (descriptor.BaseDbType is NpgsqlDbType.Numeric or NpgsqlDbType.Money)
                    {
                        result = value.GetValue<decimal>();
                        return true;
                    }
                }
                if (valueKind is JsonValueKind.True or JsonValueKind.False && descriptor.BaseDbType == NpgsqlDbType.Boolean)
                {
                    result = value.GetValue<bool>();
                    return true;
                }
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException)
            {
            }
            result = null!;
            return false;
        }

        bool TryGetNonStringValueFromString(
            ref TypeDescriptor descriptor, 
            ref string nonStrContent, 
            out object result)
        {
            if (descriptor.BaseDbType is NpgsqlDbType.Timestamp or NpgsqlDbType.TimestampTz)
            {
                if (DateTime.TryParse(nonStrContent, out var dateTimeValue))
                {
                    if (descriptor.BaseDbType == NpgsqlDbType.TimestampTz)
                    {
                        result = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc);
                    }
                    else
                    {
                        result = dateTimeValue;
                    }
                    return true;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Date)
            {
                if (DateOnly.TryParse(nonStrContent, out var dateTimeValue))
                {
                    result = dateTimeValue;
                    return true;
                }
            }
            else if (descriptor.BaseDbType is NpgsqlDbType.Time or NpgsqlDbType.TimeTz)
            {
                if (DateTime.TryParse(nonStrContent, out var dateTimeValue))
                {
                    if (descriptor.BaseDbType == NpgsqlDbType.TimeTz)
                    {
                        result = new DateTimeOffset(DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc));
                    }
                    else
                    {
                        result = TimeOnly.FromDateTime(dateTimeValue);
                    };
                    return true;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Uuid)
            {
                if (Guid.TryParse(nonStrContent, out var guidValue))
                {
                    result = guidValue;
                    return true;
                }
            }
            result = null!;
            return false;
        }
    }

    internal static string FormatParam(ref object value, ref TypeDescriptor descriptor)
    {
        if (value == DBNull.Value)
        {
            return "null";
        }
        if (descriptor.IsArray && value is IList<object> list)
        {
            var d = descriptor;
            if (descriptor is { IsNumeric: false, IsBoolean: false })
            {
                return string.Concat("{", string.Join(",", list.Select(x => string.Concat("'", Format(ref x, ref d), "'"))), "}");
            }
            return string.Concat("{", string.Join(",", list.Select(x => Format(ref x, ref d))), "}");
        }
        if (descriptor is { IsNumeric: false, IsBoolean: false })
        {
            return string.Concat("'", Format(ref value, ref descriptor), "'");
        }

        return Format(ref value, ref descriptor);

        string Format(ref object v, ref TypeDescriptor descriptor)
        {
            if (v is DateTime dt)
            {
                if (descriptor.BaseDbType == NpgsqlDbType.TimestampTz)
                {
                    return dt.ToString("O");
                }
                return dt.ToString("s");
            }
            if (v is DateOnly dateOnly)
            {
                return dateOnly.ToString("O");
            }
            if (v is DateTimeOffset dto)
            {
                return dto.DateTime.ToString("T");
            }
            
            if (descriptor.IsBoolean)
            {
                return v.ToString()?.ToLowerInvariant() ?? string.Empty;
            }
            return v.ToString() ?? string.Empty;
        }
    }
}
