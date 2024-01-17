﻿using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace NpgsqlRest;

internal class ParameterParser
{
    internal static bool TryParseParameter(
        ref StringValues values, 
        ref TypeDescriptor descriptor,
        ref NpgsqlParameter parameter)
    {
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
            else if (descriptor.BaseDbType == NpgsqlDbType.Smallint)
            {
                if (short.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var shortValue))
                {
                    resultValue = shortValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Integer)
            {
                if (int.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var intValue))
                {
                    resultValue = intValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Bigint)
            {
                if (long.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var bigValue))
                {
                    resultValue = bigValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Double)
            {
                if (double.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var doubleValue))
                {
                    resultValue = doubleValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Real)
            {
                if (float.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var floatValue))
                {
                    resultValue = floatValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Numeric || descriptor.BaseDbType == NpgsqlDbType.Money)
            {
                if (decimal.TryParse(value, CultureInfo.InvariantCulture.NumberFormat, out var decimalValue))
                {
                    resultValue = decimalValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Boolean)
            {
                if (bool.TryParse(value, out var boolValue))
                {
                    resultValue = boolValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Timestamp || descriptor.BaseDbType == NpgsqlDbType.TimestampTz)
            {
                if (DateTime.TryParse(value, out var dateTimeValue))
                {
                    if (descriptor.BaseDbType == NpgsqlDbType.TimestampTz)
                    {
                        resultValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc);
                    }
                    else
                    {
                        resultValue = dateTimeValue;
                    }
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Date)
            {
                if (DateOnly.TryParse(value, out var dateTimeValue))
                {
                    resultValue = dateTimeValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Time || descriptor.BaseDbType == NpgsqlDbType.TimeTz)
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
                    };
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Uuid)
            {
                if (Guid.TryParse(value, out var guidValue))
                {
                    resultValue = guidValue;
                    return true;
                }
                else
                {
                    resultValue = null!;
                    return false;
                }
            }
            else if (descriptor.IsText)
            {
                resultValue = value;
                return true;
            }

            // for all other cases, use raw string
            resultValue = value;
            return true;
        }

        if (descriptor.IsArray == false)
        {
            if (values.Count == 0)
            {
                parameter.Value = DBNull.Value;
                return true;
            }
            else if (values.Count == 1)
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
        else
        {
            if (values.Count == 0)
            {
                parameter.Value = DBNull.Value;
                return true;
            }
            else
            {
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
            }
        }
    }

    internal static bool TryParseParameter(
        ref JsonNode? value, 
        ref TypeDescriptor descriptor,
        ref NpgsqlParameter parameter)
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
                    else if (descriptor.BaseDbType == NpgsqlDbType.Integer)
                    {
                        result = value.GetValue<int>();
                        return true;
                    }
                    else if (descriptor.BaseDbType == NpgsqlDbType.Bigint)
                    {
                        result = value.GetValue<long>();
                        return true;
                    }
                    else if (descriptor.BaseDbType == NpgsqlDbType.Double)
                    {
                        result = value.GetValue<double>();
                        return true;
                    }
                    else if (descriptor.BaseDbType == NpgsqlDbType.Real)
                    {
                        result = value.GetValue<float>();
                        return true;
                    }
                    else if (descriptor.BaseDbType == NpgsqlDbType.Numeric || descriptor.BaseDbType == NpgsqlDbType.Money)
                    {
                        result = value.GetValue<decimal>();
                        return true;
                    }
                }
                if ((valueKind == JsonValueKind.True || valueKind == JsonValueKind.False) && descriptor.BaseDbType == NpgsqlDbType.Boolean)
                {
                    result = value.GetValue<bool>();
                    return true;
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException)
            {
            }
            result = null!;
            return false;
        }

        bool TryGetNonStringValueFromString(
            ref TypeDescriptor descriptor, 
            ref string content, 
            out object result)
        {
            if (descriptor.BaseDbType == NpgsqlDbType.Timestamp || descriptor.BaseDbType == NpgsqlDbType.TimestampTz)
            {
                if (DateTime.TryParse(content, out var dateTimeValue))
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
                if (DateOnly.TryParse(content, out var dateTimeValue))
                {
                    result = dateTimeValue;
                    return true;
                }
            }
            else if (descriptor.BaseDbType == NpgsqlDbType.Time || descriptor.BaseDbType == NpgsqlDbType.TimeTz)
            {
                if (DateTime.TryParse(content, out var dateTimeValue))
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
                if (Guid.TryParse(content, out var guidValue))
                {
                    result = guidValue;
                    return true;
                }
            }
            result = null!;
            return false;
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
    }
}