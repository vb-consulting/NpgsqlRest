using System.Text;
using Npgsql;
using NpgsqlTypes;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

public class LargeObjectUploadHandler(NpgsqlRestUploadOptions options, ILogger? logger) : IUploadHandler
{
    private const string OidParam = "oid";
    private string? _type = null;

    public bool RequiresTransaction => true;
    public string[] Parameters => [
        UploadExtensions.IncludedMimeTypeParam, 
        UploadExtensions.ExcludedMimeTypeParam, 
        UploadExtensions.BufferSize, 
        OidParam,
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
        long? oid = null;
        bool checkText = false;
        bool checkImage = false;
        int testBufferSize = options.DefaultUploadHandlerOptions.TextTestBufferSize;
        int nonPrintableThreshold = options.DefaultUploadHandlerOptions.TextNonPrintableThreshold;

        AllowedImageTypes allowedImage = options.DefaultUploadHandlerOptions.AllowedImageTypes;

        if (parameters is not null)
        {
            if (parameters.TryGetValue(OidParam, out var oidStr) && long.TryParse(oidStr, out var oidParsed))
            {
                oid = oidParsed;
            }
            if (parameters.TryGetValue(FileCheckExtensions.CheckTextParam, out var checkTextParamStr)
                && bool.TryParse(checkTextParamStr, out var checkTextParamParsed))
            {
                checkText = checkTextParamParsed;
            }
            if (parameters.TryGetValue(FileCheckExtensions.CheckImageParam, out var checkImageParamStr))
            {
                if (bool.TryParse(checkImageParamStr, out var checkImageParamParsed))
                {
                    checkImage = checkImageParamParsed;
                }
                else
                {
                    checkImage = true;
                    allowedImage = checkImageParamStr.ParseImageTypes(logger);
                }
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

        StringBuilder result = new(context.Request.Form.Files.Count*100);
        result.Append('[');
        int fileId = 0;
        for (int i = 0; i < context.Request.Form.Files.Count; i++)
        {
            IFormFile formFile = context.Request.Form.Files[i];
            if (fileId > 0)
            {
                result.Append(',');
            }

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
            if (status != UploadFileStatus.Ok)
            {
                logger?.FileUploadFailed(_type, formFile.FileName, formFile.ContentType, formFile.Length, status);
                result.Append(",\"oid\":null}");
                fileId++;
                continue;
            }

            result.Append(",\"oid\":");
            using var command = new NpgsqlCommand(oid is null ? "select lo_create(0)" : string.Concat("select lo_create(", oid.ToString(), ")"), connection);
            var resultOid = await command.ExecuteScalarAsync();
            
            result.Append(resultOid);
            result.Append('}');

            command.CommandText = "select lo_put($1,$2,$3)";
            command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Oid));
            command.Parameters[0].Value = resultOid;
            command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Bigint));
            command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Bytea));

            using var fileStream = formFile.OpenReadStream();
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            long offset = 0;
            while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
            {
                command.Parameters[1].Value = offset;
                command.Parameters[2].Value = buffer.Take(bytesRead).ToArray();
                await command.ExecuteNonQueryAsync();
                offset += bytesRead;
            }
            if (options.LogUploadEvent)
            {
                logger?.UploadedFileToLargeObject(formFile.FileName, formFile.ContentType, formFile.Length, resultOid);
            }
            fileId++;
        }

        result.Append(']');
        return result.ToString();
    }

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
    }
}
