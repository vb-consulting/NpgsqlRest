using Npgsql;

namespace NpgsqlRest;

public static class NpgsqlRestLogger
{
    private const string LogPattern = "{where}:\n{message}";

    internal static readonly LogDefineOptions LogDefineOptions = new() { SkipEnabledCheck = true };

    private static readonly Action<ILogger, string?, string, Exception?> __LogInformationCallback =
        LoggerMessage.Define<string?, string>(LogLevel.Information, 0, LogPattern, LogDefineOptions);

    private static readonly Action<ILogger, string?, string, Exception?> __LogWarningCallback =
        LoggerMessage.Define<string?, string>(LogLevel.Warning, 1, LogPattern, LogDefineOptions);

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

    private static void LogTrace(ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Trace) is true)
        {
            __LogTraceCallback(logger, where, message, null);
        }
    }

    public static void LogEndpoint(ILogger? logger, RoutineEndpoint endpoint, string parameters, string command)
    {
        if (logger?.IsEnabled(LogLevel.Debug) is true && endpoint.LogCallback is not null)
        {
            endpoint.LogCallback(logger, parameters, command, null);
        }
    }

#pragma warning disable IDE0079 // Remove unnecessary suppression
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    public static void LogConnectionNotice(ILogger? logger, PostgresNotice notice, PostgresConnectionNoticeLoggingMode mode)
    {
        if (logger is null)
        {
            return;
        }
        if (notice.IsInfo() || notice.IsNotice())
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogInformation(notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogInformation(logger, notice?.Where?.Split('\n').LastOrDefault() ?? "", notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogInformation(logger, notice?.Where, notice?.MessageText!);
            }
        }
        else if (notice.IsWarning())
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogWarning(notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogWarning(logger, notice?.Where?.Split('\n').Last() ?? "", notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogWarning(logger, notice?.Where, notice?.MessageText!);
            }
        }
        else
        {
            if (mode == PostgresConnectionNoticeLoggingMode.MessageOnly)
            {
                logger.LogTrace(notice.MessageText);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FirstStackFrameAndMessage)
            {
                LogTrace(logger, notice?.Where?.Split('\n').Last() ?? "", notice?.MessageText!);
            }
            else if (mode == PostgresConnectionNoticeLoggingMode.FullStackAndMessage)
            {
                LogTrace(logger, notice?.Where, notice?.MessageText!);
            }
        }
    }
}