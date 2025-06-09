namespace NpgsqlRest.UploadHandlers;

public abstract class UploadHandler
{
    protected string? _type = null;

    protected bool TryGetParam(Dictionary<string, string> parameters, string key, out string value)
    {
        if (parameters.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }
        if (parameters.TryGetValue(string.Concat(_type, "_", key), out val))
        {
            value = val;
            return true;
        }
        value = default!;
        return false;
    }

    protected abstract IEnumerable<string> GetParameters();

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return (this as IUploadHandler)!;
    }

    public IEnumerable<string> Parameters
    {
        get
        {
            foreach (var param in GetParameters())
            {
                yield return param;
            }
            foreach (var param in GetParameters())
            {
                yield return string.Concat(_type, "_", param);
            }
        }
    }

    public const string SingleUploadParam = "single_upload";
    public const string IncludedMimeTypeParam = "included_mime_types";
    public const string ExcludedMimeTypeParam = "excluded_mime_types";
    public const string BufferSize = "buffer_size";

    public (string[]? includedMimeTypePatterns, string[]? excludedMimeTypePatterns,int bufferSize) ParseSharedParameters(
        NpgsqlRestUploadOptions options, 
        Dictionary<string, string>? parameters)
    {
        string[]? includedMimeTypePatterns = options.DefaultUploadHandlerOptions.LargeObjectIncludedMimeTypePatterns;
        string[]? excludedMimeTypePatterns = options.DefaultUploadHandlerOptions.LargeObjectExcludedMimeTypePatterns;
        var bufferSize = options.DefaultUploadHandlerOptions.LargeObjectHandlerBufferSize;

        if (parameters is not null)
        {
            if (TryGetParam(parameters, IncludedMimeTypeParam, out var includedMimeTypeStr) && includedMimeTypeStr is not null)
            {
                includedMimeTypePatterns = includedMimeTypeStr.SplitParameter();
            }
            if (TryGetParam(parameters, ExcludedMimeTypeParam, out var excludedMimeTypeStr) && excludedMimeTypeStr is not null)
            {
                excludedMimeTypePatterns = excludedMimeTypeStr.SplitParameter();
            }
            if (TryGetParam(parameters, BufferSize, out var bufferSizeStr) && int.TryParse(bufferSizeStr, out var bufferSizeParsed))
            {
                bufferSize = bufferSizeParsed;
            }
        }

        return (includedMimeTypePatterns, excludedMimeTypePatterns, bufferSize);
    }
}
