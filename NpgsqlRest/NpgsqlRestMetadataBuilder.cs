﻿using System.Threading.Channels;
using Npgsql;
using NpgsqlRest.Defaults;
using NpgsqlRest.UploadHandlers;

namespace NpgsqlRest;

public static class NpgsqlRestMetadataBuilder
{
    public const int MaxKeyLength = 2056;
    public const int MaxPathLength = 2048;

    public static NpgsqlRestMetadata Build(NpgsqlRestOptions options, ILogger? logger, IApplicationBuilder? builder)
    {
        Dictionary<string, NpgsqlRestMetadataEntry> lookup = [];
        Dictionary<string, NpgsqlRestMetadataEntry> overloads = [];

        // Create default upload handlers from upload handler options
        options.UploadOptions.UploadHandlers ??= options.UploadOptions.CreateUploadHandlers();

        var hasLogin = false;
        if (builder is not null)
        {
            foreach (var handler in options.EndpointCreateHandlers)
            {
                handler.Setup(builder, logger, options);
            }
        }

        options.SourcesCreated(options.RoutineSources);
        var hasCachedRoutine = false;
        CommentsMode optionsCommentsMode = options.CommentsMode;
        bool hasStreamingEvents = false;
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
            foreach (var (routine, formatter) in source.Read(options, builder?.ApplicationServices))
            {
                RoutineEndpoint endpoint = DefaultEndpoint.Create(routine, options, logger)!;

                if (endpoint is null)
                {
                    continue;
                }

                if (options.EndpointCreated is not null)
                {
                    options.EndpointCreated(endpoint);
                }

                if (endpoint is null)
                {
                    continue;
                }
                
                if (endpoint.Url.Length == 0)
                {
                    throw new ArgumentException($"URL path for URL {endpoint.Url}, routine {routine.Name}  is empty.");
                }

                if (endpoint.Url.Length > MaxPathLength)
                {
                    throw new ArgumentException($"URL path for URL {endpoint.Url}, routine {routine.Name} length exceeds {MaxPathLength} characters.");
                }

                var method = endpoint.Method.ToString();
                if (endpoint.HasBodyParameter is true && endpoint.RequestParamType == RequestParamType.BodyJson)
                {
                    endpoint.RequestParamType = RequestParamType.QueryString;
                    logger?.EndpointTypeChangedBodyParam(method, endpoint.Url, endpoint!.BodyParameterName ?? "");
                }
                if (endpoint.Upload is true)
                {
                    if (endpoint.Method != Method.POST)
                    {
                        logger?.EndpointMethodChangedUpload(method, endpoint.Url, Method.POST.ToString());
                        endpoint.Method = Method.POST;
                    }
                    if (endpoint.RequestParamType == RequestParamType.BodyJson)
                    {
                        endpoint.RequestParamType = RequestParamType.QueryString;
                        logger?.EndpointTypeChangedUpload(method, endpoint.Url);
                    }
                }

                var key = string.Concat(method, endpoint?.Url);
                var value = new NpgsqlRestMetadataEntry(endpoint!, formatter, key);
                if (lookup.TryGetValue(key, out var existing))
                {
                    overloads[string.Concat(key, existing.Endpoint.Routine.ParamCount)] = existing;
                }
                lookup[key] = value;

                if (routine.ColumnsTypeDescriptor is not null && routine.ColumnsTypeDescriptor.Length == 1)
                {
                    bool[] unknownResultTypeList = new bool[routine.ColumnsTypeDescriptor.Length];
                    bool hasKnownType = false;
                    for (var i = 0; i < routine.ColumnsTypeDescriptor.Length; i++)
                    {
                        unknownResultTypeList[i] = routine.ColumnsTypeDescriptor[i].ShouldRenderAsUnknownType;
                        if (routine.ColumnsTypeDescriptor[i].ShouldRenderAsUnknownType is false)
                        {
                            hasKnownType = true;
                        }
                    }
                    if (hasKnownType)
                    {
                        routine.UnknownResultTypeList = unknownResultTypeList;
                    }
                }

                if (builder is not null)
                {
                    foreach (var handler in options.EndpointCreateHandlers)
                    {
                        handler.Handle(endpoint!);
                    }
                }

                if (endpoint?.InfoEventsStreamingPath is not null)
                {
                    if (endpoint.InfoEventsStreamingPath.StartsWith(endpoint.Url) is false)
                    {
                        endpoint.InfoEventsStreamingPath = string.Concat(
                            endpoint.Url.EndsWith('/') ? endpoint.Url[..^1] : endpoint.Url , "/", 
                            endpoint.InfoEventsStreamingPath.StartsWith('/') ? endpoint.InfoEventsStreamingPath[1..] : endpoint.InfoEventsStreamingPath);
                    }

                    NpgsqlRestNoticeEventSource.Paths.Add(endpoint.InfoEventsStreamingPath);

                    if (hasStreamingEvents is false)
                    {
                        hasStreamingEvents = true;
                    }
                }

                if (options.LogEndpointCreatedInfo)
                {
                    var urlInfo = string.Concat(method, " ", endpoint!.Url);
                    logger?.EndpointCreated(urlInfo);

                    if (endpoint?.InfoEventsStreamingPath is not null)
                    {
                        logger?.EndpointInfoStreamingPath(urlInfo, endpoint.InfoEventsStreamingPath);
                    }
                }

                if (endpoint?.Login is true)
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

                if (endpoint?.Cached is true && hasCachedRoutine is false)
                {
                    hasCachedRoutine = true;
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

        if (hasCachedRoutine is true && options.DefaultRoutineCache is RoutineCache)
        {
            RoutineCache.Start(options);
            if (builder is WebApplication app)
            {
                app.Lifetime.ApplicationStopping.Register(() =>
                {
                    RoutineCache.Shutdown();
                });
            }
        }

        if (options.UploadOptions.UploadHandlers is not null && options.UploadOptions.UploadHandlers.ContainsKey(options.UploadOptions.DefaultUploadHandler) is false)
        {
            logger?.LogError("Default upload handler {defaultUploadHandler} not found in the list of upload handlers. Using upload endpoint with default handler may cause an error.", options.UploadOptions.DefaultUploadHandler);
        }

        if (options.EndpointsCreated is not null)
        {
            options.EndpointsCreated([.. lookup.Values.Select(x => x.Endpoint)]);
        }

        if (builder is not null)
        {
            RoutineEndpoint[]? array = null;
            foreach (var handler in options.EndpointCreateHandlers)
            {
                array ??= [.. lookup.Values.Select(x => x.Endpoint)];
                handler.Cleanup(array);
                handler.Cleanup();
            }
        }

        return new NpgsqlRestMetadata(lookup, overloads) { HasStreamingEvents = hasStreamingEvents };
    }
}
