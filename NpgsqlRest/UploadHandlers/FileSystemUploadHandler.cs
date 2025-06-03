using System.Text;
using Npgsql;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

public class FileSystemUploadHandler(NpgsqlRestUploadOptions options, ILogger? logger) : IUploadHandler
{
    private string? _type = null;
    private string[]? _uploadedFiles = null;

    private const string PathParam = "path";
    private const string FileParam = "file";
    private const string UniqueNameParam = "unique_name";
    private const string CreatePathParam = "create_path";

    public bool RequiresTransaction => false;
    public string[] Parameters => [
        UploadExtensions.IncludedMimeTypeParam, UploadExtensions.ExcludedMimeTypeParam, UploadExtensions.BufferSize,
        PathParam, FileParam, UniqueNameParam, CreatePathParam,
        //!
        FileCheckExtensions.CheckTextParam, 
        FileCheckExtensions.CheckImageParam,
        FileCheckExtensions.TestBufferSizeParam, 
        FileCheckExtensions.NonPrintableThresholdParam
    ];

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return this;
    }

    public async Task<string> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        var (includedMimeTypePatterns, excludedMimeTypePatterns, bufferSize) = options.ParseSharedParameters(parameters);

        var basePath = options.DefaultUploadHandlerOptions.FileSystemHandlerPath;
        var useUniqueFileName = options.DefaultUploadHandlerOptions.FileSystemHandlerUseUniqueFileName;
        string? newFileName = null;
        bool createPathIfNotExists = options.DefaultUploadHandlerOptions.FileSystemHandlerCreatePathIfNotExists;
        bool checkText = false;
        bool checkImage = false;
        int testBufferSize = options.DefaultUploadHandlerOptions.TextTestBufferSize;
        int nonPrintableThreshold = options.DefaultUploadHandlerOptions.TextNonPrintableThreshold;

        if (parameters is not null)
        {
            if (parameters.TryGetValue(PathParam, out var path) && !string.IsNullOrEmpty(path))
            {
                basePath = path;
            }
            if (parameters.TryGetValue(FileParam, out var newFileNameStr) && !string.IsNullOrEmpty(newFileNameStr))
            {
                newFileName = newFileNameStr;
            }
            if (parameters.TryGetValue(UniqueNameParam, out var useUniqueFileNameStr) 
                && bool.TryParse(useUniqueFileNameStr, out var useUniqueFileNameParsed))
            {
                useUniqueFileName = useUniqueFileNameParsed;
            }
            if (parameters.TryGetValue(CreatePathParam, out var createPathIfNotExistsStr)
                && bool.TryParse(createPathIfNotExistsStr, out var createPathIfNotExistsParsed))
            {
                createPathIfNotExists = createPathIfNotExistsParsed;
            }
            if (parameters.TryGetValue(FileCheckExtensions.CheckTextParam, out var checkTextParamStr)
                && bool.TryParse(checkTextParamStr, out var checkTextParamParsed))
            {
                checkText = checkTextParamParsed;
            }
            if (parameters.TryGetValue(FileCheckExtensions.CheckImageParam, out var checkImageParamStr)
                && bool.TryParse(checkImageParamStr, out var checkImageParamParsed))
            {
                checkImage = checkImageParamParsed;
            }
            if (parameters.TryGetValue(FileCheckExtensions.TestBufferSizeParam, out var testBufferSizeStr) && int.TryParse(testBufferSizeStr, out var testBufferSizeParsed))
            {
                testBufferSize = testBufferSizeParsed;
            }
            if (parameters.TryGetValue(FileCheckExtensions.NonPrintableThresholdParam, out var nonPrintableThresholdStr) && int.TryParse(nonPrintableThresholdStr, out var nonPrintableThresholdParsed))
            {
                nonPrintableThreshold = nonPrintableThresholdParsed;
            }
        }

        if (createPathIfNotExists is true && Directory.Exists(basePath) is false)
        {
            Directory.CreateDirectory(basePath);
        }

        _uploadedFiles = new string[context.Request.Form.Files.Count];
        StringBuilder result = new(context.Request.Form.Files.Count * 100);
        result.Append('[');
        
        int fileId = 0;
        for (int i = 0; i < context.Request.Form.Files.Count; i++)
        {
            IFormFile formFile = context.Request.Form.Files[i];

            if (fileId > 0)
            {
                result.Append(',');
            }
            string fileName = newFileName ?? formFile.FileName;
            if (useUniqueFileName)
            {
                string extension = Path.GetExtension(fileName);
                fileName = $"{Guid.NewGuid()}{extension}";
            }

            var currentFilePath = Path.Combine(basePath, fileName);

            // Build the result JSON
            if (_type is not null)
            {
                result.Append("{\"type\":");
                result.Append(SerializeString(_type));
                result.Append(",\"fileName\":");
            }
            else
            {
                result.Append("{\"fileName\":");
            }

            result.Append(SerializeString(formFile.FileName));
            result.Append(",\"contentType\":");
            result.Append(SerializeString(formFile.ContentType));
            result.Append(",\"size\":");
            result.Append(formFile.Length);
            result.Append(",\"filePath\":");
            result.Append(SerializeString(currentFilePath));

            UploadFileStatus status = UploadFileStatus.Ok;
            if (formFile.ContentType.CheckMimeTypes(includedMimeTypePatterns, excludedMimeTypePatterns) is false)
            {
                status = UploadFileStatus.InvalidMimeType;
            }
            if (status == UploadFileStatus.Ok && (checkText is true || checkImage is true))
            {
                if (checkText is true)
                {
                    status = await formFile.CheckFileStatus(testBufferSize, nonPrintableThreshold, checkNewLines: false);
                }
                if (status == UploadFileStatus.Ok && checkImage is true)
                {
                    if (await formFile.IsImage() is false)
                    {
                        status = UploadFileStatus.NotAnImage;
                    }
                }
            }
            result.Append(",\"success\":");
            result.Append(status == UploadFileStatus.Ok ? "true" : "false");
            result.Append(",\"status\":");
            result.Append(SerializeString(status.ToString()));
            result.Append('}');
            if (status != UploadFileStatus.Ok)
            {
                logger?.FileUploadFailed(_type, formFile.FileName, formFile.ContentType, formFile.Length, status);
                fileId++;
                continue;
            }

            using (var fileStream = new FileStream(currentFilePath, FileMode.Create))
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                using var sourceStream = formFile.OpenReadStream();

                while ((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }
            _uploadedFiles[i] = currentFilePath;

            if (options.LogUploadEvent)
            {
                logger?.UploadedFileToFileSystem(formFile.FileName, formFile.ContentType, formFile.Length, currentFilePath);
            }
            fileId++;
        }

        result.Append(']');
        return result.ToString();
    }

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
        if (_uploadedFiles is not null)
        {
            for(int i = 0; i < _uploadedFiles.Length; i++)
            {
                var filePath = _uploadedFiles[i];
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch
                {
                    // Ignore any exceptions during cleanup
                }
            }
        }
    }
}