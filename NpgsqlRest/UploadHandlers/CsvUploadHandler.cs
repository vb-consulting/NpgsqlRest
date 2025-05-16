using System.Text;
using Microsoft.VisualBasic.FileIO;
using Npgsql;
using NpgsqlTypes;
using static NpgsqlRest.PgConverters;

namespace NpgsqlRest.UploadHandlers;

public class CsvUploadHandler(NpgsqlRestUploadOptions options, ILogger? logger) : IUploadHandler
{
    public bool RequiresTransaction => true;
    public string[] Parameters => _parameters;

    private readonly string[] _parameters = [
        "included_mime_types",
        "excluded_mime_types",
        "check_file",
        "test_buffer_size",
        "non_printable_threshold",
        "delimiters",
        "has_fields_enclosed_in_quotes",
        "set_white_space_to_null",
        "row_command",
    ];

    private string? _type = null;

    public IUploadHandler SetType(string type)
    {
        _type = type;
        return this;
    }

    public async Task<object> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        string[]? includedMimeTypePatterns = options.DefaultUploadHandlerOptions.CsvUploadIncludedMimeTypePatterns;
        string[]? excludedMimeTypePatterns = options.DefaultUploadHandlerOptions.CsvUploadExcludedMimeTypePatterns;

        bool checkFileStatus = options.DefaultUploadHandlerOptions.CsvUploadCheckFileStatus;
        int testBufferSize = options.DefaultUploadHandlerOptions.CsvUploadTestBufferSize;
        int nonPrintableThreshold = options.DefaultUploadHandlerOptions.CsvUploadNonPrintableThreshold;
        string delimiters = options.DefaultUploadHandlerOptions.CsvUploadDelimiterChars;
        bool hasFieldsEnclosedInQuotes = options.DefaultUploadHandlerOptions.CsvUploadHasFieldsEnclosedInQuotes;
        bool setWhiteSpaceToNull = options.DefaultUploadHandlerOptions.CsvUploadSetWhiteSpaceToNull;
        string rowCommand = options.DefaultUploadHandlerOptions.CsvUploadRowCommand;

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
            if (parameters.TryGetValue(_parameters[2], out var checkFileStatusStr) && bool.TryParse(checkFileStatusStr, out var checkFileStatusParsed))
            {
                checkFileStatus = checkFileStatusParsed;
            }
            if (parameters.TryGetValue(_parameters[3], out var testBufferSizeStr) && int.TryParse(testBufferSizeStr, out var testBufferSizeParsed))
            {
                testBufferSize = testBufferSizeParsed;
            }
            if (parameters.TryGetValue(_parameters[4], out var nonPrintableThresholdStr) && int.TryParse(nonPrintableThresholdStr, out var nonPrintableThresholdParsed))
            {
                nonPrintableThreshold = nonPrintableThresholdParsed;
            }
            if (parameters.TryGetValue(_parameters[5], out var delimitersStr) && delimitersStr is not null)
            {
                delimiters = delimitersStr!;
            }
            if (parameters.TryGetValue(_parameters[6], out var hasFieldsEnclosedInQuotesStr) && bool.TryParse(hasFieldsEnclosedInQuotesStr, out var hasFieldsEnclosedInQuotesParsed))
            {
                hasFieldsEnclosedInQuotes = hasFieldsEnclosedInQuotesParsed;
            }
            if (parameters.TryGetValue(_parameters[7], out var setWhiteSpaceToNullStr) && bool.TryParse(setWhiteSpaceToNullStr, out var setWhiteSpaceToNullParsed))
            {
                setWhiteSpaceToNull = setWhiteSpaceToNullParsed;
            }
            if (parameters.TryGetValue(_parameters[8], out var rowCommandStr) && rowCommandStr is not null)
            {
                rowCommand = rowCommandStr;
            }
        }

        string[] delimitersArr = [.. delimiters.Select(c => c.ToString())];
        using var command = new NpgsqlCommand(rowCommand, connection);
        var paramCount = rowCommand.PgCountParams();
        if (paramCount >= 1) command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Integer));
        if (paramCount >= 2) command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Text | NpgsqlDbType.Array));
        if (paramCount >= 3) command.Parameters.Add(new NpgsqlParameter());
        if (paramCount >= 4) command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Json));

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

            StringBuilder fileJson = new(100);
            if (_type is not null)
            {
                fileJson.Append("{\"type\":");
                fileJson.Append(SerializeString(_type));
                fileJson.Append(",\"fileName\":");
            }
            else
            {
                fileJson.Append("{\"fileName\":");
            }
            fileJson.Append(SerializeString(formFile.FileName));
            fileJson.Append(",\"contentType\":");
            fileJson.Append(SerializeString(formFile.ContentType));
            fileJson.Append(",\"size\":");
            fileJson.Append(formFile.Length);

            if (checkFileStatus is true)
            {
                var fileStatus = await formFile.CheckFileStatus(testBufferSize, nonPrintableThreshold, checkNewLines: true);
                fileJson.Append(",\"status\":");
                fileJson.Append(SerializeString(fileStatus.ToString()));
                fileJson.Append('}');
                if (fileStatus != UploadFileStatus.Ok)
                {
                    logger?.NotValidCsvFile(formFile.FileName, formFile.ContentType, formFile.Length, fileStatus.ToString());
                    fileId++;
                    continue;
                }
            }
            else
            {
                fileJson.Append(",\"status\":");
                fileJson.Append(SerializeString(UploadFileStatus.Ok.ToString()));
                fileJson.Append('}');
            }

            using var fileStream = formFile.OpenReadStream();
            using var streamReader = new StreamReader(fileStream);

            int rowIndex = 1;
            object? commandResult = null;
            while (await streamReader.ReadLineAsync() is { } line)
            {
                using var parser = new TextFieldParser(new StringReader(line));
                parser.SetDelimiters(delimitersArr);
                parser.HasFieldsEnclosedInQuotes = hasFieldsEnclosedInQuotes;
                string?[]? values = setWhiteSpaceToNull ? 
                    parser.ReadFields()?.Select(field => string.IsNullOrWhiteSpace(field) ? null : field).ToArray() :
                    parser.ReadFields()?.ToArray();

                if (paramCount >= 1)
                {
                    command.Parameters[0].Value = rowIndex;
                }
                if (paramCount >= 2)
                {
                    command.Parameters[1].Value = values ?? (object)DBNull.Value;
                }
                if (paramCount >= 3)
                {
                    command.Parameters[2].Value = commandResult ?? DBNull.Value;
                }
                if (paramCount >= 4)
                {
                    command.Parameters[3].Value = fileJson.ToString();
                }
                commandResult = await command.ExecuteScalarAsync();

                rowIndex++;
            }
            fileJson[^1] = ',';
            fileJson.Append("\"lastResult\":");
            fileJson.Append(SerializeDatbaseObject(commandResult));
            fileJson.Append('}');

            if (options.LogUploadEvent)
            {
                logger?.UploadedCsvFile(formFile.FileName, formFile.ContentType, formFile.Length, rowCommand);
            }
            result.Append(fileJson);
            fileId++;
        }

        result.Append(']');
        return result.ToString();
    }

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
    }
}
