using Npgsql;

namespace NpgsqlRest;

internal static class Logging
{
    private const string info = "INFO";
    private const string notice = "NOTICE";
    private const string log = "LOG";
    private const string warning = "WARNING";
    private const string debug = "DEBUG";
    private const string error = "ERROR";
    private const string panic = "PANIC";

    public static void LogConnectionNotice(ref ILogger? logger, ref NpgsqlRestOptions options, ref NpgsqlNoticeEventArgs args)
    {
        var severity = args.Notice.Severity;
        var msg = $"{args.Notice.Where}:{Environment.NewLine}{args.Notice.MessageText}{Environment.NewLine}";

        if (severity.StartsWith(info) || severity.StartsWith(notice) || severity.StartsWith(log))
        {
            LogInfo(ref logger, ref options, msg);
        }
        else if (severity.StartsWith(warning))
        {
            LogWarning(ref logger, ref options, msg);
        }
        else if (severity.StartsWith(debug))
        {
            LogDebug(ref logger, ref options, msg);
        }
        else if (severity.StartsWith(error) || severity.StartsWith(panic))
        {
            LogError(ref logger, ref options, msg);
        }
        else
        {
            LogTrace(ref logger, ref options, msg);
        }
    }

    public static void LogInfo(ref ILogger? logger, ref NpgsqlRestOptions options, string? message, params object?[] args)
    {
        if (logger is null || options.LogLevel == LogLevel.None)
        {
            return;
        }
        if (options.LogLevel <= LogLevel.Information)
        {
            logger.LogInformation(message, args);
        }
    }

    public static void LogWarning(ref ILogger? logger, ref NpgsqlRestOptions options, string? message, params object?[] args)
    {
        if (logger is null || options.LogLevel == LogLevel.None)
        {
            return;
        }
        if (options.LogLevel <= LogLevel.Warning)
        {
            logger.LogWarning(message, args);
        }
    }

    public static void LogDebug(ref ILogger? logger, ref NpgsqlRestOptions options, string? message, params object?[] args)
    {
        if (logger is null || options.LogLevel == LogLevel.None)
        {
            return;
        }
        if (options.LogLevel <= LogLevel.Debug)
        {
            logger.LogDebug(message, args);
        }
    }

    public static void LogError(ref ILogger? logger, ref NpgsqlRestOptions options, string? message, params object?[] args)
    {
        if (logger is null || options.LogLevel == LogLevel.None)
        {
            return;
        }
        if (options.LogLevel <= LogLevel.Error)
        {
            logger.LogError(message, args);
        }
    }
    
    public static void LogTrace(ref ILogger? logger, ref NpgsqlRestOptions options, string? message, params object?[] args)
    {
        if (logger is null || options.LogLevel == LogLevel.None)
        {
            return;
        }
        if (options.LogLevel <= LogLevel.Trace)
        {
            logger.LogTrace(message, args);
        }
    }
}