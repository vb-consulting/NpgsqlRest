using Npgsql;

namespace NpgsqlRest;

internal static class Logging
{
    private const string Info = "INFO";
    private const string Notice = "NOTICE";
    private const string Log = "LOG";
    private const string Warning = "WARNING";
    private const string Debug = "DEBUG";
    private const string Error = "ERROR";
    private const string Panic = "PANIC";

    public static void LogConnectionNotice(ref ILogger? logger, ref NpgsqlRestOptions options, ref NpgsqlNoticeEventArgs args)
    {
        var severity = args.Notice.Severity;
        var msg = $"{args.Notice.Where}:{Environment.NewLine}{args.Notice.MessageText}{Environment.NewLine}";

        if (string.Equals(Info, severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Log, severity, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Notice, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogInfo(ref logger, ref options, msg);
        }
        else if (string.Equals(Warning, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogWarning(ref logger, ref options, msg);
        }
        else if (string.Equals(Debug, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogDebug(ref logger, ref options, msg);
        }
        else if (string.Equals(Error, severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Panic, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogError(ref logger, ref options, msg);
        }
        LogTrace(ref logger, ref options, msg);
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