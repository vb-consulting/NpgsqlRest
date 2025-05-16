using System.Net.Mime;
using System.Text;
using Npgsql;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

public class FileSystemUploadHandler(NpgsqlRestUploadOptions options, ILogger? logger) : IUploadHandler
{
    private readonly string[] _parameters = [
        "included_mime_types",
        "excluded_mime_types",
        "path",
        "file",
        "unique_name",
        "create_path",
        "buffer_size"
    ];
    private string? _type = null;
    private string[]? _uploadedFiles = null;

    public bool RequiresTransaction => false;
    public string[] Parameters => _parameters;

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return this;
    }

    public async Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        string[]? includedMimeTypePatterns = options.DefaultUploadHandlerOptions.FileSystemIncludedMimeTypePatterns;
        string[]? excludedMimeTypePatterns = options.DefaultUploadHandlerOptions.FileSystemExcludedMimeTypePatterns;

        var basePath = options.DefaultUploadHandlerOptions.FileSystemHandlerPath;
        var useUniqueFileName = options.DefaultUploadHandlerOptions.FileSystemHandlerUseUniqueFileName;
        var bufferSize = options.DefaultUploadHandlerOptions.FileSystemHandlerBufferSize;
        string? newFileName = null;
        bool createPathIfNotExists = options.DefaultUploadHandlerOptions.FileSystemHandlerCreatePathIfNotExists;

        if (parameters is not null)
        {
            if (parameters.TryGetValue(_parameters[0], out var includedMimeTypeStr) && includedMimeTypeStr is not null)
            {
                includedMimeTypePatterns = includedMimeTypeStr.SplitParameter();
            }
            if (parameters.TryGetValue(_parameters[1], out var excludedMimeTypeStr) && excludedMimeTypeStr is not null)
            {
                excludedMimeTypePatterns = excludedMimeTypeStr.SplitParameter();
            }
            if (parameters.TryGetValue(_parameters[2], out var path) && !string.IsNullOrEmpty(path))
            {
                basePath = path;
            }
            if (parameters.TryGetValue(_parameters[3], out var newFileNameStr) && !string.IsNullOrEmpty(newFileNameStr))
            {
                newFileName = newFileNameStr;
            }
            if (parameters.TryGetValue(_parameters[4], out var useUniqueFileNameStr) 
                && bool.TryParse(useUniqueFileNameStr, out var useUniqueFileNameParsed))
            {
                useUniqueFileName = useUniqueFileNameParsed;
            }
            if (parameters.TryGetValue(_parameters[5], out var createPathIfNotExistsStr)
                && bool.TryParse(createPathIfNotExistsStr, out var createPathIfNotExistsParsed))
            {
                createPathIfNotExists = createPathIfNotExistsParsed;
            }
            if (parameters.TryGetValue(_parameters[6], out var bufferSizeStr) && int.TryParse(bufferSizeStr, out var bufferSizeParsed))
            {
                bufferSize = bufferSizeParsed;
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
            if (formFile.ContentType.CheckMimeTypes(includedMimeTypePatterns, excludedMimeTypePatterns) is false)
            {
                continue;
            }

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
            result.Append('}');
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