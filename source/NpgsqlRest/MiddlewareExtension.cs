using System.Collections.Frozen;
using System.Data;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using static NpgsqlRest.Logging;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Routing;

namespace NpgsqlRest;

public static class NpgsqlRestMiddlewareExtensions
{
    public static IApplicationBuilder UseNpgsqlRest(this IApplicationBuilder builder, NpgsqlRestOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(options.ConnectionString);
        ILogger? logger = null;
        if (builder is WebApplication app)
        {
            logger = app.Logger;
        }
        
        var dict = BuildDictionary(builder, options, logger);
        var serviceProvider = builder.ApplicationServices;

        builder.Use(async (context, next) =>
        {
            if (!dict.ContainsKey(context.Request.Path))
            {
                await next(context);
                return;
            }

            JsonObject? jsonObj = null;
            (Routine routine, RoutineEndpointMeta meta)[] tupleArray = dict[context.Request.Path];

            for (var index = 0; index < tupleArray.AsSpan().Length; index++)
            {
                var (routine, meta) = tupleArray[index];
                if (!string.Equals(context.Request.Method, meta.HttpMethod.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                NpgsqlParameter[] parameters = new NpgsqlParameter[routine.ParamCount]; // in gc we trust
                if (routine.ParamCount > 0)
                {
                    if (meta.Parameters == EndpointParameters.QueryString)
                    {
                        if (context.Request.Query.Count != meta.ParamNames.Length)
                        {
                            continue;
                        }
                        int setCount = 0;
                        for (var i = 0; i < routine.ParamCount; i++)
                        {
                            var p = meta.ParamNames[i];
                            if (context.Request.Query.TryGetValue(p, out var qsValue))
                            {
                                var value = qsValue.FirstOrDefault();
                                if (CommandParameters.TryCreateCmdParameter(ref value, ref routine.ParamTypeDescriptor[i], ref options, out var paramater))
                                {
                                    parameters[i] = paramater;
                                    setCount++;
                                }
                                else
                                {
                                    // parameters don't match, continuing to another overload
                                    if (options.LogParameterMismatchWarnings)
                                    {
                                        LogWarning(
                                            ref logger,
                                            ref options,
                                            "Could not create a valid database parameter of type {0} from value: \"{1}\", skipping path {2} and continuing to another overload...",
                                            routine.ParamTypeDescriptor[0].DbType,
                                            value,
                                            context.Request.Path);
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                break;
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
                            string body;
                            context.Request.EnableBuffering();
                            context.Request.Body.Position = 0;
                            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                            {
                                body = await reader.ReadToEndAsync();
                            }

                            if (string.IsNullOrEmpty(body))
                            {
                                continue;
                            }

                            JsonNode node;
                            try
                            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                                node = JsonNode.Parse(body);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                            }
                            catch (JsonException)
                            {
                                LogWarning(ref logger, ref options, "Could not parse json body {0}, skipping path {1}.", body, context.Request.Path);
                                continue;
                            }
                            try
                            {
                                jsonObj = node?.AsObject();
                            }
                            catch (InvalidOperationException)
                            {
                                LogWarning(ref logger, ref options, "Could not parse json body {0}, skipping path {1}.", body, context.Request.Path);
                                continue;
                            }
                        }

                        if (jsonObj?.Count != meta.ParamNames.Length)
                        {
                            continue;
                        }
                        
                        int setCount = 0;
                        for (var i = 0; i < meta.ParamNames.Length; i++)
                        {
                            var p = meta.ParamNames[i];
                            if (jsonObj.ContainsKey(p))
                            {
                                var value = jsonObj[p];
                                if (CommandParameters.TryCreateCmdParameter(ref value, ref routine.ParamTypeDescriptor[i], ref options, out var paramater))
                                {
                                    parameters[i] = paramater;
                                    setCount++;
                                }
                                else
                                {
                                    // parameters don't match, continuing to another overload
                                    if (options.LogParameterMismatchWarnings)
                                    {
                                        LogWarning(
                                            ref logger,
                                            ref options,
                                            "Could not create a valid database parameter of type {0} from value: \"{1}\", skipping path {2} and continuing to another overload...",
                                            routine.ParamTypeDescriptor[0].DbType,
                                            value,
                                            context.Request.Path);
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (setCount != routine.ParamCount)
                        {
                            continue;
                        }
                    }
                }

                if (meta.RequiresAuthorization && context.User?.Identity?.IsAuthenticated is false)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.CompleteAsync();
                    return;
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
                            LogConnectionNotice(ref logger, ref options, ref args);
                        };
                    }

                    if (connection.State != ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    await using var command = connection.CreateCommand();
                    command.Parameters.AddRange(parameters);
                    command.CommandText = routine.Expression;
                    if (meta.CommandTimeout.HasValue)
                    {
                        command.CommandTimeout = meta.CommandTimeout.Value;
                    }
                    if (routine.IsVoid)
                    {
                        await command.ExecuteNonQueryAsync();
                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        await context.Response.CompleteAsync();
                        return;
                    }
                    else
                    {
                        command.AllResultTypesAreUnknown = true;
                        using var reader = await command.ExecuteReaderAsync();
                        if (routine.ReturnsRecord == false)
                        {
                            if (await reader.ReadAsync())
                            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                                string value = reader.GetValue(0) as string;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                                if (meta.ResponseContentType is not null)
                                {
                                    context.Response.ContentType = meta.ResponseContentType;
                                }
                                else if (routine.ReturnTypeDescriptor[0].IsJson)
                                {
                                    context.Response.ContentType = Application.Json;
                                }
                                else
                                {
                                    context.Response.ContentType = Text.Plain;
                                }
                                if (meta.ResponseHeaders.Count > 0)
                                {
                                    foreach (var (headerKey, headerValue) in meta.ResponseHeaders)
                                    {
                                        context.Response.Headers.Append(headerKey, headerValue);
                                    }
                                }
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
#pragma warning disable CS8604 // Possible null reference argument.
                                await context.Response.WriteAsync(value);
#pragma warning restore CS8604 // Possible null reference argument.
                                await context.Response.CompleteAsync();
                                return;
                            }
                            else
                            {
                                LogError(ref logger, ref options, "Could not read a value from expression \"{0}\" mapped to {1} {2} ", routine.Expression, context.Request.Method, context.Request.Path);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                await context.Response.CompleteAsync();
                                return;
                            }
                        }
                        else
                        {
                            if (meta.ResponseContentType is not null)
                            {
                                context.Response.ContentType = meta.ResponseContentType;
                            }
                            else 
                            {
                                context.Response.ContentType = Application.Json;
                            }
                            if (meta.ResponseHeaders.Count > 0)
                            {
                                foreach (var (headerKey, headerValue) in meta.ResponseHeaders)
                                {
                                    context.Response.Headers.Append(headerKey, headerValue);
                                }
                            }
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            await context.Response.WriteAsync("[");
                            bool first = true;
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
                                string[] values = new string[routine.ReturnRecordCount];
                                reader.GetValues(values);
                                for (var i = 0; i < routine.ReturnRecordCount; i++)
                                {
                                    string value = values[i];
                                    if (routine.ReturnsUnnamedSet == false)
                                    {
                                        await context.Response.WriteAsync(string.Concat("\"", meta.ReturnRecordNames[i], "\":"));
                                    }
                                    var descriptor = routine.ReturnTypeDescriptor[i];
                                    if ((object)value == DBNull.Value)
                                    {
                                        await context.Response.WriteAsync("null");
                                    }
                                    else if (descriptor.IsNumeric || descriptor.IsBoolean || descriptor.IsJson)
                                    {
                                        await context.Response.WriteAsync(value);
                                    }
                                    else
                                    {
                                        await context.Response.WriteAsync(string.Concat("\"", value, "\""));
                                    }
                                }
                            }
                            await context.Response.WriteAsync("]");
                            await context.Response.CompleteAsync();
                            return;
                        }
                    }
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
        });

        return builder;
    }

    private static FrozenDictionary<string, (Routine routine, RoutineEndpointMeta meta)[]> BuildDictionary(
        IApplicationBuilder builder,
        NpgsqlRestOptions options,
        ILogger? logger)
    {
        var dict = new Dictionary<string, List<(Routine routine, RoutineEndpointMeta meta)>>();
        var httpFile = new HttpFile(builder, options, logger);
        foreach (var routine in RoutineQuery.Run(options))
        {
            var url = options.UrlPathBuilder(routine, options);
            RoutineEndpointMeta? meta = Defaults.DefaultMetaBuilder(routine, options, url);

            if (meta is null)
            {
                continue;
            }

            if (options.EndpointMetaCallback is not null)
            {
                meta = options.EndpointMetaCallback(routine, options, meta);
            }

            if (meta is null)
            {
                continue;
            }

            List<(Routine routine, RoutineEndpointMeta meta)> list = dict.TryGetValue(url, out var value) ? value : [];
            list.Add((routine, meta));
            dict[meta.Url] = list;

            httpFile.HandleEntry(routine, meta);
            LogInfo(ref logger, ref options, "Created endpoint {0} {1}", meta.HttpMethod.ToString(), meta.Url);
        }
        httpFile.FinalizeHttpFile();
        return dict
           .ToDictionary(
                x => x.Key,
                x => x.Value.ToArray())
            .ToFrozenDictionary();
    }
}