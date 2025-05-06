using Npgsql;

namespace NpgsqlRest.UploadHandlers;

/// <summary>
/// Interface for handling file uploads.
/// </summary>
public interface IUploadHandler
{
    /// <summary>
    /// Set the type of the upload handler.
    /// </summary>
    /// <param name="type"></param>
    IUploadHandler SetType(string type)
    {
        return this;
    }

    /// <summary>
    /// Uploads a file from the context.
    /// </summary>
    /// <param name="connection">Opened connection object</param>
    /// <param name="context">Http context</param>
    /// <param name="parameters">Upload parameters, specific for each type</param>
    /// <returns>
    /// JSON string with upload metadata that is passed to the upload metadata parameter. 
    /// It can be array of filename, mime type, size, etc. It depends on implementation.
    /// </returns>
    Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters);

    /// <summary>
    /// Connection in Upload call will be under transaction yes or no.
    /// </summary>
    bool RequiresTransaction { get; }

    /// <summary>
    /// List of parameters that are used in the upload handler.
    /// </summary>
    string[] Parameters => default!;

    /// <summary>
    /// Runs is the subsequent command fails.
    /// </summary>
    /// <param name="connection">Opened connection object</param>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception);
}

public class DefaultUploadHandler(params IUploadHandler[] handlers) : IUploadHandler
{
    public static IUploadHandler? Create(NpgsqlRestOptions options, RoutineEndpoint endpoint)
    {
        if (endpoint.UploadHandlers is null || endpoint.UploadHandlers.Length == 0)
        {
            if (options.UploadHandlers is not null && options.UploadHandlers.TryGetValue(options.DefaultUploadHandler, out var handler))
            {
                return handler().SetType(options.DefaultUploadHandler);
            }
            else
            {
                throw new Exception($"Default upload handler '{options.DefaultUploadHandler}' not found.");
            }
        }
        else if (endpoint.UploadHandlers.Length == 1)
        { 
            var handlerName = endpoint.UploadHandlers[0];
            if (options.UploadHandlers is not null && options.UploadHandlers.TryGetValue(handlerName, out var handler))
            {
                return handler().SetType(handlerName);
            }
            else
            {
                throw new Exception($"Upload handler '{handlerName}' not found.");
            }
        }
        else
        {
            // all handlers defined
            return new DefaultUploadHandler(options.UploadHandlers?.Select(h => h.Value().SetType(h.Key)).ToArray() ?? []);
        }
    }

    private readonly IUploadHandler[] _handlers = handlers;
    private readonly bool _requiresTransaction = handlers.Any(h => h.RequiresTransaction);

    public bool RequiresTransaction => _requiresTransaction;

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
        for(int i = 0; i < _handlers.Length; i++)
        {
            _handlers[i].OnError(connection, context, exception);
        }
    }

    public async Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        object[] results = new object[_handlers.Length];
        for (int i = 0; i < _handlers.Length; i++)
        {
            results[i] = await _handlers[i].UploadAsync(connection, context, parameters);
        }
        return results;
    }
}
