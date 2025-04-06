using Microsoft.AspNetCore.Routing;
using System.Xml.Linq;
using Npgsql;

namespace NpgsqlRest;

public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Could not read a value from {commandText} mapped to {method} {path}")]
    public static partial void CouldNotReadCommand(this ILogger logger, string commandText, string method, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not parse JSON body {body}, skipping path {path}.")]
    public static partial void CouldNotParseJson(this ILogger logger, string body, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not parse JSON body {body}, skipping path {path}. Error: {error}")]
    public static partial void CouldNotParseJson(this ILogger logger, string body, string path, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Endpoint {method} {url} changed request parameter type from body to query string because body will be used for parameter named \"{bodyParameterName}\".")]
    public static partial void EndpointTypeChanged(this ILogger logger, string method, string url, string bodyParameterName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created endpoint {method} {url}")]
    public static partial void EndpointCreated(this ILogger logger, string method, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid HTTP method '{method}' in comment for {description}. Using default '{usingDefault}'")]
    public static partial void InvalidHttpMethodComment(this ILogger logger, string method, string description, Method usingDefault);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid URL path segment '{urlPathSegment}' in comment for {description}. Using default '{defaultUrl}'")]
    public static partial void InvalidUrlPathSegmentComment(this ILogger logger, string urlPathSegment, string description, string defaultUrl);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set HTTP by the comment annotation to {method} {url}")]
    public static partial void CommentSetHttp(this ILogger logger, string description, Method method, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid parameter type '{type}' in comment for {description} Allowed values are QueryString or Query or BodyJson or Json. Using default '{defaultType}'")]
    public static partial void InvalidParameterTypeComment(this ILogger logger, string type, string description, RequestParamType defaultType);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set REQUEST PARAMETER TYPE by the comment annotation to {requestParamType}")]
    public static partial void CommentSetParameterType(this ILogger logger, string description, RequestParamType requestParamType);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set REQUIRED AUTHORIZATION by the comment annotation.")]
    public static partial void CommentSetAuth(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set REQUIRED AUTHORIZATION FOR ROLES {roles} by the comment annotation.")]
    public static partial void CommentSetAuthRoles(this ILogger logger, string description, HashSet<string> roles);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set ALLOW ANONYMOUS by the comment annotation.")]
    public static partial void CommentSetAnon(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set COMMAND TIMEOUT by the comment annotation to {parsedTimeout} seconds")]
    public static partial void CommentSetTimeout(this ILogger logger, string description, string parsedTimeout);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid command timeout '{timeout}' in comment for {description}. Using default command timeout '{defaultTimeout}'")]
    public static partial void InvalidTimeoutComment(this ILogger logger, string timeout, string description, int? defaultTimeout);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid request headers mode '{mode}' in comment for {description} Allowed values are Ignore or Context or Parameter. Using default '{defaultRequestHeadersMode}'")]
    public static partial void InvalidRequestHeadersModeComment(this ILogger logger, string mode, string description, RequestHeadersMode defaultRequestHeadersMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set REQUEST HEADERS MODE by the comment annotation to \"{requestHeadersMode}\"")]
    public static partial void CommentSetRequestHeadersMode(this ILogger logger, string description, string requestHeadersMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set REQUEST HEADERS PARAMETER NAME by the comment annotation to \"{requestHeadersParamName}\"")]
    public static partial void CommentSetRequestHeadersParamName(this ILogger logger, string description, string requestHeadersParamName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set BODY PARAMETER NAME by the comment annotation to \"{bodyParamName}\"")]
    public static partial void CommentSetBodyParamName(this ILogger logger, string description, string bodyParamName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid text response null handling mode '{mode}' in comment for {description}. Allowed values are EmptyString or NullLiteral or NoContent. Using default '{textResponseNullHandling}'")]
    public static partial void InvalidResponseNullHandlingModeComment(this ILogger logger, string mode, string description, TextResponseNullHandling textResponseNullHandling);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set TEXT RESPONSE NULL HANDLING by the comment annotation to \"{textResponseNullHandling}\"")]
    public static partial void CommentSetTextResponseNullHandling(this ILogger logger, string description, string textResponseNullHandling);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid query string null handling mode '{mode}' in comment for {description}. Allowed values are EmptyString or NullLiteral or Ignore. Using default '{queryStringNullHandling}'\"")]
    public static partial void InvalidQueryStringNullHandlingComment(this ILogger logger, string mode, string description, QueryStringNullHandling queryStringNullHandling);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set QUERY STRING NULL HANDLING by the comment annotation to \"{queryStringNullHandling}\"")]
    public static partial void CommentSetQueryStringNullHandling(this ILogger logger, string description, QueryStringNullHandling queryStringNullHandling);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set Content-Type HEADER by the comment annotation to \"{headerValue}\"")]
    public static partial void CommentSetContentType(this ILogger logger, string description, string headerValue);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set {headerName} HEADER by the comment annotation to \"{headerValue}\"")]
    public static partial void CommentSetHeader(this ILogger logger, string description, string headerName, string headerValue);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set LOGIN RESPONSE by the comment annotation.")]
    public static partial void CommentSetLogin(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set LOGOUT RESPONSE by the comment annotation.")]
    public static partial void CommentSetLogout(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Error, Message = "{description} is designated as login routine and it returns a status field that is not either boolean or numeric.")]
    public static partial void WrongStatusType(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Endpoints are using Login and the DefaultAuthenticationType is null. DefaultAuthenticationType was set to {name}")]
    public static partial void SetDefaultAuthenticationType(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set BUFFER ROWS by the comment annotation to {parsedBuffer}")]
    public static partial void CommentBufferRows(this ILogger logger, string description, string parsedBuffer);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid buffer rows '{buffer}' in comment for {description}. Using the default buffer rows '{defaultBufferRows}'")]
    public static partial void InvalidBufferRows(this ILogger logger, string buffer, string description, ulong defaultBufferRows);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set RAW MODE by the comment annotation.")]
    public static partial void CommentSetRawMode(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set SEPARATOR by the comment annotation to {value}.")]
    public static partial void CommentSetRawValueSeparator(this ILogger logger, string description, string value);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set NEW LINE by the comment annotation to {value}.")]
    public static partial void CommentSetRawNewLineSeparator(this ILogger logger, string description, string value);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set COLUMN NAMES by the comment annotation.")]
    public static partial void CommentRawSetColumnNames(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{description} has set CACHED by the comment annotation, routine doesn't return a single value. Routine will NOT be cached. Only single values can be cached.")]
    public static partial void CommentInvalidCache(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{description} has set CACHED PARAMETER NAME to {param} by the comment annotation, but that parameter doesn't exists on this routine either converted or original. This cache parameter will be ignored.")]
    public static partial void CommentInvalidCacheParam(this ILogger logger, string description, string param);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set CACHED with parameters {cachedParams} by the comment annotation.")]
    public static partial void CommentCached(this ILogger logger, string description, IEnumerable<string> cachedParams);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set PARSE RESPONSE to true by the comment annotation.")]
    public static partial void CommentParseResponse(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{description} has set PARSE RESPONSE to true by the comment annotation but routine doesn't return a single value. Routine will NOT be parsed. Only single values can be parsed.")]
    public static partial void CommentInvalidParseResponse(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set CACHE EXPIRES IN to {value} by the comment annotation.")]
    public static partial void CommentCacheExpiresIn(this ILogger logger, string description, TimeSpan value);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{description} can't set CACHE EXPIRES IN value by the comment annotation. Invalid interval value: {value}")]
    public static partial void InvalidCacheExpiresIn(this ILogger logger, string description, string value);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{description} tried to set CONNECTION NAME to {conn} but that connection could not be found in the ConnectionStrings dictionary.")]
    public static partial void CommentInvalidConnectionName(this ILogger logger, string description, string conn);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{description} tried to set CONNECTION NAME but the connection name was not initialized. Did you forget to set the connection name?")]
    public static partial void CommentEmptyConnectionName(this ILogger logger, string description);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has set CONNECTION NAME to {conn} by the comment annotation.")]
    public static partial void CommentConnectionName(this ILogger logger, string description, string conn);

    [LoggerMessage(Level = LogLevel.Information, Message = "{description} has been DISABLED by the comment annotation.")]
    public static partial void CommentDisabled(this ILogger logger, string description);
}

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