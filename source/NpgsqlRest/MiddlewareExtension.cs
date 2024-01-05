﻿using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

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
            foreach (var (routine, meta) in dict[context.Request.Path])
            {
                if (!string.Equals(context.Request.Method, meta.HttpMethod.Method, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                NpgsqlParameter[] parameters = new NpgsqlParameter[routine.ParamCount]; // in gc we trust
                if (meta.Parameters == EndpointParameters.QueryString)
                {
                    if (context.Request.Query.Count != meta.ParamNames.Length)
                    {
                        continue;
                    }
                    for (var i = 0; i < meta.ParamNames.Length; i++)
                    {
                        var p = meta.ParamNames[i];
                        if (context.Request.Query.TryGetValue(p, out var qsValue))
                        {
                            var value = qsValue.FirstOrDefault();
                            if (CommandParameters.TryCreateCmdParameter(ref value, ref routine.ParamTypeDescriptor[i], ref options, out var paramater))
                            {
                                parameters[i] = paramater;
                            }
                            else
                            {
                                Logging.LogWarning(
                                    ref logger, 
                                    ref options,
                                    "Could not create a valid database parameter of type {0} from value: \"{1}\", skipping path {2}.",
                                    routine.ParamTypeDescriptor[0].DbType,
                                    value,
                                    context.Request.Path);
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
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
                            node = JsonNode.Parse(body);
                        }
                        catch (JsonException)
                        {
                            Logging.LogWarning(ref logger, ref options, "Could not parse json body {0}, skipping path {1}.",body, context.Request.Path);
                            continue;
                        }
                        try
                        {
                            jsonObj = node?.AsObject();
                        }
                        catch (InvalidOperationException)
                        {
                            Logging.LogWarning(ref logger, ref options, "Could not parse json body {0}, skipping path {1}.", body, context.Request.Path);
                            continue;
                        }
                    }

                    if (jsonObj?.Count != meta.ParamNames.Length)
                    {
                        continue;
                    }

                    for (var i = 0; i < meta.ParamNames.Length; i++)
                    {
                        var p = meta.ParamNames[i];
                        if (jsonObj.ContainsKey(p))
                        {
                            var value = jsonObj[p];
                            if (CommandParameters.TryCreateCmdParameter(ref value, ref routine.ParamTypeDescriptor[i], ref options, out var paramater))
                            {
                                parameters[i] = paramater;
                            }
                            else
                            {
                                Logging.LogWarning(
                                    ref logger,
                                    ref options,
                                    "Could not create a valid database parameter of type {0} from value: \"{1}\", skipping path {2}.",
                                    routine.ParamTypeDescriptor[0].DbType,
                                    value,
                                    context.Request.Path);
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                //parameters parsed
                    
                if (meta.RequiresAuthorization && context.User?.Identity?.IsAuthenticated is false)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.CompleteAsync();
                    await next(context);
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
                            Logging.LogConnectionNotice(ref logger, ref options, ref args);
                        };
                    }
                    
                    {
                        await connection.OpenAsync();
                    }

                    await connection.OpenAsync();
                    await using var command = connection.CreateCommand();
                    command.Parameters.AddRange(parameters);
                    command.CommandText = routine.Expression;
                    if (routine.IsVoid)
                    {
                        await command.ExecuteNonQueryAsync();
                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        await context.Response.CompleteAsync();
                    }
                    else
                    {
                        command.AllResultTypesAreUnknown = true;
                        //...
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.CompleteAsync();
                    }

                    //command.CommandText = string.Concat("select ", routine.Name, "()");
                    //await command.ExecuteNonQueryAsync();
                    //context.Response.StatusCode = (int)HttpStatusCode.OK;
                    //await context.Response.CompleteAsync();
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

            List<(Routine routine, RoutineEndpointMeta meta)> list = dict.ContainsKey(url) ? dict[url] : new();
            list.Add((routine, meta));
            dict[meta.Url] = list;

            httpFile.HandleEntry(routine, meta);
            Logging.LogInfo(ref logger, ref options, "Created endpoint {0} {1}", meta.HttpMethod.Method, meta.Url);
        }
        httpFile.FinalizeHttpFile();
        return dict
           .ToDictionary(
                x => x.Key,
                x => x.Value.ToArray())
            .ToFrozenDictionary();
    }
}