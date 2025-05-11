namespace NpgsqlRest.UploadHandlers;

/*


                logger?.LogInformation("Uploaded file {FileName} ({ContentType}, {Length} bytes) to large object {resultOid}", 
                    formFile.FileName, formFile.ContentType, formFile.Length, resultOid);
 */
public static partial class UploadLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "File {fileName} ({contentType}, {length} bytes) is not a valid CSV file. Status: {status}")]
    public static partial void NotValidCsvFile(this ILogger logger, string fileName, string contentType, long length, string status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded file {fileName} {contentType}, {length} bytes) as CSV using command {command}")]
    public static partial void UploadedCsvFile(this ILogger logger, string fileName, string contentType, long length, string command);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded file {fileName} {contentType}, {length} bytes) to file path {currentFilePath}")]
    public static partial void UploadedFileToFileSystem(this ILogger logger, string fileName, string contentType, long length, string currentFilePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded file {fileName} {contentType}, {length} bytes) to large object {resultOid}")]
    public static partial void UploadedFileToLargeObject(this ILogger logger, string fileName, string contentType, long length, object? resultOid);
}