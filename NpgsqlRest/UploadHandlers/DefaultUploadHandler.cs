using Npgsql;

namespace NpgsqlRest.UploadHandlers;

public class DefaultUploadHandler(params IUploadHandler[] handlers) : IUploadHandler
{
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
