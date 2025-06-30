using Npgsql;

namespace NpgsqlRest;

public readonly record struct NoticeEvent(
    PostgresNotice Notice,
    RoutineEndpoint? Endpoint,
    string? ConnectionId);

public class NpgsqlRestNoticeEventSource(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public static readonly Dictionary<string, Broadcast<NoticeEvent>> Subscribers = [];

    public async Task InvokeAsync(HttpContext context)
    {
        if (Subscribers.TryGetValue(context.Request.Path, out var broadcast))
        {
            var connectionId = context.Request.QueryString.HasValue ? context.Request.QueryString.Value[1..] : null;

            var cancellationToken = context.RequestAborted;

            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            long lastVersion = Broadcast.InitialVersion;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (broadcast.TryWaitForNewMessage(lastVersion, cancellationToken, out var result))
                    {
                        if (string.Equals(result.Payload.ConnectionId, connectionId, StringComparison.Ordinal) is false)
                        {
                            continue; // Skip messages not meant for this connection
                        }

                        lastVersion = result.Version;
                        await context.Response.WriteAsync(string.Concat("data: ", result.Payload.Notice.MessageText, "\n\n"), cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when client disconnects
            }

            return;
        }
        await _next(context);
    }
}
