using Npgsql;

namespace NpgsqlRest;

internal static class Logger
{
    private const string Info = "INFO";
    private const string Notice = "NOTICE";
    private const string Log = "LOG";
    private const string Warning = "WARNING";
    private const string Debug = "DEBUG";
    private const string Error = "ERROR";
    private const string Panic = "PANIC";

    public static void LogConnectionNotice(ref ILogger? logger, ref NpgsqlNoticeEventArgs args)
    {
        if (logger is null)
        {
            return;
        }
        var severity = args.Notice.Severity;
        var msg = $"{args.Notice.Where}:{Environment.NewLine}{args.Notice.MessageText}{Environment.NewLine}";

        if (string.Equals(Info, severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Log, severity, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Notice, severity, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(msg);
        }
        else if (string.Equals(Warning, severity, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(msg);
        }
        else if (string.Equals(Debug, severity, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(msg);
        }
        else if (string.Equals(Error, severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Panic, severity, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(msg);
        }
        logger.LogTrace(msg);
    }
}