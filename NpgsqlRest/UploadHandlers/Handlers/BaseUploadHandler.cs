namespace NpgsqlRest.UploadHandlers.Handlers;

public abstract class BaseUploadHandler
{
    protected HashSet<string> _skipFileNames = new(StringComparer.OrdinalIgnoreCase);

    protected string? _type = default;
    protected bool CheckMimeTypes(string contentType)
    {
        // File must match AT LEAST ONE included pattern
        if (_includedMimeTypePatterns is not null && _includedMimeTypePatterns.Length > 0)
        {
            bool matchesAny = false;
            for (int j = 0; j < _includedMimeTypePatterns.Length; j++)
            {
                if (Parser.IsPatternMatch(contentType, _includedMimeTypePatterns[j]))
                {
                    matchesAny = true;
                    break;
                }
            }

            if (!matchesAny)
            {
                return false;
            }
        }

        // File must NOT match ANY excluded patterns
        if (_excludedMimeTypePatterns is not null)
        {
            for (int j = 0; j < _excludedMimeTypePatterns.Length; j++)
            {
                if (Parser.IsPatternMatch(contentType, _excludedMimeTypePatterns[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

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

    protected string[]? _includedMimeTypePatterns = default;
    protected string[]? _excludedMimeTypePatterns = default;
    protected int _bufferSize = default;
    protected bool _stopAfterFirstSuccess = default;

    public void ParseSharedParameters(NpgsqlRestUploadOptions options, Dictionary<string, string>? parameters)
    {
        _includedMimeTypePatterns = options.DefaultUploadHandlerOptions.IncludedMimeTypePatterns;
        _excludedMimeTypePatterns = options.DefaultUploadHandlerOptions.ExcludedMimeTypePatterns;
        _bufferSize = options.DefaultUploadHandlerOptions.BufferSize;
        _stopAfterFirstSuccess = options.DefaultUploadHandlerOptions.StopAfterFirstSuccess;

        if (parameters is not null)
        {
            if (TryGetParam(parameters, IncludedMimeTypeParam, out var includedMimeTypeStr) && includedMimeTypeStr is not null)
            {
                _includedMimeTypePatterns = includedMimeTypeStr.SplitParameter();
            }
            if (TryGetParam(parameters, ExcludedMimeTypeParam, out var excludedMimeTypeStr) && excludedMimeTypeStr is not null)
            {
                _excludedMimeTypePatterns = excludedMimeTypeStr.SplitParameter();
            }
            if (TryGetParam(parameters, BufferSizeParam, out var bufferSizeStr) && int.TryParse(bufferSizeStr, out var bufferSizeParsed))
            {
                _bufferSize = bufferSizeParsed;
            }
            if (TryGetParam(parameters, StopAfterFirstParam, out var stopAfterFirstSuccessStr) && bool.TryParse(stopAfterFirstSuccessStr, out var stopAfterFirstSuccessParsed))
            {
                _stopAfterFirstSuccess = stopAfterFirstSuccessParsed;
            }
        }
    }

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return (this as IUploadHandler)!;
    }

    public IEnumerable<string> Parameters
    {
        get
        {
            yield return StopAfterFirstParam;
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

    public const string StopAfterFirstParam = "stop_after_first_success";
    public const string IncludedMimeTypeParam = "included_mime_types";
    public const string ExcludedMimeTypeParam = "excluded_mime_types";
    public const string BufferSizeParam = "buffer_size";

    public bool StopAfterFirst => _stopAfterFirstSuccess;
    
    public void SetSkipFileNames(HashSet<string> skipFileNames)
    {
        _skipFileNames = skipFileNames;
    }

    public HashSet<string> GetSkipFileNames => _skipFileNames;
}
