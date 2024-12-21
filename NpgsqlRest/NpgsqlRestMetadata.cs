using System.Security.Cryptography.X509Certificates;
using Npgsql;
using NpgsqlRest.Defaults;

namespace NpgsqlRest;

public class NpgsqlRestMetadataEntry
{
    internal NpgsqlRestMetadataEntry(
        Routine routine,
        RoutineEndpoint endpoint,
        IRoutineSourceParameterFormatter formatter,
        string key)
    {
        Routine = routine;
        Endpoint = endpoint;
        Formatter = formatter;
        Key = key;
    }

    public Routine Routine { get; }
    public RoutineEndpoint Endpoint { get; }
    public IRoutineSourceParameterFormatter Formatter { get; }
    public string Key { get; }
}

public class NpgsqlRestMetadata
{
    internal NpgsqlRestMetadata(
        Dictionary<string, NpgsqlRestMetadataEntry> entries,
        Dictionary<string, NpgsqlRestMetadataEntry> overloads)
    {
        Entries = entries;
        Overloads = overloads;
    }

    public Dictionary<string, NpgsqlRestMetadataEntry> Entries { get; }
    public Dictionary<string, NpgsqlRestMetadataEntry> Overloads { get; }
}

public static class NpgsqlRestMetadataBuilder
{
    public const int MaxKeyLength = 2056;
    public const int MaxPathLength = 2048;
    public static NpgsqlRestMetadata Build(NpgsqlRestOptions options, ILogger? logger, IApplicationBuilder? builder)
    {
        Dictionary<string, NpgsqlRestMetadataEntry> lookup = [];
        Dictionary<string, NpgsqlRestMetadataEntry> overloads = [];

        var hasLogin = false;
        if (builder is not null)
        {
            foreach (var handler in options.EndpointCreateHandlers)
            {
                handler.Setup(builder, logger, options);
            }
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
            foreach (var (routine, formatter) in source.Read(options, builder?.ApplicationServices))
            {
                RoutineEndpoint endpoint = DefaultEndpoint.Create(routine, options, logger)!;

                if (endpoint is null)
                {
                    continue;
                }

                if (options.EndpointCreated is not null)
                {
                    endpoint = options.EndpointCreated(routine, endpoint)!;
                }

                if (endpoint is null)
                {
                    continue;
                }

                if (endpoint.Url.Length > MaxPathLength)
                {
                    throw new ArgumentException($"URL path for URL {endpoint.Url}, routine {routine.Name} length exceeds {MaxPathLength} characters.");
                }

                var method = endpoint.Method.ToString();
                if (endpoint.HasBodyParameter is true && endpoint.RequestParamType == RequestParamType.BodyJson)
                {
                    endpoint.RequestParamType = RequestParamType.QueryString;
                    logger?.EndpointTypeChanged(method, endpoint.Url, endpoint!.BodyParameterName ?? "");

                }
                var key = string.Concat(method, endpoint?.Url);
                var value = new NpgsqlRestMetadataEntry(routine, endpoint!, formatter, key);
                if (lookup.TryGetValue(key, out var existing))
                {
                    overloads[string.Concat(key, existing.Routine.ParamCount)] = existing;
                }
                lookup[key] = value;

                if (builder is not null)
                {
                    foreach (var handler in options.EndpointCreateHandlers)
                    {
                        handler.Handle(routine, endpoint!);
                    }
                }

                if (options.LogEndpointCreatedInfo)
                {
                    logger?.EndpointCreated(method, endpoint!.Url);
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
            options.EndpointsCreated(lookup.Values.Select(x => (x.Routine, x.Endpoint)).ToArray());
        }

        if (builder is not null)
        {
            (Routine routine, RoutineEndpoint endpoint)[]? array = null;
            foreach (var handler in options.EndpointCreateHandlers)
            {
                array ??= lookup.Values.Select(x => (x.Routine, x.Endpoint)).ToArray();
                handler.Cleanup(array);
                handler.Cleanup();
            }
        }

        return new NpgsqlRestMetadata(lookup, overloads);
    }
}
