namespace NpgsqlRest.UploadHandlers;

public class UploadHandlerOptions
{
    public bool UploadsEnabled { get; set; } = true;
    public bool LargeObjectEnabled { get; set; } = true;
    public string LargeObjectKey { get; set; } = "large_object";
    public int LargeObjectHandlerBufferSize { get; set; } = 8192;

    public bool FileSystemEnabled { get; set; } = true;
    public string FileSystemKey { get; set; } = "file_system";
    public string FileSystemHandlerPath { get; set; } = "./";
    public bool FileSystemHandlerUseUniqueFileName { get; set; } = true;
    public bool FileSystemHandlerCreatePathIfNotExists { get; set; } = false;
    public int FileSystemHandlerBufferSize { get; set; } = 8192;

    public static Dictionary<string, Func<IUploadHandler>>? CreateUploadHandlers(UploadHandlerOptions options)
    {
        if (options is null)
        {
            return null;
        }
        if (options.UploadsEnabled is false)
        {
            return null;
        }
        if (options.LargeObjectEnabled is false && options.FileSystemEnabled is false)
        {
            return null;
        }
        var result = new Dictionary<string, Func<IUploadHandler>>();
        if (options.LargeObjectEnabled)
        {
            result.Add(options.LargeObjectKey, () => new LargeObjectUploadHandler(options.LargeObjectHandlerBufferSize));
        }
        if (options.FileSystemEnabled)
        {
            result.Add(options.FileSystemKey, () => new FileSystemUploadHandler(
                options.FileSystemHandlerPath,
                options.FileSystemHandlerUseUniqueFileName,
                options.FileSystemHandlerCreatePathIfNotExists,
                options.FileSystemHandlerBufferSize));
        }
        return result;
    }
}
