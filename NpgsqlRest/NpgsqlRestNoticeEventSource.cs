using Npgsql;

namespace NpgsqlRest;

public readonly record struct NoticeEvent(
    PostgresNotice Notice,
    RoutineEndpoint? Endpoint,
    string? ExecutionId);

public class NpgsqlRestNoticeEventSource(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public static readonly HashSet<string> Paths = [];
    public static readonly Broadcaster<NoticeEvent> Broadcaster = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (Paths.Contains(context.Request.Path) is false)
        {
            await _next(context);
            return;
        }

        var executionId = context.Request.QueryString.HasValue ? context.Request.QueryString.Value[1..] : null;

        var cancellationToken = context.RequestAborted;
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate, max-age=0";
        context.Response.Headers.Connection = "keep-alive";

        if (NpgsqlRestMiddleware.Options.CustomServerSentEventsResponseHeaders.Count > 0)
        {
            foreach (var header in NpgsqlRestMiddleware.Options.CustomServerSentEventsResponseHeaders)
            {
                if (context.Response.Headers.ContainsKey(header.Key))
                {
                    context.Response.Headers[header.Key] = header.Value;
                }
                else
                {
                    context.Response.Headers.Append(header.Key, header.Value);
                }
            }
        }

        var connectionId = Guid.NewGuid();
        var reader = Broadcaster.Subscribe(connectionId);
        try
        {
            await foreach (var noticeEvent in reader.ReadAllAsync(cancellationToken))
            {
                if (noticeEvent.Endpoint?.InfoEventsScope == InfoEventsScope.Self)
                {
                    if (string.Equals(noticeEvent.ExecutionId, executionId, StringComparison.Ordinal) is false)
                    {
                        continue; // Skip events not matching the current execution ID
                    }
                }
                else if (noticeEvent.Endpoint?.InfoEventsScope == InfoEventsScope.Matching)
                {
                    if (context.User?.Identity?.IsAuthenticated is false && 
                        (noticeEvent.Endpoint.RequiresAuthorization is true || noticeEvent.Endpoint.AuthorizeRoles is not null))
                    {
                        continue; // Skip events for unauthorized users
                    }

                    if (noticeEvent.Endpoint.AuthorizeRoles is not null)
                    {
                        bool ok = false;
                        foreach (var claim in context.User?.Claims ?? [])
                        {
                            if (string.Equals(claim.Type, NpgsqlRestMiddleware.Options.AuthenticationOptions.DefaultRoleClaimType, StringComparison.Ordinal))
                            {
                                if (noticeEvent.Endpoint.AuthorizeRoles.Contains(claim.Value) is true)
                                {
                                    ok = true;
                                    break;
                                }
                            }
                        }
                        if (ok is false)
                        {
                            continue;
                        }
                    }
                }
                else if (noticeEvent.Endpoint?.InfoEventsScope == InfoEventsScope.Authenticated)
                {
                    if (context.User?.Identity?.IsAuthenticated is false)
                    {
                        continue; // Skip events for unauthorized users
                    }

                    if (noticeEvent.Endpoint.InfoEventsRoles is not null)
                    {
                        bool ok = false;
                        foreach (var claim in context.User?.Claims ?? [])
                        {
                            if (string.Equals(claim.Type, NpgsqlRestMiddleware.Options.AuthenticationOptions.DefaultRoleClaimType, StringComparison.Ordinal))
                            {
                                if (noticeEvent.Endpoint.InfoEventsRoles.Contains(claim.Value) is true)
                                {
                                    ok = true;
                                    break;
                                }
                            }
                        }
                        if (ok is false)
                        {
                            continue;
                        }
                    }
                }

                try
                {
                    await context.Response.WriteAsync($"data: {noticeEvent.Notice.MessageText}\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    NpgsqlRestMiddleware.Logger?.LogError(ex, "Failed to write notice event to response at path {path} (ExecutionId={executionId})", context.Request.Path, executionId);
                    continue;
                }
            }
        }
        finally
        {
            Broadcaster.Unsubscribe(connectionId);
        }
    }
}
