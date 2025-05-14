using System.Net.Mime;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

public class LargeObjectUploadHandler(UploadHandlerOptions options, ILogger? logger) : IUploadHandler
{
    private readonly string[] _parameters = [
        "included_mime_types",
        "excluded_mime_types",
        "oid",
        "buffer_size"
    ];
    private string? _type = null;

    public bool RequiresTransaction => true;
    public string[] Parameters => _parameters;

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return this;
    }

    public async Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        string[]? includedMimeTypePatterns = options.LargeObjectIncludedMimeTypePatterns;
        string[]? excludedMimeTypePatterns = options.LargeObjectExcludedMimeTypePatterns;
        var bufferSize = options.LargeObjectHandlerBufferSize;
        long? oid = null;

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
            if (parameters.TryGetValue(_parameters[2], out var oidStr) && long.TryParse(oidStr, out var oidParsed))
            {
                oid = oidParsed;
            }
            if (parameters.TryGetValue(_parameters[3], out var bufferSizeStr) && int.TryParse(bufferSizeStr, out var bufferSizeParsed))
            {
                bufferSize = bufferSizeParsed;
            }
        }

        StringBuilder result = new(context.Request.Form.Files.Count*100);
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
