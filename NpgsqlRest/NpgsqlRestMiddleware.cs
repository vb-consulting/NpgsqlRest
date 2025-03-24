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
using static NpgsqlRest.ParameterParser;

using NpgsqlRest.Auth;

namespace NpgsqlRest;

public class NpgsqlRestMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    private static ILogger? logger = default;
    private static NpgsqlRestOptions options = default!;
    private static IServiceProvider serviceProvider = default!;

    internal static NpgsqlRestMetadata metadata = default!;
    internal static Dictionary<string, NpgsqlRestMetadataEntry>.AlternateLookup<ReadOnlySpan<char>> lookup;

    public static ILogger? Logger => logger;
    internal static void SetLogger(ILogger? logger) => NpgsqlRestMiddleware.logger = logger;

    internal static void SetOptions(NpgsqlRestOptions options)
    {
        NpgsqlRestMiddleware.options = options;
    }

    internal static void SetMetadata(NpgsqlRestMetadata metadata)
    {
        NpgsqlRestMiddleware.metadata = metadata;
        lookup = metadata.Entries.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    internal static NpgsqlRestMetadata Metadata => metadata;
    internal static void SetServiceProvider(IServiceProvider serviceProvider) => NpgsqlRestMiddleware.serviceProvider = serviceProvider;

    public async Task InvokeAsync(HttpContext context)
    {
        Span<char> pathKeybuffer = stackalloc char[NpgsqlRestMetadataBuilder.MaxKeyLength];
        var position = 0;
        context.Request.Method.CopyTo(pathKeybuffer[position..]);
        position += context.Request.Method.Length;
        var path = context.Request.Path.ToString();
        path.CopyTo(pathKeybuffer[position..]);
        position += path.Length;
        var pathKey = pathKeybuffer[..position];

        if (!lookup.TryGetValue(pathKey, out var entry))
        {
            if (pathKey[^1] != '/')
            {
                pathKeybuffer[position] = '/';
                position += 1;
                var pathKey2 = pathKeybuffer[..position];
                if (!lookup.TryGetValue(pathKey2, out entry))
                {
                    await _next(context);
                    return;
                }
            }
            else
            {
                var pathKey2 = pathKeybuffer[..(position - 1)];
                if (!lookup.TryGetValue(pathKey2, out entry))
                {
                    await _next(context);
                    return;
                }
            }
        }

        if (entry is null)
        {
            await _next(context);
            return;
        }

        Routine routine = entry.Endpoint.Routine;
        RoutineEndpoint endpoint = entry.Endpoint;
        IRoutineSourceParameterFormatter formatter = entry.Formatter;

        string? headers = null;

        if (endpoint.RequestHeadersMode == RequestHeadersMode.Context)
        {
            if (headers is null)
            {
                SearializeHeader(options, context, ref headers);
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
            if (endpoint.ConnectionName is not null)
            {
                if (options.ConnectionStrings?.TryGetValue(endpoint.ConnectionName, out var connectionString) is true)
                {
                    connection = new(connectionString);
                }
                else
                {
                    await ReturnErrorAsync($"Connection name {endpoint.ConnectionName} could not be found in options ConnectionStrings dictionary.", true, context);
                    return;
                }
            }
            else if (options.ServiceProviderMode != ServiceProviderObject.None)
            {
                if (options.ServiceProviderMode == ServiceProviderObject.NpgsqlDataSource)
                {
                    connection = await serviceProvider.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync();
                }
                else if (options.ServiceProviderMode == ServiceProviderObject.NpgsqlConnection)
                {
                    shouldDispose = false;
                    connection = serviceProvider.GetRequiredService<NpgsqlConnection>();
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
                await ReturnErrorAsync("Connection did not initialize!", log: true, context);
                return;
            }

            if (options.LogConnectionNoticeEvents && logger != null)
            {
                connection.Notice += (sender, args) =>
                {
                    NpgsqlRestLogger.LogConnectionNotice(logger, args, options.LogConnectionNoticeEventsMode);
                };
            }

            await using var command = connection.CreateCommand();

            var shouldLog = options.LogCommands && logger != null;
            StringBuilder? cmdLog = shouldLog ?
                new(string.Concat("-- ", context.Request.Method, " ", context.Request.GetDisplayUrl(), Environment.NewLine)) :
                null;

            if (formatter.IsFormattable is false)
            {
                commandText = routine.Expression;
            }

            // paramsList
            bool hasNulls = false;
            int paramIndex = 0;
            JsonObject? jsonObj = null;
            Dictionary<string, JsonNode?>? bodyDict = null;
            string? body = null;
            Dictionary<string, StringValues>? queryDict = null;
            StringBuilder? cacheKeys = null;
            if (endpoint.Cached is true)
            {
                cacheKeys = new(endpoint.CachedParams?.Count ?? 0 + 1);
                cacheKeys.Append(routine.Expression);
            }

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
                    await _next(context);
                    return;
                }

                if (queryDict.Count != routine.ParamCount && metadata.Overloads.Count > 0)
                {
                    if (metadata.Overloads.TryGetValue(string.Concat(entry.Key, queryDict.Count), out var overload))
                    {
                        routine = overload.Endpoint.Routine;
                        endpoint = overload.Endpoint;
                        formatter = overload.Formatter;
                    }
                }

                for (int i = 0; i < routine.Parameters.Length; i++)
                {
                    var parameter = routine.Parameters[i].NpgsqlResMemberwiseClone();

                    // body parameter
                    if (endpoint.HasBodyParameter &&
                        (
                        string.Equals(endpoint.BodyParameterName, parameter.ConvertedName, StringComparison.Ordinal) ||
                        string.Equals(endpoint.BodyParameterName, parameter.ActualName, StringComparison.Ordinal)
                        )
                    )
                    {
                        if (body is null)
                        {
                            parameter.ParamType = ParamType.BodyParam;
                            parameter.Value = DBNull.Value;
                            hasNulls = true;

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
                            if (endpoint.Cached is true && endpoint.CachedParams is not null)
                            {
                                if (endpoint.CachedParams.Contains(parameter.ConvertedName) || endpoint.CachedParams.Contains(parameter.ActualName))
                                {
                                    cacheKeys?.Append(parameter.GetCacheStringValue());
                                }
                            }
                            command.Parameters.Add(parameter);

                            if (hasNulls is false && parameter.Value == DBNull.Value)
                            {
                                hasNulls = true;
                            }

                            if (formatter.IsFormattable is false)
                            {
                                if (formatter.RefContext)
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(parameter, paramIndex, context));
                                    if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(parameter, paramIndex));
                                }
                            }
                            paramIndex++;
                            if (shouldLog && options.LogCommandParameters)
                            {
                                object value = parameter.NpgsqlValue!;
                                var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                                    "***" :
                                    FormatParam(value, parameter.TypeDescriptor);
                                cmdLog!.AppendLine(string.Concat(
                                    "-- $",
                                    paramIndex.ToString(),
                                    " ", parameter.TypeDescriptor.OriginalType,
                                    " = ",
                                    p));
                            }
                        }
                        else
                        {
                            StringValues bodyStringValues = body;
                            if (TryParseParameter(parameter, ref bodyStringValues, endpoint.QueryStringNullHandling))
                            {
                                parameter.ParamType = ParamType.BodyParam;
                                hasNulls = false;

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
                                if (endpoint.Cached is true && endpoint.CachedParams is not null)
                                {
                                    if (endpoint.CachedParams.Contains(parameter.ConvertedName) || endpoint.CachedParams.Contains(parameter.ActualName))
                                    {
                                        cacheKeys?.Append(parameter.GetCacheStringValue());
                                    }
                                }
                                command.Parameters.Add(parameter);

                                if (hasNulls is false && parameter.Value == DBNull.Value)
                                {
                                    hasNulls = true;
                                }

                                if (formatter.IsFormattable is false)
                                {
                                    if (formatter.RefContext)
                                    {
                                        commandText = string.Concat(commandText,
                                            formatter.AppendCommandParameter(parameter, paramIndex, context));
                                        if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                        {
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        commandText = string.Concat(commandText,
                                            formatter.AppendCommandParameter(parameter, paramIndex));
                                    }
                                }
                                paramIndex++;
                                if (shouldLog && options.LogCommandParameters)
                                {
                                    object value = parameter.NpgsqlValue!;
                                    var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                                        "***" :
                                        FormatParam(value, parameter.TypeDescriptor);
                                    cmdLog!.AppendLine(string.Concat(
                                        "-- $",
                                        paramIndex.ToString(),
                                        " ", parameter.TypeDescriptor.OriginalType,
                                        " = ",
                                        p));
                                }
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
                                SearializeHeader(options, context, ref headers);
                            }
                            parameter.ParamType = ParamType.HeaderParam;
                            parameter.Value = headers;

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
                            if (endpoint.Cached is true && endpoint.CachedParams is not null)
                            {
                                if (endpoint.CachedParams.Contains(parameter.ConvertedName) || endpoint.CachedParams.Contains(parameter.ActualName))
                                {
                                    cacheKeys?.Append(parameter.GetCacheStringValue());
                                }
                            }
                            command.Parameters.Add(parameter);

                            if (hasNulls is false && parameter.Value == DBNull.Value)
                            {
                                hasNulls = true;
                            }

                            if (formatter.IsFormattable is false)
                            {
                                if (formatter.RefContext)
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(parameter, paramIndex, context));
                                    if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(parameter, paramIndex));
                                }
                            }
                            paramIndex++;
                            if (shouldLog && options.LogCommandParameters)
                            {
                                object value = parameter.NpgsqlValue!;
                                var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                                    "***" :
                                    FormatParam(value, parameter.TypeDescriptor);
                                cmdLog!.AppendLine(string.Concat(
                                    "-- $",
                                    paramIndex.ToString(),
                                    " ", parameter.TypeDescriptor.OriginalType,
                                    " = ",
                                    p));
                            }

                            continue;
                        }
                    }

                    if (queryDict.TryGetValue(parameter.ConvertedName, out var qsValue) is false)
                    {
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
                        if (parameter.Value is null)
                        {
                            if (parameter.TypeDescriptor.HasDefault is false)
                            {
                                await _next(context);
                                return;
                            }
                            continue;
                        }
                    }

                    if (parameter.Value is null && TryParseParameter(parameter, ref qsValue, endpoint.QueryStringNullHandling) is false)
                    {
                        if (parameter.TypeDescriptor.HasDefault is false)
                        {
                            await _next(context);
                            return;
                        }
                        continue;
                    }

                    parameter.ParamType = ParamType.QueryString;
                    parameter.QueryStringValues = qsValue;
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
                    if (endpoint.Cached is true && endpoint.CachedParams is not null)
                    {
                        if (endpoint.CachedParams.Contains(parameter.ConvertedName) || endpoint.CachedParams.Contains(parameter.ActualName))
                        {
                            cacheKeys?.Append(parameter.GetCacheStringValue());
                        }
                    }
                    command.Parameters.Add(parameter);

                    if (hasNulls is false && parameter.Value == DBNull.Value)
                    {
                        hasNulls = true;
                    }

                    if (formatter.IsFormattable is false)
                    {
                        if (formatter.RefContext)
                        {
                            commandText = string.Concat(commandText,
                                formatter.AppendCommandParameter(parameter, paramIndex, context));
                            if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                            {
                                return;
                            }
                        }
                        else
                        {
                            commandText = string.Concat(commandText,
                                formatter.AppendCommandParameter(parameter, paramIndex));
                        }
                    }
                    paramIndex++;
                    if (shouldLog && options.LogCommandParameters)
                    {
                        object value = parameter.NpgsqlValue!;
                        var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                            "***" :
                            FormatParam(value, parameter.TypeDescriptor);
                        cmdLog!.AppendLine(string.Concat(
                            "-- $",
                            paramIndex.ToString(),
                            " ", parameter.TypeDescriptor.OriginalType,
                            " = ",
                            p));
                    }
                }

                if (command.Parameters.Count < queryDict.Count)
                {
                    foreach (var queryKey in queryDict.Keys)
                    {
                        if (routine.ParamsHash.Contains(queryKey) is false)
                        {
                            await _next(context);
                            return;
                        }
                    }
                }
            }
            else if (endpoint.RequestParamType == RequestParamType.BodyJson)
            {
                if (bodyDict is null)
                {
                    await _next(context);
                    return;
                }

                if (bodyDict.Count != routine.ParamCount && metadata.Overloads.Count > 0)
                {
                    if (metadata.Overloads.TryGetValue(string.Concat(entry.Key, bodyDict.Count), out var overload))
                    {
                        routine = overload.Endpoint.Routine;
                        endpoint = overload.Endpoint;
                        formatter = overload.Formatter;
                    }
                }

                for (int i = 0; i < routine.Parameters.Length; i++)
                {
                    var parameter = routine.Parameters[i].NpgsqlResMemberwiseClone();

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
                                SearializeHeader(options, context, ref headers);
                            }
                            parameter.ParamType = ParamType.HeaderParam;
                            parameter.Value = headers;

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
                            if (endpoint.Cached is true && endpoint.CachedParams is not null)
                            {
                                if (endpoint.CachedParams.Contains(parameter.ConvertedName) || endpoint.CachedParams.Contains(parameter.ActualName))
                                {
                                    cacheKeys?.Append(parameter.GetCacheStringValue());
                                }
                            }
                            command.Parameters.Add(parameter);

                            if (hasNulls is false && parameter.Value == DBNull.Value)
                            {
                                hasNulls = true;
                            }

                            if (formatter.IsFormattable is false)
                            {
                                if (formatter.RefContext)
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(parameter, paramIndex, context));
                                    if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    commandText = string.Concat(commandText,
                                        formatter.AppendCommandParameter(parameter, paramIndex));
                                }
                            }
                            paramIndex++;
                            if (shouldLog && options.LogCommandParameters)
                            {
                                object pvalue = parameter.NpgsqlValue!;
                                var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                                    "***" :
                                    FormatParam(pvalue, parameter.TypeDescriptor);
                                cmdLog!.AppendLine(string.Concat(
                                    "-- $",
                                    paramIndex.ToString(),
                                    " ", parameter.TypeDescriptor.OriginalType,
                                    " = ",
                                    p));
                            }

                            continue;
                        }
                    }

                    if (bodyDict.TryGetValue(parameter.ConvertedName, out var value) is false)
                    {
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
                        if (parameter.Value is null)
                        {
                            if (parameter.TypeDescriptor.HasDefault is false)
                            {
                                await _next(context);
                                return;
                            }
                            continue;
                        }
                    }

                    if (parameter.Value is null && TryParseParameter(parameter, value) is false)
                    {
                        if (parameter.TypeDescriptor.HasDefault is false)
                        {
                            await _next(context);
                            return;
                        }
                        continue;
                    }
                    parameter.ParamType = ParamType.BodyJson;
                    parameter.JsonBodyNode = value;
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
                    if (endpoint.Cached is true && endpoint.CachedParams is not null)
                    {
                        if (endpoint.CachedParams.Contains(parameter.ConvertedName) || endpoint.CachedParams.Contains(parameter.ActualName))
                        {
                            cacheKeys?.Append(parameter.GetCacheStringValue());
                        }
                    }
                    command.Parameters.Add(parameter);

                    if (hasNulls is false && parameter.Value == DBNull.Value)
                    {
                        hasNulls = true;
                    }

                    if (formatter.IsFormattable is false)
                    {
                        if (formatter.RefContext)
                        {
                            commandText = string.Concat(commandText,
                                formatter.AppendCommandParameter(parameter, paramIndex, context));
                            if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                            {
                                return;
                            }
                        }
                        else
                        {
                            commandText = string.Concat(commandText,
                                formatter.AppendCommandParameter(parameter, paramIndex));
                        }
                    }
                    paramIndex++;
                    if (shouldLog && options.LogCommandParameters)
                    {
                        object pvalue = parameter.NpgsqlValue!;
                        var p = options.AuthenticationOptions.ObfuscateAuthParameterLogValues && endpoint.IsAuth ?
                            "***" :
                            FormatParam(pvalue, parameter.TypeDescriptor);
                        cmdLog!.AppendLine(string.Concat(
                            "-- $",
                            paramIndex.ToString(),
                            " ", parameter.TypeDescriptor.OriginalType,
                            " = ",
                            p));
                    }
                }

                if (command.Parameters.Count < bodyDict.Count)
                {
                    foreach (var bodyKey in bodyDict.Keys)
                    {
                        if (routine.ParamsHash.Contains(bodyKey) is false)
                        {
                            await _next(context);
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
            // paramsList is ready

            if (formatter.IsFormattable is true)
            {
                if (formatter.RefContext)
                {
                    commandText = formatter.FormatCommand(routine, command.Parameters, context);
                    if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
                    {
                        return;
                    }
                }
                else
                {
                    commandText = formatter.FormatCommand(routine, command.Parameters);
                }
            }
            if (formatter.IsFormattable is false)
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

            if (commandText is null)
            {
                await _next(context);
                return;
            }

            if (endpoint.Login is true)
            {
                if (await PrepareCommand(connection, command, commandText, context, endpoint, headers, false) is false)
                {
                    return;
                }
                await AuthHandler.HandleLoginAsync(command, context, routine, options, logger);
                if (context.Response.HasStarted is true || options.AuthenticationOptions.SerializeAuthEndpointsResponse is false)
                {
                    return;
                }
            }

            if (endpoint.Logout is true)
            {
                if (await PrepareCommand(connection, command, commandText, context, endpoint, headers, true) is false)
                {
                    return;
                }
                await AuthHandler.HandleLogoutAsync(command, routine, context);
                return;
            }

            if (routine.IsVoid)
            {
                if (await PrepareCommand(connection, command, commandText, context, endpoint, headers, true) is false)
                {
                    return;
                }
                await command.ExecuteNonQueryAsync();
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }
            else // end if (routine.IsVoid)
            {
                if (routine.ReturnsSet == false && routine.ColumnCount == 1 && routine.ReturnsRecordType is false)
                {
                    string? valueResult;
                    if (endpoint.Cached is true)
                    {
                        if (options.DefaultRoutineCache.Get(endpoint, cacheKeys?.ToString()!, out valueResult) is false)
                        {
                            if (await PrepareCommand(connection, command, commandText, context, endpoint, headers, true) is false)
                            {
                                return;
                            }

                            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                            if (shouldLog)
                            {
                                NpgsqlRestLogger.LogEndpoint(logger, endpoint, cmdLog?.ToString() ?? "", command.CommandText);
                            }
                            if (await reader.ReadAsync())
                            {
                                valueResult = reader.GetValue(0) as string;
                                options.DefaultRoutineCache.AddOrUpdate(endpoint, cacheKeys?.ToString()!, valueResult);
                            }
                            else
                            {
                                logger?.CouldNotReadCommand(command.CommandText, context.Request.Method, context.Request.Path);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                return;
                            }
                        }
                        else
                        {
                            if (shouldLog)
                            {
                                cmdLog?.AppendLine("/* from cache */");
                                NpgsqlRestLogger.LogEndpoint(logger, endpoint, cmdLog?.ToString() ?? "", commandText);
                            }
                        }
                    }
                    else
                    { 
                        if (await PrepareCommand(connection, command, commandText, context, endpoint, headers, true) is false)
                        {
                            return;
                        }

                        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                        if (shouldLog)
                        {
                            NpgsqlRestLogger.LogEndpoint(logger, endpoint, cmdLog?.ToString() ?? "", command.CommandText);
                        }
                        if (await reader.ReadAsync())
                        {
                            valueResult = reader.GetValue(0) as string;
                        }
                        else
                        {
                            logger?.CouldNotReadCommand(command.CommandText, context.Request.Method, context.Request.Path);
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return;
                        }
                    }

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
                        var span = (valueResult ?? "").AsSpan();
                        if (options.DefaultResponseParser is not null && endpoint.ParseResponse)
                        {
                            span = options.DefaultResponseParser.Parse(span, endpoint, context);
                        }
                        writer.Advance(Encoding.UTF8.GetBytes(span, writer.GetSpan(Encoding.UTF8.GetMaxByteCount(span.Length))));
                    }
                    else
                    {
                        if (valueResult is null)
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
                            return;
                        }

                        var span = (descriptor.IsArray && valueResult is not null) ? 
                            PgConverters.PgArrayToJsonArray(valueResult.AsSpan(), descriptor) : valueResult.AsSpan();
                        if (options.DefaultResponseParser is not null && endpoint.ParseResponse)
                        {
                            span = options.DefaultResponseParser.Parse(span, endpoint, context);
                        }
                        writer.Advance(Encoding.UTF8.GetBytes(span, writer.GetSpan(Encoding.UTF8.GetMaxByteCount(span.Length))));
                    }
                    return;

                }
                else // end if (routine.ReturnsRecord == false)
                {
                    if (await PrepareCommand(connection, command, commandText, context, endpoint, headers, true) is false)
                    {
                        return;
                    }

                    await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    if (shouldLog)
                    {
                        NpgsqlRestLogger.LogEndpoint(logger, endpoint, cmdLog?.ToString() ?? "", command.CommandText);
                    }
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

        await _next(context);
        return;
    }

    private static async Task<bool> PrepareCommand(
        NpgsqlConnection connection,
        NpgsqlCommand command,
        string commandText,
        HttpContext context, 
        RoutineEndpoint endpoint, 
        string? headers,
        bool allResultsAreUnknown)
    {
        if (connection.State != ConnectionState.Open)
        {
            if (options.BeforeConnectionOpen is not null)
            {
                options.BeforeConnectionOpen(connection, endpoint, context);
            }
            await connection.OpenAsync();
        }
        if (endpoint.RequestHeadersMode == RequestHeadersMode.Context)
        {
            command.CommandText = string.Concat("select set_config('request.headers','", headers, "',false)");
            command.ExecuteNonQuery();
        }
        command.CommandText = commandText;
        
        if (endpoint.CommandTimeout.HasValue)
        {
            command.CommandTimeout = endpoint.CommandTimeout.Value;
        }
        //command callback
        if (options.CommandCallbackAsync is not null)
        {
            await options.CommandCallbackAsync(endpoint, command, context);
            if (context.Response.HasStarted || context.Response.StatusCode != (int)HttpStatusCode.OK)
            {
                return false;
            }
        }
        command.AllResultTypesAreUnknown = allResultsAreUnknown;
        return true;
    }

    private static void SearializeHeader(NpgsqlRestOptions options, HttpContext context, ref string? headers)
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

    private static async Task ReturnErrorAsync(string message, bool log, HttpContext context)
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
}