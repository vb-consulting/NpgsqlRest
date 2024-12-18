﻿using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Extensions;

using NpgsqlRest.Defaults;

using static System.Net.Mime.MediaTypeNames;
using static NpgsqlRest.ParameterParser;

using Tuple = (
    NpgsqlRest.Routine routine,
    NpgsqlRest.RoutineEndpoint endpoint,
    NpgsqlRest.IRoutineSourceParameterFormatter formatter
);

using NpgsqlRest.Auth;

namespace NpgsqlRest;

public static class NpgsqlRestMiddlewareExtensions
{
    private static ILogger? logger = null;
    public static ILogger? GetLogger() => logger;
    
    public static IApplicationBuilder UseNpgsqlRest(this IApplicationBuilder builder, NpgsqlRestOptions options)
    {
        if (options.ConnectionString is null && options.DataSource is null && options.ServiceProviderMode == ServiceProviderObject.None)
        {
            throw new ArgumentException("ConnectionString and DataSource are null and ServiceProviderMode is set to None. You must specify connection with connection string, DataSource object or with ServiceProvider");
        }

        if (options.ConnectionString is not null && options.DataSource is not null && options.ServiceProviderMode == ServiceProviderObject.None)
        {
            throw new ArgumentException("Both ConnectionString and DataSource are provided. Please specify only one.");
        }

        if (options.Logger is not null)
        {
            logger = options.Logger;
        }
        else if (builder is WebApplication app)
        {
            var factory = app.Services.GetRequiredService<ILoggerFactory>();
            logger = factory is not null ? factory.CreateLogger(options.LoggerName ?? typeof(NpgsqlRestMiddlewareExtensions).Namespace ?? "NpgsqlRest") : app.Logger;
        }

        var dict = BuildDictionary(builder, options, logger);
        var serviceProvider = builder.ApplicationServices;

        if (dict.Count == 0)
        {
            return builder;
        }

        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();
        builder.Use(async (context, next) =>
        {
            var pathKey = string.Concat(
                context.Request.Method, 
                context.Request.Path.ToString(), '/')
                .AsSpan();
            
            if (!lookup.TryGetValue(pathKey, out var tupleArray))
            {
                if (pathKey[^1] == '/' && pathKey[^2] == '/')
                {
                    if (!lookup.TryGetValue(pathKey[..^2], out tupleArray))
                    {
                        await next(context);
                        return;
                    }
                }
                else if (pathKey[^1] == '/')
                {
                    if (!lookup.TryGetValue(pathKey[..^1], out tupleArray))
                    {
                        await next(context);
                        return;
                    }
                }
            }
            
            JsonObject? jsonObj = null;
            Dictionary<string, JsonNode?> bodyDict = default!;
            string? body = null;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var len = tupleArray.Length;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            
            bool overloaded = len > 1;

            for (var index = len - 1; index >= 0; index--)
            {
                var (routine, endpoint, formatter) = tupleArray[index];
                if (!string.Equals(context.Request.Method, endpoint.Method.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (overloaded)
                {
                    if (endpoint.RequestParamType == RequestParamType.QueryString)
                    {
                        var count = endpoint.BodyParameterName is null ? context.Request.Query.Count : context.Request.Query.Count + 1;
                        if (routine.ParamCount != count)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (jsonObj is null)
                        {
                            await ParseJsonBody();
                            if (jsonObj is null && routine.ParamCount > 0)
                            {
                                continue;
                            }
                        }
                        if (routine.ParamCount != (jsonObj?.Count ?? 0))
                        {
                            continue;
                        }
                    }
                }

                List<NpgsqlRestParameter> paramsList = new(routine.ParamCount + 5);

                bool hasNulls = false;
                NpgsqlRestParameter? headerParam = null;
                if (routine.ParamCount > 0)
                {
                    if (endpoint.RequestParamType == RequestParamType.QueryString)
                    {
                        bool shouldContinue = false;
                        foreach (var key in context.Request.Query.Keys)
                        {
                            if (endpoint.ParamsNameHash.Contains(key) is false)
                            {
                                shouldContinue = true;
                                break;
                            }
                        }
                        if (shouldContinue)
                        {
                            continue;
                        }
                        int setCount = 0;
                        for (var i = 0; i < routine.ParamCount; i++)
                        {
                            string p = endpoint.ParamNames[i];
                            var descriptor = routine.ParamTypeDescriptor[i];
                            var parameter = new NpgsqlRestParameter
                            {
                                NpgsqlDbType = descriptor.ActualDbType,
                                ActualName = routine.ParamNames[i],
                                TypeDescriptor = descriptor
                            };
                            if (endpoint.RequestHeadersMode == RequestHeadersMode.Parameter)
                            {
                                if (string.Equals(p, endpoint.RequestHeadersParameterName, StringComparison.Ordinal) ||
                                    string.Equals(routine.ParamNames[i], endpoint.RequestHeadersParameterName, StringComparison.Ordinal))
                                {
                                    headerParam = parameter;
                                }
                            }

                            if (context.Request.Query.TryGetValue(p, out var qsValue))
                            {
                                if (TryParseParameter(ref qsValue, ref descriptor, ref parameter, endpoint.QueryStringNullHandling))
                                {
                                    if (options.ValidateParameters is not null || options.ValidateParametersAsync is not null)
                                    {
                                        if (options.ValidateParameters is not null)
                                        {
                                            options.ValidateParameters(new(
                                                context,
                                                routine,
                                                parameter,
                                                paramName: p,
                                                typeDescriptor: descriptor,
                                                requestParamType: endpoint.RequestParamType,
                                                queryStringValues: qsValue,
                                                jsonBodyNode: null));
                                        }
                                        if (options.ValidateParametersAsync is not null)
                                        {
                                            await options.ValidateParametersAsync(new(
                                                context,
                                                routine,
                                                parameter,
                                                paramName: p,
                                                typeDescriptor: descriptor,
                                                requestParamType: endpoint.RequestParamType,
                                                queryStringValues: qsValue,
                                                jsonBodyNode: null));
                                        }
                                        if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                        {
                                            return;
                                        }
                                    }
                                    paramsList.Add(parameter);
                                    setCount++;
                                }
                            }
                            else
                            {
                                // set param endpoint.BodyParameterName here
                                if (endpoint.BodyParameterName is not null)
                                {
                                    if (string.Equals(p, endpoint.BodyParameterName, StringComparison.Ordinal) ||
                                        string.Equals(routine.ParamNames[i], endpoint.BodyParameterName, StringComparison.Ordinal))
                                    {
                                        await ParseJsonBody();
                                        if (body is null)
                                        {
                                            parameter.Value = DBNull.Value;
                                        }
                                        else
                                        {
                                            StringValues bodyStringValues = body;
                                            _ = TryParseParameter(ref bodyStringValues, ref descriptor, ref parameter, endpoint.QueryStringNullHandling);
                                        }
                                    }
                                }
                                if (options.ValidateParameters is not null || options.ValidateParametersAsync is not null)
                                {
                                    if (options.ValidateParameters is not null)
                                    {
                                        options.ValidateParameters(new(
                                            context,
                                            routine,
                                            parameter,
                                            paramName: p,
                                            typeDescriptor: descriptor,
                                            requestParamType: endpoint.RequestParamType,
                                            queryStringValues: qsValue,
                                            jsonBodyNode: null));
                                    }
                                    if (options.ValidateParametersAsync is not null)
                                    {
                                        await options.ValidateParametersAsync(new(
                                            context,
                                            routine,
                                            parameter,
                                            paramName: p,
                                            typeDescriptor: descriptor,
                                            requestParamType: endpoint.RequestParamType,
                                            queryStringValues: qsValue,
                                            jsonBodyNode: null));
                                    }
                                    if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                    {
                                        return;
                                    }
                                }

                                if (parameter.Value is not null)
                                {
                                    paramsList.Add(parameter);
                                    setCount++;
                                }
                                else if (descriptor.HasDefault)
                                {
                                    setCount++;
                                }
                            }

                            if (hasNulls is false && parameter?.Value == DBNull.Value)
                            {
                                hasNulls = true;
                            }
                        }

                        if (setCount != routine.ParamCount)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (jsonObj is null)
                        {
                            await ParseJsonBody();
                            if (jsonObj is null)
                            {
                                continue;
                            }
                        }
                        bool shouldContinue = false;
                        foreach (var key in bodyDict.Keys)
                        {
                            if (endpoint.ParamsNameHash.Contains(key) is false)
                            {
                                shouldContinue = true;
                                break;
                            }
                        }
                        if (shouldContinue)
                        {
                            continue;
                        }

                        int setCount = 0;
                        for (var i = 0; i < endpoint.ParamNames.Length; i++)
                        {
                            string p = endpoint.ParamNames[i];
                            var descriptor = routine.ParamTypeDescriptor[i];
                            var parameter = new NpgsqlRestParameter
                            {
                                NpgsqlDbType = descriptor.ActualDbType,
                                ActualName = routine.ParamNames[i],
                                TypeDescriptor = descriptor
                            };
                            if (endpoint.RequestHeadersMode == RequestHeadersMode.Parameter)
                            {
                                if (string.Equals(p, endpoint.RequestHeadersParameterName, StringComparison.Ordinal) ||
                                    string.Equals(routine.ParamNames[i], endpoint.RequestHeadersParameterName, StringComparison.Ordinal))
                                {
                                    headerParam = parameter;
                                }
                            }
                            if (bodyDict.TryGetValue(p, out var value))
                            {
                                if (TryParseParameter(ref value, ref descriptor, ref parameter))
                                {
                                    if (options.ValidateParameters is not null || options.ValidateParametersAsync is not null)
                                    {
                                        if (options.ValidateParameters is not null)
                                        {
                                            options.ValidateParameters(new ParameterValidationValues(
                                                context,
                                                routine,
                                                parameter,
                                                paramName: p,
                                                typeDescriptor: descriptor,
                                                requestParamType: endpoint.RequestParamType,
                                                queryStringValues: null,
                                                jsonBodyNode: value));
                                        }
                                        if (options.ValidateParametersAsync is not null)
                                        {
                                            await options.ValidateParametersAsync(new(
                                                context,
                                                routine,
                                                parameter,
                                                paramName: p,
                                                typeDescriptor: descriptor,
                                                requestParamType: endpoint.RequestParamType,
                                                queryStringValues: null,
                                                jsonBodyNode: value));
                                        }
                                        if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                        {
                                            return;
                                        }
                                    }
                                    paramsList.Add(parameter);
                                    setCount++;
                                }
                            }
                            else
                            {
                                if (options.ValidateParameters is not null || options.ValidateParametersAsync is not null)
                                {
                                    if (options.ValidateParameters is not null)
                                    {
                                        options.ValidateParameters(new ParameterValidationValues(
                                            context,
                                            routine,
                                            parameter,
                                            paramName: p,
                                            typeDescriptor: descriptor,
                                            requestParamType: endpoint.RequestParamType,
                                            queryStringValues: null,
                                            jsonBodyNode: null));
                                    }
                                    if (options.ValidateParametersAsync is not null)
                                    {
                                        await options.ValidateParametersAsync(new ParameterValidationValues(
                                            context,
                                            routine,
                                            parameter,
                                            paramName: p,
                                            typeDescriptor: descriptor,
                                            requestParamType: endpoint.RequestParamType,
                                            queryStringValues: null,
                                            jsonBodyNode: null));
                                    }
                                    if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                    {
                                        return;
                                    }
                                }

                                if (parameter.Value is not null)
                                {
                                    paramsList.Add(parameter);
                                    setCount++;
                                }
                                else if (descriptor.HasDefault)
                                {
                                    setCount++;
                                }
                            }

                            if (hasNulls is false && parameter?.Value == DBNull.Value)
                            {
                                hasNulls = true;
                            }
                        }
                        if (setCount != routine.ParamCount)
                        {
                            continue;
                        }
                    }
                }

                if (endpoint.Login is false)
                {
                    if ((endpoint.RequiresAuthorization || endpoint.AuthorizeRoles is not null) && context.User?.Identity?.IsAuthenticated is false)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        await context.Response.CompleteAsync();
                        return;
                    }

                    if (endpoint.AuthorizeRoles is not null)
                    {
                        bool ok = false;
                        foreach (var claim in context.User?.Claims ?? [])
                        {
                            if (string.Equals(claim.Type, options.AuthenticationOptions.DefaultRoleClaimType, StringComparison.Ordinal))
                            {
                                if (endpoint.AuthorizeRoles.Contains(claim.Value) is true)
                                {
                                    ok = true;
                                    break;
                                }
                            }
                        }
                        if (ok is false)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            await context.Response.CompleteAsync();
                            return;
                        }
                    }
                }

                if (hasNulls && routine.IsStrict)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    await context.Response.CompleteAsync();
                    return;
                }

                string? headers = null;
                if (endpoint.RequestHeadersMode != RequestHeadersMode.Ignore)
                {
                    if (options.CustomRequestHeaders.Count > 0)
                    {
                        foreach (var header in options.CustomRequestHeaders)
                        {
                            context.Request.Headers.Add(header);
                        }
                    }

                    headers = "{";
                    var i = 0;

                    foreach (var header in context.Request.Headers)
                    {
                        if (i++ > 0)
                        {
                            headers = string.Concat(headers, ",");
                        }
                        var key = header.Key;
                        var value = header.Value.ToString();
                        headers = string.Concat(
                            headers,
                            PgConverters.SerializeString(key),
                            ":",
                            PgConverters.SerializeString(value));
                    }
                    headers = string.Concat(headers, "}");
                    if (endpoint.RequestHeadersMode == RequestHeadersMode.Parameter && headerParam is not null)
                    {
                        var headerAdded = false;
                        for (var ip = 0; ip < paramsList.Count; ip++)
                        {
                            var p = paramsList[ip];
                            if (p == headerParam)
                            {
                                //p.Value = headers;
                                headerAdded = true;
                                break;
                            }
                        }
                        if (headerAdded is false)
                        {
                            headerParam.Value = headers;
                            paramsList.Add(headerParam);
                        }
                    }
                }

                NpgsqlConnection? connection = null;
                string? commandText = null;
                bool shouldDispose = true;
                var writer = System.IO.Pipelines.PipeWriter.Create(context.Response.Body);
                try
                {
                    if (options.ServiceProviderMode != ServiceProviderObject.None)
                    {
                        if (options.ServiceProviderMode == ServiceProviderObject.NpgsqlDataSource)
                        {
                            connection = await builder.ApplicationServices.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync();
                        }
                        else if (options.ServiceProviderMode == ServiceProviderObject.NpgsqlConnection)
                        {
                            shouldDispose = false;
                            connection = builder.ApplicationServices.GetRequiredService<NpgsqlConnection>();
                        }
                    }
                    else
                    {
                        if (options.DataSource is not null)
                        {
                            connection = options.DataSource.CreateConnection();
                        }
                        else
                        {
                            connection = new(options.ConnectionString);
                        }
                    }

                    if (connection is null)
                    {
                        continue;
                    }

                    if (options.LogConnectionNoticeEvents && logger != null)
                    {
                        connection.Notice += (sender, args) =>
                        {
                            NpgsqlRestLogger.LogConnectionNotice(ref logger, ref args);
                        };
                    }

                    if (connection.State != ConnectionState.Open)
                    {
                        if (options.BeforeConnectionOpen is not null)
                        {
                            options.BeforeConnectionOpen(connection, routine, endpoint, context);
                        }
                        await connection.OpenAsync();
                    }

                    await using var command = connection.CreateCommand();

                    if (endpoint.RequestHeadersMode == RequestHeadersMode.Context)
                    {
                        command.CommandText = string.Concat("select set_config('request.headers','", headers, "',false)");
                        command.ExecuteNonQuery();
                    }

                    var shouldLog = options.LogCommands && logger != null;
                    StringBuilder? cmdLog = shouldLog ?
                        new(string.Concat("-- ", context.Request.Method, " ", context.Request.GetDisplayUrl(), Environment.NewLine)) :
                        null;

                    int paramCount = paramsList.Count;
                    if (formatter.RefContext)
                    {
                        commandText = formatter.IsFormattable ?
                            formatter.FormatCommand(ref routine, ref paramsList, ref context) :
                            routine.Expression;
                        if (formatter.IsFormattable)
                        {
                            if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        commandText = formatter.IsFormattable ?
                            formatter.FormatCommand(ref routine, ref paramsList) :
                            routine.Expression;
                    }

                    if (paramCount == 0 && formatter.IsFormattable is false)
                    {
                        if (formatter.RefContext)
                        {
                            commandText = string.Concat(commandText, formatter.AppendEmpty(ref context));
                            if (formatter.IsFormattable)
                            {
                                if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            commandText = string.Concat(commandText, formatter.AppendEmpty());
                        }
                    }
                    else
                    {
                        for (var i = 0; i < paramCount; i++)
                        {
                            var parameter = paramsList[i];
                            command.Parameters.Add(parameter);

                            if (formatter.IsFormattable is false)
                            {
                                if (formatter.RefContext)
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(ref parameter, ref i, ref paramCount, ref context));
                                    if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(ref parameter, ref i, ref paramCount));
                                }
                            }

                            if (shouldLog && options.LogCommandParameters)
                            {
                                object value = parameter.NpgsqlValue!;
                                var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                                    "***" :
                                    FormatParam(ref value, ref routine.ParamTypeDescriptor[i]);
                                cmdLog!.AppendLine(string.Concat(
                                    "-- $",
                                    (i + 1).ToString(),
                                    " ", routine.ParamTypeDescriptor[i].OriginalType,
                                    " = ",
                                    p));
                            }

                        } // end for parameters
                    }
                    if (commandText is null)
                    {
                        await next(context);
                        return;
                    }
                    command.CommandText = commandText;

                    if (shouldLog)
                    {
                        NpgsqlRestLogger.LogEndpoint(ref logger, ref endpoint, cmdLog?.ToString() ?? "", command.CommandText);
                    }

                    if (endpoint.CommandTimeout.HasValue)
                    {
                        command.CommandTimeout = endpoint.CommandTimeout.Value;
                    }

                    //command callback
                    if (options.CommandCallbackAsync is not null)
                    {
                        await options.CommandCallbackAsync((routine, command, context));
                        if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                        {
                            return;
                        }
                    }

                    if (endpoint.Login is true)
                    {
                        await AuthHandler.HandleLoginAsync(command, context, endpoint, routine, options, logger);
                        if (context.Response.HasStarted is true || options.AuthenticationOptions.SerializeAuthEndpointsResponse is false)
                        {
                            return;
                        }
                    }

                    if (endpoint.Logout is true)
                    {
                        await AuthHandler.HandleLogoutAsync(command, routine, context);
                        return;
                    }

                    if (routine.IsVoid)
                    {
                        await command.ExecuteNonQueryAsync();
                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        return;
                    }
                    else // end if (routine.IsVoid)
                    {
                        command.AllResultTypesAreUnknown = true;

                        await using var reader = await command.ExecuteReaderAsync();
                        if (routine.ReturnsSet == false && routine.ColumnCount == 1 && routine.ReturnsRecordType is false)
                        {
                            if (await reader.ReadAsync())
                            {
                                string? value = reader.GetValue(0) as string;
                                TypeDescriptor descriptor = routine.ColumnsTypeDescriptor[0];
                                if (endpoint.ResponseContentType is not null)
                                {
                                    context.Response.ContentType = endpoint.NeedsParsing ?
                                        PgConverters.ParseParameters(ref paramsList, endpoint.ResponseContentType) : 
                                        endpoint.ResponseContentType;

                                }
                                else if (descriptor.IsJson || descriptor.IsArray)
                                {
                                    context.Response.ContentType = Application.Json;
                                }
                                else
                                {
                                    context.Response.ContentType = Text.Plain;
                                }
                                if (endpoint.ResponseHeaders.Count > 0)
                                {
                                    foreach ((string headerKey, StringValues headerValue) in endpoint.ResponseHeaders)
                                    {
                                        context.Response.Headers.Append(headerKey, 
                                            endpoint.NeedsParsing ? 
                                            PgConverters.ParseParameters(ref paramsList, headerValue.ToString()) : headerValue);
                                    }
                                }

                                // if raw
                                if (endpoint.Raw)
                                {
                                    writer.Advance(Encoding.UTF8.GetBytes((value ?? "").AsSpan(), writer.GetSpan(Encoding.UTF8.GetMaxByteCount((value ?? "").Length))));
                                }
                                else
                                {
                                    if (descriptor.IsArray && value is not null)
                                    {
                                        value = PgConverters.PgArrayToJsonArray(value.AsSpan(), descriptor).ToString();
                                    }
                                    if (value is not null)
                                    {
                                        writer.Advance(Encoding.UTF8.GetBytes(value.AsSpan(), writer.GetSpan(Encoding.UTF8.GetMaxByteCount(value.Length))));
                                    }
                                    else
                                    {
                                        if (endpoint.TextResponseNullHandling == TextResponseNullHandling.NullLiteral)
                                        {
                                            writer.Advance(Encoding.UTF8.GetBytes(Consts.Null.AsSpan(), writer.GetSpan(Encoding.UTF8.GetMaxByteCount(Consts.Null.Length))));
                                        }
                                        else if (endpoint.TextResponseNullHandling == TextResponseNullHandling.NoContent)
                                        {
                                            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                                        }
                                        // else OK empty string
                                    }
                                }
                                return;
                            }
                            else
                            {
                                logger?.CouldNotReadCommand(command.CommandText, context.Request.Method, context.Request.Path);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                return;
                            }
                        }
                        else // end if (routine.ReturnsRecord == false)
                        {
                            if (endpoint.ResponseContentType is not null)
                            {
                                context.Response.ContentType = endpoint.NeedsParsing ?
                                    PgConverters.ParseParameters(ref paramsList, endpoint.ResponseContentType) :
                                    endpoint.ResponseContentType;
                            }
                            else
                            {
                                context.Response.ContentType = Application.Json;
                            }
                            if (endpoint.ResponseHeaders.Count > 0)
                            {
                                foreach (var (headerKey, headerValue) in endpoint.ResponseHeaders)
                                {
                                    context.Response.Headers.Append(headerKey,
                                        endpoint.NeedsParsing ?
                                        PgConverters.ParseParameters(ref paramsList, headerValue.ToString()) : headerValue);
                                }
                            }

                            if (routine.ReturnsSet && endpoint.Raw is false)
                            {
                                writer.Advance(Encoding.UTF8.GetBytes(Consts.OpenBracket.ToString().AsSpan(), writer.GetSpan(Encoding.UTF8.GetMaxByteCount(1))));
                            }
                            bool first = true;
                            var routineReturnRecordCount = routine.ColumnCount;

                            StringBuilder row = new();
                            ulong rowCount = 0;

                            if (endpoint.Raw is true && endpoint.RawColumnNames is true)
                            {
                                StringBuilder columns = new();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (endpoint.RawValueSeparator is not null && i > 0)
                                    {
                                        columns.Append(endpoint.RawValueSeparator);
                                    }
                                    columns.Append(PgConverters.QuoteText(reader.GetName(i).AsSpan()));
                                }
                                if (endpoint.RawNewLineSeparator is not null)
                                {
                                    columns.Append(endpoint.RawNewLineSeparator);
                                }
                                row.Append(columns);
                            }

                            var bufferRows = endpoint.BufferRows ?? options.BufferRows;
                            while (await reader.ReadAsync())
                            {
                                rowCount++;
                                if (!first)
                                {
                                    // if raw
                                    if (endpoint.Raw is false)
                                    {
                                        row.Append(Consts.Comma);
                                    }
                                    else if (endpoint.RawNewLineSeparator is not null)
                                    {
                                        row.Append(endpoint.RawNewLineSeparator);
                                    }
                                }
                                else
                                {
                                    first = false;
                                }

                                for (var i = 0; i < routineReturnRecordCount; i++)
                                {
                                    object value = reader.GetValue(i);
                                    // AllResultTypesAreUnknown = true always returns string, except for null
                                    var raw = (value == DBNull.Value ? "" : (string)value).AsSpan();

                                    // if raw
                                    if (endpoint.Raw)
                                    {
                                        if (endpoint.RawValueSeparator is not null)
                                        {
                                            var descriptor = routine.ColumnsTypeDescriptor[i];
                                            if (descriptor.IsText || descriptor.IsDate || descriptor.IsDateTime)
                                            {
                                                row.Append(PgConverters.QuoteText(raw));
                                            }
                                            else
                                            {
                                                row.Append(raw);
                                            }
                                            if (i < routineReturnRecordCount - 1)
                                            {
                                                row.Append(endpoint.RawValueSeparator);
                                            }
                                        }
                                        else
                                        {
                                            row.Append(raw);
                                        }
                                    }
                                    else
                                    {
                                        if (routine.ReturnsUnnamedSet == false)
                                        {
                                            if (i == 0)
                                            {
                                                row.Append(Consts.OpenBrace);
                                            }
                                            row.Append(Consts.DoubleQuote);
                                            row.Append(endpoint.ColumnNames[i]);
                                            row.Append(Consts.DoubleQuoteColon);
                                        }

                                        var descriptor = routine.ColumnsTypeDescriptor[i];
                                        if (value == DBNull.Value)
                                        {
                                            row.Append(Consts.Null);
                                        }
                                        else if (descriptor.IsArray && value is not null)
                                        {
                                            row.Append(PgConverters.PgArrayToJsonArray(raw, descriptor));
                                        }
                                        else if ((descriptor.IsNumeric || descriptor.IsBoolean || descriptor.IsJson) && value is not null)
                                        {
                                            if (descriptor.IsBoolean)
                                            {
                                                if (raw.Length == 1 && raw[0] == 't')
                                                {
                                                    row.Append(Consts.True);
                                                }
                                                else if (raw.Length == 1 && raw[0] == 'f')
                                                {
                                                    row.Append(Consts.False);
                                                }
                                                else
                                                {
                                                    row.Append(raw);
                                                }
                                            }
                                            else
                                            {
                                                // numeric and json
                                                row.Append(raw);
                                            }
                                        }
                                        else
                                        {
                                            if (descriptor.ActualDbType == NpgsqlDbType.Unknown)
                                            {
                                                row.Append(PgConverters.PgUnknownToJsonArray(raw));
                                            }
                                            else if (descriptor.NeedsEscape)
                                            {
                                                row.Append(PgConverters.SerializeString(raw));
                                            }
                                            else
                                            {
                                                if (descriptor.IsDateTime)
                                                {
                                                    row.Append(PgConverters.QuoteDateTime(raw));
                                                }
                                                else
                                                {
                                                    row.Append(PgConverters.Quote(raw));
                                                }
                                            }
                                        }
                                        if (routine.ReturnsUnnamedSet == false && i == routine.ColumnCount - 1)
                                        {
                                            row.Append(Consts.CloseBrace);
                                        }
                                        if (i < routine.ColumnCount - 1)
                                        {
                                            row.Append(Consts.Comma);
                                        }
                                    }
                                } // end for

                                if (bufferRows != 1 && rowCount % bufferRows == 0)
                                {
                                    foreach (ReadOnlyMemory<char> chunk in row.GetChunks())
                                    {
                                        var buffer = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(chunk.Length));
                                        int bytesWritten = Encoding.UTF8.GetBytes(chunk.Span, buffer);
                                        writer.Advance(bytesWritten);
                                    }
                                    await writer.FlushAsync();
                                    row.Clear();
                                }
                            } // end while

                            if (row.Length > 0)
                            {
                                foreach (ReadOnlyMemory<char> chunk in row.GetChunks())
                                {
                                    var buffer = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(chunk.Length));
                                    int bytesWritten = Encoding.UTF8.GetBytes(chunk.Span, buffer);
                                    writer.Advance(bytesWritten);
                                }
                                await writer.FlushAsync();
                            }
                            if (routine.ReturnsSet && endpoint.Raw is false)
                            {
                                writer.Advance(Encoding.UTF8.GetBytes(Consts.CloseBracket.ToString().AsSpan(), writer.GetSpan(Encoding.UTF8.GetMaxByteCount(1))));
                            }
                            return;
                        } // end if (routine.ReturnsRecord == true)
                    } // end if (routine.IsVoid is false)
                }
                catch (NpgsqlException exception)
                {
                    if (options.PostgreSqlErrorCodeToHttpStatusCodeMapping.TryGetValue(exception.SqlState ?? "", out var code))
                    {
                        context.Response.StatusCode = code;
                    }
                    if (options.ReturnNpgsqlExceptionMessage && context.Response.HasStarted is false)
                    {
                        if (context.Response.StatusCode == 200)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                        if (context.Response.StatusCode != 205) // 205 forbids writing
                        {
                            writer.Advance(Encoding.UTF8.GetBytes(exception.Message.AsSpan(), writer.GetSpan(Encoding.UTF8.GetMaxByteCount(exception.Message.Length))));
                        }
                    }
                    if (context.Response.StatusCode != 200 || context.Response.HasStarted)
                    {
                        logger?.LogError(exception, "Error executing command: {commandText} mapped to endpoint: {Url}", commandText, endpoint.Url);
                        return;
                    }
                }
                finally
                {
                    await writer.CompleteAsync();
                    await context.Response.CompleteAsync();
                    if (connection is not null && shouldDispose is true)
                    {
                        await connection.DisposeAsync();
                    }
                }
            }

            await next(context);
            return;

            async Task ParseJsonBody()
            {
                if (jsonObj is null)
                {
                    context.Request.EnableBuffering();
                    context.Request.Body.Position = 0;
                    if (body is null)
                    {
                        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                        body = await reader.ReadToEndAsync();
                    }
                    JsonNode? node;
                    try
                    {
                        node = JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                    }
                    catch (JsonException)
                    {
                        return;
                    }
                    try
                    {
                        jsonObj = node?.AsObject();
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        bodyDict = jsonObj?.ToDictionary();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                    }
                    catch (Exception e)
                    {
                        logger?.CouldNotParseJson(body, context.Request.Path, e.Message);
                    }
                }
            }
        });

        return builder;
    }

    private static Dictionary<string, Tuple[]> BuildDictionary(
        IApplicationBuilder builder,
        NpgsqlRestOptions options,
        ILogger? logger)
    {
        var hasLogin = false;
        var dict = new Dictionary<string, List<Tuple>>();
        foreach (var handler in options.EndpointCreateHandlers)
        {
            handler.Setup(builder, logger, options);
        }

        options.SourcesCreated(options.RoutineSources);

        CommentsMode optionsCommentsMode = options.CommentsMode;
        foreach (var source in options.RoutineSources)
        {
            if (source.CommentsMode.HasValue)
            {
                options.CommentsMode = source.CommentsMode.Value;
            }
            else
            {
                options.CommentsMode = optionsCommentsMode;
            }
            foreach (var (routine, formatter) in source.Read(options))
            {
                RoutineEndpoint? result = DefaultEndpoint.Create(routine, options, logger);

                if (result is null)
                {
                    continue;
                }

                if (options.EndpointCreated is not null)
                {
                    result = options.EndpointCreated(routine, result.Value);
                }

                if (result is null)
                {
                    continue;
                }

                RoutineEndpoint endpoint = result.Value;
                var method = endpoint.Method.ToString();
                if (endpoint.BodyParameterName is not null && endpoint.RequestParamType == RequestParamType.BodyJson)
                {
                    endpoint = endpoint with { RequestParamType = RequestParamType.QueryString };
                    logger?.EndpointTypeChanged(method, endpoint.Url, endpoint.BodyParameterName);
                }
                var key = string.Concat(method, endpoint.Url);
                List<Tuple> list = dict.TryGetValue(key, out var value) ? value : [];
                list.Add((routine, endpoint, formatter));
                dict[key] = list;

                foreach (var handler in options.EndpointCreateHandlers)
                {
                    handler.Handle(routine, endpoint);
                }

                if (options.LogEndpointCreatedInfo)
                {
                    logger?.EndpointCreated(method, endpoint.Url);
                }

                if (endpoint.Login is true)
                {
                    if (hasLogin is false)
                    {
                        hasLogin = true;
                    }
                    if (routine.IsVoid is true || routine.ReturnsUnnamedSet is true)
                    {
                        throw new ArgumentException($"{routine.Type.ToString().ToLowerInvariant()} {routine.Schema}.{routine.Name} is marked as login and it can't be void or returning unnamed data sets.");
                    }
                }
            }
        }

        if (hasLogin is true)
        {
            if (options.AuthenticationOptions.DefaultAuthenticationType is null)
            {
                string db = new NpgsqlConnectionStringBuilder(options.ConnectionString).Database ?? "NpgsqlRest";
                options.AuthenticationOptions.DefaultAuthenticationType = db;
                logger?.SetDefaultAuthenticationType(db);
            }
        }

        if (options.EndpointsCreated is not null)
        {
            options.EndpointsCreated(dict.Values.SelectMany(x => x).Select(x => (x.routine, x.endpoint)).ToArray());
        }

        (Routine routine, RoutineEndpoint endpoint)[]? array = null;
        foreach (var handler in options.EndpointCreateHandlers)
        {
            array ??= dict.Values.SelectMany(x => x).Select(x => (x.routine, x.endpoint)).ToArray();
            handler.Cleanup(ref array);
            handler.Cleanup();
        }

        return dict
            .ToDictionary(
                x => x.Key,
                x => x.Value.ToArray());
    }
}