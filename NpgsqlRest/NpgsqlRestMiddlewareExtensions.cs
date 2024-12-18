using System.Data;
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

        Dictionary<string, Tuple> dict = [];
        Dictionary<string, Tuple> overloads = [];
        {
            var hasLogin = false;
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
                        result = options.EndpointCreated(routine, result);
                    }

                    if (result is null)
                    {
                        continue;
                    }

                    RoutineEndpoint endpoint = result;
                    var method = endpoint.Method.ToString();
                    if (endpoint.HasBodyParameter is true && endpoint.RequestParamType == RequestParamType.BodyJson)
                    {
                        endpoint.RequestParamType = RequestParamType.QueryString;
#pragma warning disable CS8604 // Possible null reference argument.
                        logger?.EndpointTypeChanged(method, endpoint.Url, endpoint.BodyParameterName);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    var key = string.Concat(method, endpoint.Url);
                    var value = (routine, endpoint, formatter);
                    if (dict.TryGetValue(key, out var existing))
                    {
                        overloads[string.Concat(key, existing.routine.ParamCount)] = existing;
                    }
                    dict[key] = value;

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
                options.EndpointsCreated(dict.Values.Select(x => (x.routine, x.endpoint)).ToArray());
            }

            (Routine routine, RoutineEndpoint endpoint)[]? array = null;
            foreach (var handler in options.EndpointCreateHandlers)
            {
                array ??= dict.Values.Select(x => (x.routine, x.endpoint)).ToArray();
                handler.Cleanup(array);
                handler.Cleanup();
            }
        }
        if (dict.Count == 0)
        {
            return builder;
        }

        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        builder.Use(async (context, next) =>
        {
            string key = default!;
            var pathKey = string.Concat(
                context.Request.Method,
                context.Request.Path.ToString(), '/')
                .AsSpan();

            if (!lookup.TryGetValue(pathKey, out var tuple))
            {
                if (pathKey[^1] == '/' && pathKey[^2] == '/')
                {
                    if (!lookup.TryGetValue(pathKey[..^2], out tuple))
                    {
                        await next(context);
                        return;
                    }
                    else
                    {
                        key = pathKey[..^2].ToString();
                    }
                }
                else if (pathKey[^1] == '/')
                {
                    if (!lookup.TryGetValue(pathKey[..^1], out tuple))
                    {
                        await next(context);
                        return;
                    }
                    else
                    {
                        key = pathKey[..^1].ToString();
                    }
                }
            }
            else
            {
                key = pathKey.ToString();
            }

            var (routine, endpoint, formatter) = tuple;
            string? headers = null;

            // paramsList
            // paramsList is ready

            if (endpoint.RequestHeadersMode == RequestHeadersMode.Context)
            {
                if (headers is null)
                {
                    SearializeHeader();
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
                    await ReturnErrorAsync("Connection did not initialize!", log: true);
                    return;
                }

                if (options.LogConnectionNoticeEvents && logger != null)
                {
                    connection.Notice += (sender, args) =>
                    {
                        NpgsqlRestLogger.LogConnectionNotice(logger, args);
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

                // paramsList
                bool hasNulls = false;
                JsonObject? jsonObj = null;
                Dictionary<string, JsonNode?>? bodyDict = null;
                string? body = null;
                Dictionary<string, StringValues>? queryDict = null;

                if (endpoint.RequestParamType == RequestParamType.QueryString)
                {
                    queryDict = context.Request.Query.ToDictionary();
                }
                if (endpoint.HasBodyParameter || endpoint.RequestParamType == RequestParamType.BodyJson)
                {
                    context.Request.EnableBuffering();
                    context.Request.Body.Position = 0;

                    using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                    {
                        body = await reader.ReadToEndAsync();
                    }
                    if (endpoint.RequestParamType == RequestParamType.BodyJson)
                    {
                        JsonNode? node = null;
                        try
                        {
                            node = JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                        }
                        catch (JsonException e)
                        {
                            logger?.CouldNotParseJson(body, context.Request.Path, e.Message);
                            node = null;
                        }
                        if (node is not null)
                        {
                            try
                            {
                                jsonObj = node?.AsObject();
                                bodyDict = jsonObj?.ToDictionary();
                            }
                            catch (Exception e)
                            {
                                logger?.CouldNotParseJson(body, context.Request.Path, e.Message);
                                bodyDict = null;
                            }
                        }
                    }
                }

                if (endpoint.RequestParamType == RequestParamType.QueryString)
                {
                    if (queryDict is null)
                    {
                        await next(context);
                        return;
                    }

                    if (queryDict.Count != routine.ParamCount && overloads.Count > 0)
                    {
                        if (overloads.TryGetValue(string.Concat(key, queryDict.Count), out var overload))
                        {
                            (routine, endpoint, formatter) = overload;
                        }
                    }

                    for (int i = 0; i < routine.Parameters.Length; i++)
                    {
                        var parameter = routine.Parameters[i];

                        // body parameter
                        if (endpoint.HasBodyParameter &&
                            (
                            string.Equals(endpoint.BodyParameterName, parameter.ConvertedName, StringComparison.Ordinal) ||
                            string.Equals(endpoint.BodyParameterName, parameter.ActualName, StringComparison.Ordinal)
                            )
                        )
                        {
                            var bodyParameter = parameter.NpgsqlResMemberwiseClone();
                            if (body is null)
                            {
                                bodyParameter.ParamType = ParamType.BodyParam;
                                bodyParameter.Value = DBNull.Value;
                                hasNulls = true;
                                //paramsList.Add(bodyParameter);
                                command.Parameters.Add(bodyParameter);
                            }
                            else
                            {
                                StringValues bodyStringValues = body;
                                if (TryParseParameter(bodyParameter, ref bodyStringValues, endpoint.QueryStringNullHandling))
                                {
                                    bodyParameter.ParamType = ParamType.BodyParam;
                                    hasNulls = false;
                                    //paramsList.Add(bodyParameter);
                                    command.Parameters.Add(bodyParameter);
                                }
                            }
                            continue;
                        }

                        // header parameter
                        if (
                            endpoint.RequestHeadersMode == RequestHeadersMode.Parameter &&
                            parameter.TypeDescriptor.HasDefault is true &&
                            (
                            string.Equals(endpoint.RequestHeadersParameterName, parameter.ConvertedName, StringComparison.Ordinal) ||
                            string.Equals(endpoint.RequestHeadersParameterName, parameter.ActualName, StringComparison.Ordinal)
                            )
                        )
                        {
                            if (queryDict.ContainsKey(parameter.ConvertedName) is false)
                            {
                                if (headers is null)
                                {
                                    SearializeHeader();
                                }
                                var headerParameter = parameter.NpgsqlResMemberwiseClone();
                                headerParameter.ParamType = ParamType.HeaderParam;
                                headerParameter.Value = headers;
                                //paramsList.Add(headerParameter);
                                command.Parameters.Add(headerParameter);
                                continue;
                            }
                        }

                        if (queryDict.TryGetValue(parameter.ConvertedName, out var qsValue) is false)
                        {
                            if (parameter.TypeDescriptor.HasDefault is false)
                            {
                                await next(context);
                                return;
                            }
                            continue;
                        }

                        var queryParemeter = parameter.NpgsqlResMemberwiseClone();
                        if (TryParseParameter(queryParemeter, ref qsValue, endpoint.QueryStringNullHandling) is false)
                        {
                            if (queryParemeter.TypeDescriptor.HasDefault is false)
                            {
                                await next(context);
                                return;
                            }
                            continue;
                        }

                        queryParemeter.ParamType = ParamType.QueryString;
                        queryParemeter.QueryStringValues = qsValue;
                        //paramsList.Add(queryParemeter);
                        command.Parameters.Add(queryParemeter);

                        if (hasNulls is false && queryParemeter.Value == DBNull.Value)
                        {
                            hasNulls = true;
                        }
                    }

                    if (command.Parameters.Count < queryDict.Count)
                    {
                        foreach (var queryKey in queryDict.Keys)
                        {
                            if (routine.ParamsHash.Contains(queryKey) is false)
                            {
                                await next(context);
                                return;
                            }
                        }
                    }
                }
                else if (endpoint.RequestParamType == RequestParamType.BodyJson)
                {
                    if (bodyDict is null)
                    {
                        await next(context);
                        return;
                    }

                    if (bodyDict.Count != routine.ParamCount && overloads.Count > 0)
                    {
                        if (overloads.TryGetValue(string.Concat(key, bodyDict.Count), out var overload))
                        {
                            (routine, endpoint, formatter) = overload;
                        }
                    }

                    for (int i = 0; i < routine.Parameters.Length; i++)
                    {
                        var parameter = routine.Parameters[i];

                        // header parameter
                        if (
                            endpoint.RequestHeadersMode == RequestHeadersMode.Parameter &&
                            parameter.TypeDescriptor.HasDefault is true &&
                        (
                            string.Equals(endpoint.RequestHeadersParameterName, parameter.ConvertedName, StringComparison.Ordinal) ||
                            string.Equals(endpoint.RequestHeadersParameterName, parameter.ActualName, StringComparison.Ordinal)
                            )
                        )
                        {
                            if (bodyDict.ContainsKey(parameter.ConvertedName) is false)
                            {
                                if (headers is null)
                                {
                                    SearializeHeader();
                                }
                                var headerParameter = parameter.NpgsqlResMemberwiseClone();
                                headerParameter.ParamType = ParamType.HeaderParam;
                                headerParameter.Value = headers;
                                //paramsList.Add(headerParameter);
                                command.Parameters.Add(headerParameter);
                                continue;
                            }
                        }

                        if (bodyDict.TryGetValue(parameter.ConvertedName, out var value) is false)
                        {
                            if (parameter.TypeDescriptor.HasDefault is false)
                            {
                                await next(context);
                                return;
                            }
                            continue;
                        }
                        var bodyParameter = parameter.NpgsqlResMemberwiseClone();
                        if (TryParseParameter(bodyParameter, value) is false)
                        {
                            if (bodyParameter.TypeDescriptor.HasDefault is false)
                            {
                                await next(context);
                                return;
                            }
                            continue;
                        }
                        bodyParameter.ParamType = ParamType.BodyJson;
                        bodyParameter.JsonBodyNode = value;
                        //paramsList.Add(bodyParameter);
                        command.Parameters.Add(bodyParameter);
                        if (hasNulls is false && bodyParameter.Value == DBNull.Value)
                        {
                            hasNulls = true;
                        }
                    }

                    if (command.Parameters.Count < bodyDict.Count)
                    {
                        foreach (var bodyKey in bodyDict.Keys)
                        {
                            if (routine.ParamsHash.Contains(bodyKey) is false)
                            {
                                await next(context);
                                return;
                            }
                        }
                    }
                }

                if (hasNulls && routine.IsStrict)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    await context.Response.CompleteAsync();
                    return;
                }


                if (options.ValidateParameters is not null || options.ValidateParametersAsync is not null)
                {
                    for (var i = 0; i < command.Parameters.Count; i++)
                    {
                        NpgsqlRestParameter parameter = (NpgsqlRestParameter)command.Parameters[i];
                        if (options.ValidateParameters is not null)
                        {
                            options.ValidateParameters(new ParameterValidationValues(
                                context,
                                routine,
                                parameter));
                            if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                            {
                                return;
                            }
                        }
                        if (options.ValidateParametersAsync is not null)
                        {
                            await options.ValidateParametersAsync(new ParameterValidationValues(
                                context,
                                routine,
                                parameter));
                            if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                            {
                                return;
                            }
                        }
                    }
                }
                // paramsList is ready

                var shouldLog = options.LogCommands && logger != null;
                StringBuilder? cmdLog = shouldLog ?
                    new(string.Concat("-- ", context.Request.Method, " ", context.Request.GetDisplayUrl(), Environment.NewLine)) :
                    null;

                if (formatter.RefContext)
                {
                    commandText = formatter.IsFormattable ?
                        formatter.FormatCommand(routine, command.Parameters, context) :
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
                        formatter.FormatCommand(routine, command.Parameters) :
                        routine.Expression;
                }

                if (command.Parameters.Count == 0 && formatter.IsFormattable is false)
                {
                    if (formatter.RefContext)
                    {
                        commandText = string.Concat(commandText, formatter.AppendEmpty(context));
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
                    for (var i = 0; i < command.Parameters.Count; i++)
                    {
                        //var parameter = paramsList[i];
                        //command.Parameters.Add(parameter);
                        NpgsqlRestParameter parameter = (NpgsqlRestParameter)command.Parameters[i];

                        if (formatter.IsFormattable is false)
                        {
                            if (formatter.RefContext)
                            {
                                commandText = string.Concat(commandText,
                                    formatter.AppendCommandParameter(parameter, i, command.Parameters.Count, context));
                                if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                commandText = string.Concat(commandText,
                                    formatter.AppendCommandParameter(parameter, i, command.Parameters.Count));
                            }
                        }

                        if (shouldLog && options.LogCommandParameters)
                        {
                            object value = parameter.NpgsqlValue!;
                            var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                                "***" :
                                FormatParam(value, parameter.TypeDescriptor);
                            cmdLog!.AppendLine(string.Concat(
                                "-- $",
                                (i + 1).ToString(),
                                " ", parameter.TypeDescriptor.OriginalType,
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
                    NpgsqlRestLogger.LogEndpoint(logger, endpoint, cmdLog?.ToString() ?? "", command.CommandText);
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
                    await AuthHandler.HandleLoginAsync(command, context, routine, options, logger);
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
                                    PgConverters.ParseParameters(command.Parameters, endpoint.ResponseContentType) :
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
                                        PgConverters.ParseParameters(command.Parameters, headerValue.ToString()) : headerValue);
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
                                PgConverters.ParseParameters(command.Parameters, endpoint.ResponseContentType) :
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
                                    PgConverters.ParseParameters(command.Parameters, headerValue.ToString()) : headerValue);
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
                                        row.Append(routine.ColumnNames[i]);
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
                                            row.Append(PgConverters.PgUnknownToJsonArray(ref raw));
                                        }
                                        else if (descriptor.NeedsEscape)
                                        {
                                            row.Append(PgConverters.SerializeString(ref raw));
                                        }
                                        else
                                        {
                                            if (descriptor.IsDateTime)
                                            {
                                                row.Append(PgConverters.QuoteDateTime(ref raw));
                                            }
                                            else
                                            {
                                                row.Append(PgConverters.Quote(ref raw));
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

            await next(context);
            return;

            async Task ReturnErrorAsync(string message, bool log)
            {
                if (log)
                {
                    logger?.LogError("{message}", message);
                }
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = Text.Plain;
                await context.Response.WriteAsync(message);
                await context.Response.CompleteAsync();
            }

            void SearializeHeader()
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
                    headers = string.Concat(
                        headers,
                        PgConverters.SerializeString(header.Key),
                        ":",
                        PgConverters.SerializeString(header.Value.ToString()));
                }
                headers = string.Concat(headers, "}");
            }
        });

        return builder;
    }
}