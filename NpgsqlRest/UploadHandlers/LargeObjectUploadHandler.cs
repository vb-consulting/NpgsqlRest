﻿using System.Text;
using Npgsql;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

/// <summary>
/// Custom parameters:
/// - oid: OID of the large object to upload to. If not specified, it will be chosen by database.
/// - buffer_size: int - size of the buffer to use when saving the file
/// </summary>
public class LargeObjectUploadHandler(int bufferSize = 8192) : IUploadHandler
{
    public bool RequiresTransaction => true;
    public string[] Parameters => _parameters;

    private readonly string[] _parameters = [
        "oid",
        "buffer_size"
    ];
    private int _bufferSize => bufferSize;
    private object? _oid = null;
    private string? _type = null;

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return this;
    }

    public async Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        var bufferSize = _bufferSize;

        long? oid = null;
        if (parameters is not null)
        {
            if (parameters.TryGetValue(_parameters[0], out var oidStr) && long.TryParse(oidStr, out var oidParsed))
            {
                oid = oidParsed;
            }
            if (parameters.TryGetValue(_parameters[1], out var bufferSizeStr) && int.TryParse(bufferSizeStr, out var bufferSizeParsed))
            {
                bufferSize = bufferSizeParsed;
            }
        }

        StringBuilder result = new(context.Request.Form.Files.Count*100);
        result.Append('[');
        for (int i = 0; i < context.Request.Form.Files.Count; i++)
        {
            IFormFile formFile = context.Request.Form.Files[i];

            if (i > 0)
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
            _oid = await command.ExecuteScalarAsync();
            
            result.Append(_oid);
            result.Append('}');

            command.CommandText = "select lo_put($1,$2,$3)";
            command.Parameters.Add(new NpgsqlParameter() 
            { 
                NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Oid,
                Value = _oid
            });
            command.Parameters.Add(new NpgsqlParameter() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea });

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
        }

        result.Append(']');
        return result.ToString();
    }

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
        if (exception is not null || connection is null || _oid is null)
        {
            return;
        }

        using var command = new NpgsqlCommand("select lo_unlink($1)", connection);
        command.Parameters.Add(new NpgsqlParameter()
        {
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Oid,
            Value = _oid
        });
        command.ExecuteNonQuery();
    }
}
