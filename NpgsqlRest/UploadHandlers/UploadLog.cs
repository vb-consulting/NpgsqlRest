namespace NpgsqlRest.UploadHandlers;

public static partial class UploadLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded file {fileName} ({contentType}, {length} bytes) as CSV using command {command}")]
    public static partial void UploadedCsvFile(this ILogger logger, string fileName, string contentType, long length, string command);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded file {fileName} ({contentType}, {length} bytes) to file path {currentFilePath}")]
    public static partial void UploadedFileToFileSystem(this ILogger logger, string fileName, string contentType, long length, string currentFilePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded file {fileName} ({contentType}, {length} bytes) to large object {resultOid}")]
    public static partial void UploadedFileToLargeObject(this ILogger logger, string fileName, string contentType, long length, object? resultOid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "File upload check failed for type {type}. File {fileName} ({contentType}, {length} bytes) failed with status {status}.")]
    public static partial void FileUploadFailed(this ILogger logger, string? type, string fileName, string contentType, long length, UploadFileStatus status);
}