using System.Text;
using Npgsql;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

public class FileSystemUploadHandler : IUploadHandler
{
    private readonly string _basePath;
    private readonly bool _useUniqueFileName;
    private readonly int _bufferSize;
    private string? _type = null;
    private string? _currentFilePath = null;

    public FileSystemUploadHandler(
        string path = "/tmp/uploads",
        bool useUniqueFileName = true,
        bool createPathIfNotExists = true,
        int bufferSize = 8192)
    {
        _basePath = path;
        _useUniqueFileName = useUniqueFileName;
        _bufferSize = bufferSize;

        // Ensure the directory exists
        if (createPathIfNotExists is true && Directory.Exists(_basePath) is false)
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public int BufferSize => _bufferSize;

    // This implementation doesn't require a transaction
    public bool RequiresTransaction => false;

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return this;
    }

    public async Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context)
    {
        StringBuilder result = new(context.Request.Form.Files.Count * 100);
        result.Append('[');

        for (int i = 0; i < context.Request.Form.Files.Count; i++)
        {
            IFormFile formFile = context.Request.Form.Files[i];

            if (i > 0)
            {
                result.Append(',');
            }

            // Generate file path (with unique name if specified)
            string fileName = formFile.FileName;
            if (_useUniqueFileName)
            {
                string extension = Path.GetExtension(fileName);
                fileName = $"{Guid.NewGuid()}{extension}";
            }

            _currentFilePath = Path.Combine(_basePath, fileName);

            // Save the file
            using (var fileStream = new FileStream(_currentFilePath, FileMode.Create))
            {
                byte[] buffer = new byte[BufferSize];
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