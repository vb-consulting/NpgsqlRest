using Npgsql;

namespace NpgsqlRest;

public static class NpgsqlRestLogger
{
    private const string Info = "INFO";
    private const string Notice = "NOTICE";
    private const string Log = "LOG";
    private const string Warning = "WARNING";
    private const string Debug = "DEBUG";
    private const string Error = "ERROR";
    private const string Panic = "PANIC";
    private const string LogPattern = "{where}:\n{message}";

    internal static readonly LogDefineOptions LogDefineOptions = new() { SkipEnabledCheck = true };

    private static readonly Action<ILogger, string?, string, Exception?> __LogInformationCallback =
        LoggerMessage.Define<string?, string>(LogLevel.Information, 0, LogPattern, LogDefineOptions);

    private static readonly Action<ILogger, string?, string, Exception?> __LogWarningCallback =
        LoggerMessage.Define<string?, string>(LogLevel.Warning, 1, LogPattern, LogDefineOptions);

    private static readonly Action<ILogger, string?, string, Exception?> __LogDebugCallback =
        LoggerMessage.Define<string?, string>(LogLevel.Debug, 2, LogPattern, LogDefineOptions);

    private static readonly Action<ILogger, string?, string, Exception?> __LogErrorCallback =
        LoggerMessage.Define<string?, string>(LogLevel.Error, 3, LogPattern, LogDefineOptions);

    private static readonly Action<ILogger, string?, string, Exception?> __LogTraceCallback =
        LoggerMessage.Define<string?, string>(LogLevel.Trace, 4, LogPattern, LogDefineOptions);

    private static void LogInformation(ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Information) is true)
        {
            __LogInformationCallback(logger, where, message, null);
        }
    }

    private static void LogWarning(ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Warning) is true)
        {
            __LogWarningCallback(logger, where, message, null);
        }
    }

    private static void LogDebug(ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Debug) is true)
        {
            __LogDebugCallback(logger, where, message, null);
        }
    }

    private static void LogError(ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Error) is true)
        {
            __LogErrorCallback(logger, where, message, null);
        }
    }

    private static void LogTrace(ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Trace) is true)
        {
            __LogTraceCallback(logger, where, message, null);
        }
    }

    public static void LogEndpoint(ILogger? logger, RoutineEndpoint endpoint, string parameters, string command)
    {
        if (logger?.IsEnabled(LogLevel.Information) is true && endpoint.LogCallback is not null)
        {
            endpoint.LogCallback(logger, parameters, command, null);
        }
    }

#pragma warning disable IDE0079 // Remove unnecessary suppression
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    public static void LogConnectionNotice(ILogger? logger, NpgsqlNoticeEventArgs args, PostgresConnectionNoticeLoggingMode mode)
    {
        if (logger is null)
        {
            return;
        }
        if (string.Equals(Info, args.Notice.Severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Log, args.Notice.Severity, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Notice, args.Notice.Severity, StringComparison.OrdinalIgnoreCase))
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogInformation(args.Notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogInformation(logger, args.Notice?.Where?.Split('\n').LastOrDefault() ?? "", args.Notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogInformation(logger, args.Notice?.Where, args.Notice?.MessageText!);
            }
        }
        else if (string.Equals(Warning, args.Notice.Severity, StringComparison.OrdinalIgnoreCase))
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogWarning(args.Notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogWarning(logger, args.Notice?.Where?.Split('\n').Last() ?? "", args.Notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogWarning(logger, args.Notice?.Where, args.Notice?.MessageText!);
            }
        }
        else if (string.Equals(Debug, args.Notice.Severity, StringComparison.OrdinalIgnoreCase))
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogDebug(args.Notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogDebug(logger, args.Notice?.Where?.Split('\n').Last() ?? "", args.Notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogDebug(logger, args.Notice?.Where, args.Notice?.MessageText!);
            }
        }
        else if (string.Equals(Error, args.Notice.Severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Panic, args.Notice.Severity, StringComparison.OrdinalIgnoreCase))
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogError(args.Notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogError(logger, args.Notice?.Where?.Split('\n').Last() ?? "", args.Notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogError(logger, args.Notice?.Where, args.Notice?.MessageText!);
            }
        }
        else
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogTrace(args.Notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogTrace(logger, args.Notice?.Where?.Split('\n').Last() ?? "", args.Notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogTrace(logger, args.Notice?.Where, args.Notice?.MessageText!);
            }
        }
    }
}