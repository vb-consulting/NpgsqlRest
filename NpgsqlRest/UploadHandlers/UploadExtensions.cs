namespace NpgsqlRest.UploadHandlers;

public enum UploadFileStatus { Empty, ProbablyBinary, NoNewLines, Ok }

public static class UploadExtensions
{
    public static Dictionary<string, Func<ILogger?, IUploadHandler>>? CreateUploadHandlers(this NpgsqlRestUploadOptions options)
    {
        if (options is null)
        {
            return null;
        }
        if (options.Enabled is false)
        {
            return null;
        }
        if (options.DefaultUploadHandlerOptions.LargeObjectEnabled is false && 
            options.DefaultUploadHandlerOptions.FileSystemEnabled is false && 
            options.DefaultUploadHandlerOptions.CsvUploadEnabled is false)
        {
            return null;
        }
        var result = new Dictionary<string, Func<ILogger?, IUploadHandler>>();
        if (options.DefaultUploadHandlerOptions.LargeObjectEnabled)
        {
            result.Add(options.DefaultUploadHandlerOptions.LargeObjectKey, logger => new LargeObjectUploadHandler(options, logger));
        }
        if (options.DefaultUploadHandlerOptions.FileSystemEnabled)
        {
            result.Add(options.DefaultUploadHandlerOptions.FileSystemKey, logger => new FileSystemUploadHandler(options, logger));
        }
        if (options.DefaultUploadHandlerOptions.CsvUploadEnabled)
        {
            result.Add(options.DefaultUploadHandlerOptions.CsvUploadKey, logger => new CsvUploadHandler(options, logger));
        }
        return result;
    }

    public static IUploadHandler? CreateUploadHandler(this NpgsqlRestOptions options, RoutineEndpoint endpoint, ILogger? logger)
    {
        if (endpoint.UploadHandlers is null || endpoint.UploadHandlers.Length == 0)
        {
            if (options.UploadOptions.UploadHandlers is not null && options.UploadOptions.UploadHandlers.TryGetValue(options.UploadOptions.DefaultUploadHandler, out var handler))
            {
                return handler(logger).SetType(options.UploadOptions.DefaultUploadHandler);
            }
            else
            {
                throw new Exception($"Default upload handler '{options.UploadOptions.DefaultUploadHandler}' not found.");
            }
        }
        else if (endpoint.UploadHandlers.Length == 1)
        { 
            var handlerName = endpoint.UploadHandlers[0];
            if (options.UploadOptions.UploadHandlers is not null && options.UploadOptions.UploadHandlers.TryGetValue(handlerName, out var handler))
            {
                return handler(logger).SetType(handlerName);
            }
            else
            {
                throw new Exception($"Upload handler '{handlerName}' not found.");
            }
        }
        else
        {
            // all handlers defined
            return new DefaultUploadHandler(options.UploadOptions.UploadHandlers?.Select(h => h.Value(logger).SetType(h.Key)).ToArray() ?? []);
        }
    }

    public static string[]? SplitParameter(this string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }
        return type.Split(',', ' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool CheckMimeTypes(this string contentType, string[]? includedMimeTypePatterns, string[]? excludedMimeTypePatterns)
    {
        // File must match AT LEAST ONE included pattern
        if (includedMimeTypePatterns is not null && includedMimeTypePatterns.Length > 0)
        {
            bool matchesAny = false;
            for (int j = 0; j < includedMimeTypePatterns.Length; j++)
            {
                if (Parser.IsPatternMatch(contentType, includedMimeTypePatterns[j]))
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
        if (excludedMimeTypePatterns is not null)
        {
            for (int j = 0; j < excludedMimeTypePatterns.Length; j++)
            {
                if (Parser.IsPatternMatch(contentType, excludedMimeTypePatterns[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static async Task<UploadFileStatus> CheckFileStatus(
        this IFormFile formFile,
        int testBufferSize = 4096,
        int nonPrintableThreshold = 5,
        bool checkNewLines = true)
    {
        int length = formFile.Length < testBufferSize ? (int)formFile.Length : testBufferSize;

        if (length == 0)
        {
            return UploadFileStatus.Empty;
        }

        using var fileStream = formFile.OpenReadStream();
        byte[] buffer = new byte[length];
        int bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, length));

        if (bytesRead == 0)
        {
            return UploadFileStatus.Empty;
        }

        int nonPrintableCount = 0;
        int newLineCount = 0;

        for (int i = 0; i < bytesRead; i++)
        {
            // Check for null byte - immediate binary indicator
            if (buffer[i] == 0)
            {
                return UploadFileStatus.ProbablyBinary;
            }

            // Count newlines (LF character) if we're checking for them
            if (checkNewLines && buffer[i] == 10) // ASCII LF (Line Feed)
            {
                newLineCount++;
            }

            // Count non-printable characters
            else if (buffer[i] < 32 && buffer[i] != 9 && buffer[i] != 13)
            {
                nonPrintableCount++;
            }
        }

        // Check if binary based on non-printable characters
        if (nonPrintableCount > nonPrintableThreshold)
        {
            return UploadFileStatus.ProbablyBinary;
        }

        // Only check for newlines if the parameter is true
        if (checkNewLines)
        {
            if (newLineCount > 0)
            {
                return UploadFileStatus.Ok;
            }
            return UploadFileStatus.NoNewLines;
        }
        else
        {
            // If we're not checking for newlines, consider it OK if it passes binary checks
            return UploadFileStatus.Ok;
        }
    }
}
