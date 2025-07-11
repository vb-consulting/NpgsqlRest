﻿using System.Text;
using Npgsql;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers.Handlers;

public class FileSystemUploadHandler(NpgsqlRestUploadOptions options, ILogger? logger) : BaseUploadHandler, IUploadHandler
{
    private string[]? _uploadedFiles = null;

    private const string PathParam = "path";
    private const string FileParam = "file";
    private const string UniqueNameParam = "unique_name";
    private const string CreatePathParam = "create_path";
    protected override IEnumerable<string> GetParameters()
    {
        yield return IncludedMimeTypeParam;
        yield return ExcludedMimeTypeParam;
        yield return BufferSizeParam;
        yield return PathParam;
        yield return FileParam;
        yield return UniqueNameParam;
        yield return CreatePathParam;
        yield return FileCheckExtensions.CheckTextParam;
        yield return FileCheckExtensions.CheckImageParam;
        yield return FileCheckExtensions.TestBufferSizeParam;
        yield return FileCheckExtensions.NonPrintableThresholdParam;
    }

    public bool RequiresTransaction => false;

    public async Task<string> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        var basePath = options.DefaultUploadHandlerOptions.FileSystemPath;
        var useUniqueFileName = options.DefaultUploadHandlerOptions.FileSystemUseUniqueFileName;
        string? newFileName = null;
        bool createPathIfNotExists = options.DefaultUploadHandlerOptions.FileSystemCreatePathIfNotExists;
        bool checkText = options.DefaultUploadHandlerOptions.FileSystemCheckText;
        bool checkImage = options.DefaultUploadHandlerOptions.FileSystemCheckImage;
        int testBufferSize = options.DefaultUploadHandlerOptions.TextTestBufferSize;
        int nonPrintableThreshold = options.DefaultUploadHandlerOptions.TextNonPrintableThreshold;

        AllowedImageTypes allowedImage = options.DefaultUploadHandlerOptions.AllowedImageTypes;

        if (parameters is not null)
        {
            if (TryGetParam(parameters, PathParam, out var path) && !string.IsNullOrEmpty(path))
            {
                basePath = path;
            }
            if (TryGetParam(parameters, FileParam, out var newFileNameStr) && !string.IsNullOrEmpty(newFileNameStr))
            {
                newFileName = newFileNameStr;
            }
            if (TryGetParam(parameters, UniqueNameParam, out var useUniqueFileNameStr) 
                && bool.TryParse(useUniqueFileNameStr, out var useUniqueFileNameParsed))
            {
                useUniqueFileName = useUniqueFileNameParsed;
            }
            if (TryGetParam(parameters, CreatePathParam, out var createPathIfNotExistsStr)
                && bool.TryParse(createPathIfNotExistsStr, out var createPathIfNotExistsParsed))
            {
                createPathIfNotExists = createPathIfNotExistsParsed;
            }
            if (TryGetParam(parameters, FileCheckExtensions.CheckTextParam, out var checkTextParamStr)
                && bool.TryParse(checkTextParamStr, out var checkTextParamParsed))
            {
                checkText = checkTextParamParsed;
            }
            if (TryGetParam(parameters, FileCheckExtensions.CheckImageParam, out var checkImageParamStr))
            {
                if (bool.TryParse(checkImageParamStr, out var checkImageParamParsed))
                {
                    checkImage = checkImageParamParsed;
                }
                else
                {
                    checkImage = true;
                    allowedImage = checkImageParamStr.ParseImageTypes(logger) ?? options.DefaultUploadHandlerOptions.AllowedImageTypes;
                }
            }
            if (TryGetParam(parameters, FileCheckExtensions.TestBufferSizeParam, out var testBufferSizeStr) && int.TryParse(testBufferSizeStr, out var testBufferSizeParsed))
            {
                testBufferSize = testBufferSizeParsed;
            }
            if (TryGetParam(parameters, FileCheckExtensions.NonPrintableThresholdParam, out var nonPrintableThresholdStr) && int.TryParse(nonPrintableThresholdStr, out var nonPrintableThresholdParsed))
            {
                nonPrintableThreshold = nonPrintableThresholdParsed;
            }
        }

        if (options.LogUploadParameters is true)
        {
            logger?.LogInformation("Upload for {_type}: includedMimeTypePatterns={includedMimeTypePatterns}, excludedMimeTypePatterns={excludedMimeTypePatterns}, bufferSize={bufferSize}, basePath={basePath}, useUniqueFileName={useUniqueFileName}, newFileName={newFileName}, createPathIfNotExists={createPathIfNotExists}, checkText={checkText}, checkImage={checkImage}, allowedImage={allowedImage}, testBufferSize={testBufferSize}, nonPrintableThreshold={nonPrintableThreshold}",
                _type, _includedMimeTypePatterns, _excludedMimeTypePatterns, _bufferSize, basePath, useUniqueFileName, newFileName, createPathIfNotExists, checkText, checkImage, allowedImage, testBufferSize, nonPrintableThreshold);
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
            if (_stopAfterFirstSuccess is true && _skipFileNames.Contains(formFile.FileName, StringComparer.OrdinalIgnoreCase))
            {
                status = UploadFileStatus.Ignored;
            }
            if (status == UploadFileStatus.Ok && this.CheckMimeTypes(formFile.ContentType) is false)
            {
                status = UploadFileStatus.InvalidMimeType;
            }
            if (status == UploadFileStatus.Ok && (checkText is true || checkImage is true))
            {
                if (checkText is true)
                {
                    status = await formFile.CheckTextContentStatus(testBufferSize, nonPrintableThreshold, checkNewLines: false);
                }
                if (status == UploadFileStatus.Ok && checkImage is true)
                {
                    if (await formFile.IsImage(allowedImage) is false)
                    {
                        status = UploadFileStatus.InvalidImage;
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
            if (_stopAfterFirstSuccess is true)
            {
                _skipFileNames.Add(formFile.FileName);
            }

            using (var fileStream = new FileStream(currentFilePath, FileMode.Create))
            {
                byte[] buffer = new byte[_bufferSize];
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