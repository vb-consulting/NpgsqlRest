using System.IO;
using System.Text;
using Npgsql;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

/// <summary>
/// Custom parameters:
/// - path: string - path to save the file
/// - file: string - name of the file
/// - unique_name: bool - whether to use a unique file name
/// - create_path: bool - whether to create the path if it does not exist
/// - buffer_size: int - size of the buffer to use when saving the file
/// </summary>
public class FileSystemUploadHandler(
    string path = "./",
    bool useUniqueFileName = true,
    bool createPathIfNotExists = true,
    int bufferSize = 8192) : IUploadHandler
{
    private readonly string _basePath = path;
    private readonly bool _useUniqueFileName = useUniqueFileName;
    private readonly bool _createPathIfNotExists = createPathIfNotExists;
    private readonly int _bufferSize = bufferSize;
    private string? _type = null;
    private string? _currentFilePath = null;
    private readonly string[] _parameters = [
        "path",
        "file",
        "unique_name",
        "create_path",
        "buffer_size"
    ];

    public bool RequiresTransaction => false;
    public string[] Parameters => _parameters;

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return this;
    }

    public async Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        var basePath = _basePath;
        var useUniqueFileName = _useUniqueFileName;
        var bufferSize = _bufferSize;
        string? newFileName = null;
        bool createPathIfNotExists = _createPathIfNotExists;

        if (parameters is not null)
        {
            if (parameters.TryGetValue(_parameters[0], out var path) && !string.IsNullOrEmpty(path))
            {
                basePath = path;
            }
            if (parameters.TryGetValue(_parameters[1], out var newFileNameStr) && !string.IsNullOrEmpty(newFileNameStr))
            {
                newFileName = newFileNameStr;
            }
            if (parameters.TryGetValue(_parameters[2], out var useUniqueFileNameStr) 
                && bool.TryParse(useUniqueFileNameStr, out var useUniqueFileNameParsed))
            {
                useUniqueFileName = useUniqueFileNameParsed;
            }
            if (parameters.TryGetValue(_parameters[3], out var createPathIfNotExistsStr)
                && bool.TryParse(createPathIfNotExistsStr, out var createPathIfNotExistsParsed))
            {
                createPathIfNotExists = createPathIfNotExistsParsed;
            }
            if (parameters.TryGetValue(_parameters[4], out var bufferSizeStr) && int.TryParse(bufferSizeStr, out var bufferSizeParsed))
            {
                bufferSize = bufferSizeParsed;
            }
        }

        if (createPathIfNotExists is true && Directory.Exists(basePath) is false)
        {
            Directory.CreateDirectory(basePath);
        }

        StringBuilder result = new(context.Request.Form.Files.Count * 100);
        result.Append('[');

        for (int i = 0; i < context.Request.Form.Files.Count; i++)
        {
            IFormFile formFile = context.Request.Form.Files[i];

            if (i > 0)
            {
                result.Append(',');
            }

            string fileName = newFileName ?? formFile.FileName;
            if (useUniqueFileName)
            {
                string extension = Path.GetExtension(fileName);
                fileName = $"{Guid.NewGuid()}{extension}";
            }

            _currentFilePath = Path.Combine(basePath, fileName);
            using (var fileStream = new FileStream(_currentFilePath, FileMode.Create))
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                using var sourceStream = formFile.OpenReadStream();

                while ((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }

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
            result.Append(SerializeString(_currentFilePath));
            result.Append('}');
        }

        result.Append(']');
        return result.ToString();
    }

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
        // If there was an error and we have a current file path, delete the file
        if (_currentFilePath != null && File.Exists(_currentFilePath))
        {
            try
            {
                File.Delete(_currentFilePath);
            }
            catch
            {
                // Ignore any exceptions during cleanup
            }
        }
    }
}