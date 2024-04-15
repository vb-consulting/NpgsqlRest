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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid HTTP method '{method}' in comment for routine '{schema}.{name}'. Using default '{usingDefault}'")]
    public static partial void InvalidHttpMethodComment(this ILogger logger, string method, string schema, string name, Method usingDefault);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid URL path segment '{urlPathSegment}' in comment for routine '{schema}.{name}'. Using default '{defaultUrl}'")]
    public static partial void InvalidUrlPathSegmentComment(this ILogger logger, string urlPathSegment, string schema, string name, string defaultUrl);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set HTTP by the comment annotation to {method} {url}")]
    public static partial void CommentSetHttp(this ILogger logger, RoutineType type, string schema, string name, Method method, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid parameter type '{type}' in comment for routine '{schema}.{name}' Allowed values are QueryString or Query or BodyJson or Json. Using default '{defaultType}'")]
    public static partial void InvalidParameterTypeComment(this ILogger logger, string type, string schema, string name, RequestParamType defaultType);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set REQUEST PARAMETER TYPE by the comment annotation to {requestParamType}")]
    public static partial void CommentSetParameterType(this ILogger logger, RoutineType type, string schema, string name, RequestParamType requestParamType);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set REQUIRED AUTHORIZATION by the comment annotation.")]
    public static partial void CommentSetAuth(this ILogger logger, RoutineType type, string schema, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set REQUIRED AUTHORIZATION FOR ROLES {roles} by the comment annotation.")]
    public static partial void CommentSetAuthRoles(this ILogger logger, RoutineType type, string schema, string name, HashSet<string> roles);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set ALLOW ANONYMOUS by the comment annotation.")]
    public static partial void CommentSetAnon(this ILogger logger, RoutineType type, string schema, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set COMMAND TIMEOUT by the comment annotation to {parsedTimeout} seconds")]
    public static partial void CommentSetTimeout(this ILogger logger, RoutineType type, string schema, string name, string parsedTimeout);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid command timeout '{timeout}' in comment for routine '{schema}.{name}'. Using default command timeout '{defaultTimeout}'")]
    public static partial void InvalidTimeoutComment(this ILogger logger, string timeout, string schema, string name, int? defaultTimeout);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid request headers mode '{mode}' in comment for routine '{schema}.{name}' Allowed values are Ignore or Context or Parameter. Using default '{defaultRequestHeadersMode}'")]
    public static partial void InvalidRequestHeadersModeComment(this ILogger logger, string mode, string schema, string name, RequestHeadersMode defaultRequestHeadersMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set REQUEST HEADERS MODE by the comment annotation to \"{requestHeadersMode}\"")]
    public static partial void CommentSetRequestHeadersMode(this ILogger logger, RoutineType type, string schema, string name, string requestHeadersMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set REQUEST HEADERS PARAMETER NAME by the comment annotation to \"{requestHeadersParamName}\"")]
    public static partial void CommentSetRequestHeadersParamName(this ILogger logger, RoutineType type, string schema, string name, string requestHeadersParamName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set BODY PARAMETER NAME by the comment annotation to \"{bodyParamName}\"")]
    public static partial void CommentSetBodyParamName(this ILogger logger, RoutineType type, string schema, string name, string bodyParamName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid text response null handling mode '{mode}' in comment for routine '{schema}.{name}'. Allowed values are EmptyString or NullLiteral or NoContent. Using default '{textResponseNullHandling}'")]
    public static partial void InvalidResponseNullHandlingModeComment(this ILogger logger, string mode, string schema, string name, TextResponseNullHandling textResponseNullHandling);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set TEXT RESPONSE NULL HANDLING by the comment annotation to \"{textResponseNullHandling}\"")]
    public static partial void CommentSetTextResponseNullHandling(this ILogger logger, RoutineType type, string schema, string name, string textResponseNullHandling);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid query string null handling mode '{mode}' in comment for routine '{schema}.{name}'. Allowed values are EmptyString or NullLiteral or Ignore. Using default '{queryStringNullHandling}'\"")]
    public static partial void InvalidQueryStringNullHandlingComment(this ILogger logger, string mode, string schema, string name, QueryStringNullHandling queryStringNullHandling);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set QUERY STRING NULL HANDLING by the comment annotation to \"{queryStringNullHandling}\"")]
    public static partial void CommentSetQueryStringNullHandling(this ILogger logger, RoutineType type, string schema, string name, QueryStringNullHandling queryStringNullHandling);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set Content-Type HEADER by the comment annotation to \"{headerValue}\"")]
    public static partial void CommentSetContentType(this ILogger logger, RoutineType type, string schema, string name, string headerValue);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set {headerName} HEADER by the comment annotation to \"{headerValue}\"")]
    public static partial void CommentSetHeader(this ILogger logger, RoutineType type, string schema, string name, string headerName, string headerValue);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set LOGIN RESPONSE by the comment annotation.")]
    public static partial void CommentSetLogin(this ILogger logger, RoutineType type, string schema, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set LOGOUT RESPONSE by the comment annotation.")]
    public static partial void CommentSetLogout(this ILogger logger, RoutineType type, string schema, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "{type} {schema}.{name} is designated as login routine and it returns a status field that is not either boolean or numeric.")]
    public static partial void WrongStatusType(this ILogger logger, RoutineType type, string schema, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Endpoints are using Login and the DefaultAuthenticationType is null. DefaultAuthenticationType was set to {name}")]
    public static partial void SetDefaultAuthenticationType(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} {schema}.{name} has set BUFFER ROWS by the comment annotation to {parsedBuffer} seconds")]
    public static partial void CommentBufferRows(this ILogger logger, RoutineType type, string schema, string name, string parsedBuffer);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid buffer rows '{buffer}' in comment for routine '{schema}.{name}'. Using the default buffer rows '{defaultBufferRows}'")]
    public static partial void InvalidBufferRows(this ILogger logger, string buffer, string schema, string name, ulong defaultBufferRows);
}

internal static class Logger
{
    private const string Info = "INFO";
    private const string Notice = "NOTICE";
    private const string Log = "LOG";
    private const string Warning = "WARNING";
    private const string Debug = "DEBUG";
    private const string Error = "ERROR";
    private const string Panic = "PANIC";
    private const string LogPattern = "{where} {message}";

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

    private static void LogInformation(ref ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Information) is true)
        {
            __LogInformationCallback(logger, where, message, null);
        }
    }

    private static void LogWarning(ref ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Warning) is true)
        {
            __LogWarningCallback(logger, where, message, null);
        }
    }

    private static void LogDebug(ref ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Debug) is true)
        {
            __LogDebugCallback(logger, where, message, null);
        }
    }

    private static void LogError(ref ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Error) is true)
        {
            __LogErrorCallback(logger, where, message, null);
        }
    }

    private static void LogTrace(ref ILogger? logger, string? where, string message)
    {
        if (logger?.IsEnabled(LogLevel.Trace) is true)
        {
            __LogTraceCallback(logger, where, message, null);
        }
    }

    public static void LogEndpoint(ref ILogger? logger, ref RoutineEndpoint endpoint, string parameters, string command)
    {
        if (logger?.IsEnabled(LogLevel.Information) is true && endpoint.LogCallback is not null)
        {
            endpoint.LogCallback(logger, parameters, command, null);
        }
    }

    public static void LogConnectionNotice(ref ILogger? logger, ref NpgsqlNoticeEventArgs args)
    {
        if (logger is null)
        {
            return;
        }
        var severity = args.Notice.Severity;
        var where = string.Concat(args.Notice.Where, " ", severity);
        if (string.Equals(Info, severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Log, severity, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Notice, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogInformation(ref logger, where, args.Notice.MessageText);
        }
        else if (string.Equals(Warning, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogWarning(ref logger, where, args.Notice.MessageText);
        }
        else if (string.Equals(Debug, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogDebug(ref logger, where, args.Notice.MessageText);
        }
        else if (string.Equals(Error, severity, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(Panic, severity, StringComparison.OrdinalIgnoreCase))
        {
            LogError(ref logger, where, args.Notice.MessageText);
        }
        else
        {
            LogTrace(ref logger, where, args.Notice.MessageText);
        }
    }
}