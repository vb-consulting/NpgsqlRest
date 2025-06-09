using System.Text;
using Npgsql;

namespace NpgsqlRest.UploadHandlers.Handlers;

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

    public async Task<string> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        StringBuilder result = new(100);
        for (int i = 0; i < _handlers.Length; i++)
        {
            var segment = await _handlers[i].UploadAsync(connection, context, parameters);
            if (i == 0 && _handlers.Length == 1)
            {
                result.Append(segment);
            }
            else if (i == 0 && _handlers.Length > 1)
            {
                result.Append(segment[..^1]).Append(',');
            }
            else if (i > 0 && i < _handlers.Length - 1)
            {
                result.Append(segment[1..^1]).Append(',');
            }
            else if (i > 0 && i == _handlers.Length - 1)
            {
                result.Append(segment[1..]);
            }
        }
        return result.ToString();
    }
}
