﻿using System.Collections.Frozen;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Extensions;
using static System.Net.Mime.MediaTypeNames;

using Tuple = (
    NpgsqlRest.Routine routine,
    NpgsqlRest.RoutineEndpoint endpoint,
    NpgsqlRest.IRoutineSourceParameterFormatter formatter
);
using NpgsqlRest.Defaults;

namespace NpgsqlRest;

public static class NpgsqlRestMiddlewareExtensions
{
    public static IApplicationBuilder UseNpgsqlRest(this IApplicationBuilder builder, NpgsqlRestOptions options)
    {
        if (options.ConnectionString is null && options.ConnectionFromServiceProvider is false)
        {
            throw new ArgumentException("Connection string is null and ConnectionFromServiceProvider is false. Set the connection string or use ConnectionFromServiceProvider");
        }
        
        ILogger? logger = null;
        if (options.Logger is not null)
        {
            logger = options.Logger;
        }
        else if (builder is WebApplication app)
        {
            var factory = app.Services.GetRequiredService<ILoggerFactory>();
            if (factory is not null)
            {
                logger = factory.CreateLogger(options.LoggerName ?? typeof(NpgsqlRestMiddlewareExtensions).Namespace ?? "NpgsqlRest");
            }
            else
            {
                logger = app.Logger;
            }
        }

        var dict = BuildDictionary(builder, options, logger);
        var serviceProvider = builder.ApplicationServices;

        builder.Use(async (context, next) =>
        {
            var path = context.Request.Path.ToString();
            if (!dict.TryGetValue(string.Concat(context.Request.Method, path), out Tuple[]? tupleArray))
            {
                if (path.EndsWith('/'))
                {
                    if (!dict.TryGetValue(string.Concat(context.Request.Method, path[..^1]), out tupleArray))
                    {
                        await next(context);
                        return;
                    }
                }
                else
                {
                    if (!dict.TryGetValue(string.Concat(context.Request.Method, path, '/'), out tupleArray))
                    {
                        await next(context);
                        return;
                    }
                }
            }

            JsonObject? jsonObj = null;
            IDictionary<string, JsonNode?> bodyDict = default!;
            string? body = null;

            var len = tupleArray.Length;
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

                List<NpgsqlRestParameter> paramsList = new(routine.ParamCount);
                bool hasNulls = false;
                int? headerParameterIndex = null;
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
                            if (context.Request.Query.TryGetValue(p, out var qsValue))
                            {
                                if (ParameterParser.TryParseParameter(ref qsValue, ref descriptor, ref parameter))
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
                                else
                                {
                                    // parameters don't match, continuing to another overload
                                    if (options.LogParameterMismatchWarnings)
                                    {
                                        logger?.LogWarning(
                                            "Could not create a valid database parameter of type {0} from value: \"{1}\", skipping path {2} and continuing to another overload...",
                                            routine.ParamTypeDescriptor[0].DbType,
                                            qsValue.ToString(),
                                            context.Request.Path);
                                    }
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
                                            if (!ParameterParser.TryParseParameter(ref bodyStringValues, ref descriptor, ref parameter))
                                            {
                                                // parameters don't match, continuing to another overload
                                                if (options.LogParameterMismatchWarnings)
                                                {
                                                    logger?.LogWarning(
                                                        "Could not create a valid database parameter of type {0} from value: \"{1}\", skipping path {2} and continuing to another overload...",
                                                        routine.ParamTypeDescriptor[0].DbType,
                                                        qsValue.ToString(),
                                                        context.Request.Path);
                                                }
                                            }
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
                                if (parameter.Value is null)
                                {
                                    if (endpoint.RequestHeadersMode == RequestHeadersMode.Parameter)
                                    {
                                        if (string.Equals(p, endpoint.RequestHeadersParameterName, StringComparison.Ordinal) ||
                                            string.Equals(routine.ParamNames[i], endpoint.RequestHeadersParameterName, StringComparison.Ordinal))
                                        {
                                            headerParameterIndex = i;
                                        }
                                    }
                                }
                                if (parameter.Value is not null || headerParameterIndex is not null)
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
                            if (bodyDict.TryGetValue(p, out var value))
                            {
                                if (ParameterParser.TryParseParameter(ref value, ref descriptor, ref parameter))
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
                                else
                                {
                                    // parameters don't match, continuing to another overload
                                    if (options.LogParameterMismatchWarnings)
                                    {
                                        logger?.LogWarning(
                                            "Could not create a valid database parameter of type {0} from value: \"{1}\", skipping path {2} and continuing to another overload...",
                                            routine.ParamTypeDescriptor[0].DbType,
                                            value,
                                            context.Request.Path);
                                    }
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
                                if (parameter.Value is null)
                                {
                                    if (endpoint.RequestHeadersMode == RequestHeadersMode.Parameter)
                                    {
                                        if (string.Equals(p, endpoint.RequestHeadersParameterName, StringComparison.Ordinal) ||
                                            string.Equals(routine.ParamNames[i], endpoint.RequestHeadersParameterName, StringComparison.Ordinal))
                                        {
                                            headerParameterIndex = i;
                                        }
                                    }
                                }
                                if (parameter.Value is not null || headerParameterIndex is not null)
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

                if (endpoint.RequiresAuthorization && context.User?.Identity?.IsAuthenticated is false)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.CompleteAsync();
                    return;
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
                            PgConverters.SerializeString(ref key),
                            ":",
                            PgConverters.SerializeString(ref value));
                    }
                    headers = string.Concat(headers, "}");
                    if (endpoint.RequestHeadersMode == RequestHeadersMode.Parameter)
                    {
                        if (headerParameterIndex.HasValue)
                        {
                            paramsList[headerParameterIndex.Value].Value = headers;
                        }
                    }
                }

                NpgsqlConnection? connection = null;
                try
                {
                    if (options.ConnectionFromServiceProvider)
                    {
                        using IServiceScope? scope = options.ConnectionFromServiceProvider ? serviceProvider.CreateScope() : null;
                        connection = scope?.ServiceProvider.GetRequiredService<NpgsqlConnection>();
                    }
                    else
                    {
                        connection = new(options.ConnectionString);
                    }

                    if (connection is null)
                    {
                        continue;
                    }

                    if (options.LogConnectionNoticeEvents && logger != null)
                    {
                        connection.Notice += (sender, args) =>
                        {
                            Logger.LogConnectionNotice(ref logger, ref args);
                        };
                    }

                    if (connection.State != ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    await using var command = connection.CreateCommand();

                    if (endpoint.RequestHeadersMode == RequestHeadersMode.Context)
                    {
                        command.CommandText = string.Concat("select set_config('request.headers','",headers,"',false)");
                        command.ExecuteNonQuery();
                    }

                    var shouldLog = options.LogCommands && logger != null;
                    StringBuilder? cmdLog = shouldLog ? 
                        new(string.Concat("-- ", context.Request.Method, " ", context.Request.GetDisplayUrl(), Environment.NewLine)) :
                        null;

                    int paramCount = paramsList.Count;
                    var commandText = formatter.IsFormattable ? 
                        formatter.FormatCommand(ref routine, ref paramsList) : 
                        routine.Expression;
                    if (paramCount == 0)
                    {
                        commandText = formatter.IsFormattable ?
                            formatter.FormatEmpty(ref routine) :
                            string.Concat(commandText, formatter.AppendEmpty());
                    } 
                    else
                    {
                        for (var i = 0; i < paramCount; i++)
                        {
                            var parameter = paramsList[i];
                            command.Parameters.Add(parameter);

                            if (formatter.IsFormattable is false)
                            {
                                commandText = string.Concat(commandText,
                                    formatter.AppendCommandParameter(ref parameter, ref i, ref paramCount));
                            }

                            if (shouldLog)
                            {
                                object value = parameter.NpgsqlValue!;
                                var p = ParameterParser.FormatParam(ref value, ref routine.ParamTypeDescriptor[i]);
                                cmdLog!.AppendLine(string.Concat(
                                    "-- $",
                                    paramCount.ToString(),
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
                        cmdLog!.AppendLine(command.CommandText);
                        logger?.LogInformation(cmdLog.ToString());
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
                    
                    if (routine.IsVoid)
                    {
                        await command.ExecuteNonQueryAsync();
                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        await context.Response.CompleteAsync();
                        return;
                    }
                    else // end if (routine.IsVoid)
                    {
                        command.AllResultTypesAreUnknown = true;
                        await using var reader = await command.ExecuteReaderAsync();
                        if (routine.ReturnsRecord == false)
                        {
                            if (await reader.ReadAsync())
                            {
                                string? value = reader.GetValue(0) as string;
                                TypeDescriptor descriptor = routine.ReturnTypeDescriptor[0];
                                if (endpoint.ResponseContentType is not null)
                                {
                                    context.Response.ContentType = endpoint.ResponseContentType;
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
                                        context.Response.Headers.Append(headerKey, headerValue);
                                    }
                                }
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                if (descriptor.IsArray && value is not null)
                                {
                                    value = PgConverters.PgArrayToJsonArray(ref value, ref descriptor);
                                }
                                if (value is not null)
                                {
                                    await context.Response.WriteAsync(value);
                                }
                                await context.Response.CompleteAsync();
                                return;
                            }
                            else
                            {
                                logger?.LogError( 
                                    "Could not read a value from {0} \"{1}\" mapped to {2} {3} ", 
                                    routine.Type.ToString().ToLower(), 
                                    command.CommandText,
                                    context.Request.Method, 
                                    context.Request.Path);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                await context.Response.CompleteAsync();
                                return;
                            }
                        }
                        else // end if (routine.ReturnsRecord == false)
                        {
                            if (endpoint.ResponseContentType is not null)
                            {
                                context.Response.ContentType = endpoint.ResponseContentType;
                            }
                            else
                            {
                                context.Response.ContentType = Application.Json;
                            }
                            if (endpoint.ResponseHeaders.Count > 0)
                            {
                                foreach (var (headerKey, headerValue) in endpoint.ResponseHeaders)
                                {
                                    context.Response.Headers.Append(headerKey, headerValue);
                                }
                            }
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            await context.Response.WriteAsync("[");
                            bool first = true;
                            var routineReturnRecordCount = routine.ReturnRecordCount;
                            while (await reader.ReadAsync())
                            {
                                if (!first)
                                {
                                    await context.Response.WriteAsync(",");
                                }
                                else
                                {
                                    first = false;
                                }

                                for (var i = 0; i < routineReturnRecordCount; i++)
                                {
                                    object value = reader.GetValue(i);
                                    // AllResultTypesAreUnknown = true always returns string, except for null
                                    string raw = value == DBNull.Value ? "" : (string)value;
                                    if (routine.ReturnsUnnamedSet == false)
                                    {
                                        if (i == 0)
                                        {
                                            await context.Response.WriteAsync("{");
                                        }
                                        await context.Response.WriteAsync(string.Concat("\"", endpoint.ReturnRecordNames[i], "\":"));
                                    }

                                    var descriptor = routine.ReturnTypeDescriptor[i];
                                    if (value == DBNull.Value)
                                    {
                                        await context.Response.WriteAsync("null");
                                    }
                                    else if (descriptor.IsArray && value is not null)
                                    {
                                        raw = PgConverters.PgArrayToJsonArray(ref raw, ref descriptor);
                                        await context.Response.WriteAsync(raw);
                                    }
                                    else if ((descriptor.IsNumeric || descriptor.IsBoolean || descriptor.IsJson) && value is not null)
                                    {
                                        if (descriptor.IsBoolean)
                                        {
                                            if (string.Equals(raw, "t", StringComparison.Ordinal))
                                            {
                                                await context.Response.WriteAsync("true");
                                            }
                                            else if (string.Equals(raw, "f", StringComparison.Ordinal))
                                            {
                                                await context.Response.WriteAsync("false");
                                            }
                                            else
                                            {
                                                await context.Response.WriteAsync(raw);
                                            }
                                        }
                                        else
                                        {
                                            // numeric and json
                                            await context.Response.WriteAsync(raw);
                                        }
                                    }
                                    else
                                    {
                                        if (descriptor.ActualDbType == NpgsqlDbType.Unknown)
                                        {
                                            await context.Response.WriteAsync(PgConverters.PgUnknownToJsonArray(ref raw));
                                        }
                                        else if (descriptor.NeedsEscape)
                                        {
                                            await context.Response.WriteAsync(PgConverters.SerializeString(ref raw));
                                        }
                                        else
                                        {
                                            if (descriptor.IsDateTime)
                                            {
                                                await context.Response.WriteAsync(string.Concat("\"", raw.Replace(' ', 'T'), "\""));
                                            }
                                            else
                                            {
                                                await context.Response.WriteAsync(string.Concat("\"", raw, "\""));
                                            }
                                        }
                                    }
                                    if (routine.ReturnsUnnamedSet == false && i == routine.ReturnRecordCount - 1)
                                    {
                                        await context.Response.WriteAsync("}");
                                    }
                                    if (i < routine.ReturnRecordCount - 1)
                                    {
                                        await context.Response.WriteAsync(",");
                                    }
                                } // end for

                            } // end while
                            await context.Response.WriteAsync("]");
                            await context.Response.CompleteAsync();
                            return;
                        } // end if (routine.ReturnsRecord == true)
                    } // end if (routine.IsVoid is false)
                }
                finally
                {
                    if (connection is not null && options.ConnectionFromServiceProvider is false)
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
                        logger?.LogWarning("Could not parse JSON body {0}, skipping path {1}.", body, context.Request.Path);
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
                        logger?.LogWarning("Could not parse JSON body {0}, skipping path {1}. Error: {2}", 
                            body, context.Request.Path, e.Message);
                    }
                }
            }
        });

        return builder;
    }

    private static FrozenDictionary<string, Tuple[]> BuildDictionary(
        IApplicationBuilder builder,
        NpgsqlRestOptions options,
        ILogger? logger)
    {
        var dict = new Dictionary<string, List<Tuple>>();

        foreach (var handler in options.EndpointCreateHandlers)
        {
            handler.Setup(builder, logger);
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
                    logger?.LogWarning( 
                        "Endpoint {0} {1} changed request parameter type from body to query string because body will be used for parameter named `{2}`.",
                        method, 
                        endpoint.Url,
                        endpoint.BodyParameterName);
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
                    logger?.LogInformation("Created endpoint {0} {1}", method, endpoint.Url);
                }
            }
        }

        if (options.EndpointsCreated is not null)
        {
            options.EndpointsCreated(dict.Values.SelectMany(x => x).Select(x => (x.routine, x.endpoint)).ToArray());
        }

        foreach (var handler in options.EndpointCreateHandlers)
        {
            handler.Cleanup();
        }

        return dict
            .ToDictionary(
                x => x.Key,
                x => x.Value.ToArray())
            .ToFrozenDictionary();
    }
}