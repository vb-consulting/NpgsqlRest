using System.Collections.Frozen;
using System.Data;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExcelDataReader;
using Npgsql;
using NpgsqlRest;
using NpgsqlRest.UploadHandlers;
using NpgsqlRest.UploadHandlers.Handlers;
using NpgsqlTypes;

namespace NpgsqlRestClient;

public class ExcelUploadOptions
{
    private static readonly ExcelUploadOptions _instance = new();
    public static ExcelUploadOptions Instance => _instance;

    public bool ExcelCheckFileStatus { get; set; } = true;
    public string? ExcelSheetName { get; set; } = null;
    public bool ExcelAllSheets { get; set; } = false;
    public string ExcelTimeFormat { get; set; } = "HH:mm:ss";
    public string ExcelDateFormat { get; set; } = "yyyy-MM-dd";
    public string ExcelDateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public bool ExcelRowDataAsJson { get; set; } = false;
    public string ExcelUploadRowCommand { get; set; } = "call process_excel_row($1,$2,$3,$4)";
}

public class ExcelUploadHandler(NpgsqlRestUploadOptions options, ILogger? logger) : BaseUploadHandler, IUploadHandler
{
    private const string CheckFileParam = "check_excel";
    private const string SheetNameParam = "sheet_name";
    private const string AllSheetsParam = "all_sheets";
    private const string TimeFormatParam = "time_format";
    private const string DateFormatParam = "date_format";
    private const string DateTimeFormatParam = "datetime_format";
    private const string JsonRowDataParam = "json_row_data";
    private const string RowCommandParam = "row_command";

    protected override IEnumerable<string> GetParameters()
    {
        yield return IncludedMimeTypeParam;
        yield return ExcludedMimeTypeParam;
        yield return CheckFileParam;
        yield return SheetNameParam;
        yield return AllSheetsParam;
        yield return TimeFormatParam;
        yield return DateFormatParam;
        yield return DateTimeFormatParam;
        yield return JsonRowDataParam;
        yield return RowCommandParam;
    }

    public bool RequiresTransaction => true;

    public async Task<string> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        // Initialize ExcelDataReader encoding provider
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        bool checkFileStatus = ExcelUploadOptions.Instance.ExcelCheckFileStatus;
        string? targetSheetName = ExcelUploadOptions.Instance.ExcelSheetName;
        bool allSheets = ExcelUploadOptions.Instance.ExcelAllSheets;
        string timeFormat = ExcelUploadOptions.Instance.ExcelTimeFormat;
        string dateFormat = ExcelUploadOptions.Instance.ExcelDateFormat;
        string dateTimeFormat = ExcelUploadOptions.Instance.ExcelDateTimeFormat;
        bool dataAsJson = ExcelUploadOptions.Instance.ExcelRowDataAsJson;
        string rowCommand = ExcelUploadOptions.Instance.ExcelUploadRowCommand;

        if (parameters is not null)
        {
            if (TryGetParam(parameters, CheckFileParam, out var checkFileStatusStr) && bool.TryParse(checkFileStatusStr, out var checkFileStatusParsed))
            {
                checkFileStatus = checkFileStatusParsed;
            }
            if (TryGetParam(parameters, SheetNameParam, out var sheetNameStr))
            {
                targetSheetName = sheetNameStr;
            }
            if (TryGetParam(parameters, AllSheetsParam, out var allSheetsStr) && bool.TryParse(allSheetsStr, out var allSheetsParsed))
            {
                allSheets = allSheetsParsed;
            }
            if (TryGetParam(parameters, TimeFormatParam, out var timeFormatStr))
            {
                timeFormat = timeFormatStr;
            }
            if (TryGetParam(parameters, DateFormatParam, out var dateFormatStr))
            {
                dateFormat = dateFormatStr;
            }
            if (TryGetParam(parameters, DateTimeFormatParam, out var dateTimeFormatStr))
            {
                dateTimeFormat = dateTimeFormatStr;
            }
            if (TryGetParam(parameters, JsonRowDataParam, out var dataAsJsonStr) && bool.TryParse(dataAsJsonStr, out var dataAsJsonParsed))
            {
                dataAsJson = dataAsJsonParsed;
            }
            if (TryGetParam(parameters, RowCommandParam, out var rowCommandStr))
            {
                rowCommand = rowCommandStr;
            }
        }

        ParseSharedParameters(options, parameters);

        if (options.LogUploadParameters is true)
        {
            logger?.LogInformation("Upload for Excel: includedMimeTypePatterns={includedMimeTypePatterns}, excludedMimeTypePatterns={excludedMimeTypePatterns}, checkFileStatus={checkFileStatus}, targetSheetName={targetSheetName}, allSheets={allSheets}, timeFormat={timeFormat}, dateFormat={dateFormat}, dateTimeFormat={dateTimeFormat}, rowCommand={rowCommand}",
                _includedMimeTypePatterns, _excludedMimeTypePatterns, checkFileStatus, targetSheetName, allSheets, timeFormat, dateFormat, dateTimeFormat, rowCommand);
        }

        using var command = new NpgsqlCommand(rowCommand, connection);
        var paramCount = rowCommand.PgCountParams();
        if (paramCount >= 1) command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Integer));
        if (paramCount >= 2)
        {
            if (dataAsJson is true)
            {
                command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Json));
            }
            else
            {
                command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Text | NpgsqlDbType.Array));
            }
        }
        if (paramCount >= 3) command.Parameters.Add(new NpgsqlParameter());
        if (paramCount >= 4) command.Parameters.Add(NpgsqlRestParameter.CreateParamWithType(NpgsqlDbType.Json));

        StringBuilder result = new(context.Request.Form.Files.Count * 100);
        result.Append('[');
        int fileId = 0;

        for (int i = 0; i < context.Request.Form.Files.Count; i++)
        {
            IFormFile formFile = context.Request.Form.Files[i];

            StringBuilder fileJson = new(100);
            if (_type is not null)
            {
                fileJson.Append("{\"type\":");
                fileJson.Append(PgConverters.SerializeString(_type));
                fileJson.Append(",\"fileName\":");
            }
            else
            {
                fileJson.Append("{\"fileName\":");
            }
            fileJson.Append(PgConverters.SerializeString(formFile.FileName));
            fileJson.Append(",\"contentType\":");
            fileJson.Append(PgConverters.SerializeString(formFile.ContentType));
            fileJson.Append(",\"size\":");
            fileJson.Append(formFile.Length);

            UploadFileStatus status = UploadFileStatus.Ok;
            if (_stopAfterFirstSuccess is true && _skipFileNames.Contains(formFile.FileName, StringComparer.OrdinalIgnoreCase))
            {
                status = UploadFileStatus.Ignored;
            }
            if (status == UploadFileStatus.Ok && this.CheckMimeTypes(formFile.ContentType) is false)
            {
                status = UploadFileStatus.InvalidMimeType;
            }
            if (status == UploadFileStatus.Ok && checkFileStatus is true)
            {
                if (IsValidExcelFile(formFile) is false)
                {
                    status = UploadFileStatus.InvalidFormat;
                }
            }

            if (status != UploadFileStatus.Ok)
            {
                fileJson.Append(",\"success\":false,\"status\":");
                fileJson.Append(PgConverters.SerializeString(status.ToString()));
                fileJson.Append('}');
                logger?.FileUploadFailed(_type, formFile.FileName, formFile.ContentType, formFile.Length, status);
                result.Append(fileJson);
                if (i < context.Request.Form.Files.Count - 1) result.Append(',');
                continue;
            }
            if (_stopAfterFirstSuccess is true)
            {
                _skipFileNames.Add(formFile.FileName);
            }

            try
            {
                using var stream = formFile.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = false
                    }
                });

                var sheetsToProcess = GetSheetsToProcess(dataSet, targetSheetName, allSheets);

                foreach (var (table, sheetName) in sheetsToProcess)
                {
                    if (fileId > 0) result.Append(',');

                    var rowMeta = string.Concat(fileJson.ToString(),
                        ",\"sheet\":",
                        PgConverters.SerializeDatbaseObject(sheetName));

                    int rowIndex = 1;
                    object? commandResult = null;

                    foreach (DataRow row in table.Rows)
                    {
                        object? values = null;

                        if (dataAsJson is true)
                        {
                            values = SerializeRowAsJson(row);
                        }
                        else
                        {
                            values = SerializeRowAsArray(row, timeFormat, dateFormat, dateTimeFormat);
                        }

                        if (paramCount >= 1) command.Parameters[0].Value = rowIndex;
                        if (paramCount >= 2) command.Parameters[1].Value = values;
                        if (paramCount >= 3) command.Parameters[2].Value = sheetName;
                        if (paramCount >= 4) command.Parameters[3].Value = rowMeta;

                        commandResult = await command.ExecuteScalarAsync();
                        rowIndex++;
                    }

                    var finalJson = string.Concat(rowMeta,
                        ",\"success\":true,\"rows\":",
                        rowIndex - 1,
                        ",\"result\":",
                        PgConverters.SerializeDatbaseObject(commandResult),
                        '}');

                    result.Append(finalJson);
                    fileId++;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error processing Excel file {fileName}", formFile.FileName);
                fileJson.Append(",\"success\":false,\"status\":\"Error\",\"error\":");
                fileJson.Append(PgConverters.SerializeString(ex.Message));
                fileJson.Append('}');
                result.Append(fileJson);
                if (i < context.Request.Form.Files.Count - 1) result.Append(',');
            }
        }

        result.Append(']');
        return result.ToString();
    }

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
    }

    private static IEnumerable<(DataTable table, string name)> GetSheetsToProcess(DataSet dataSet, string? targetSheetName, bool allSheets)
    {
        if (allSheets)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                yield return (table, table.TableName);
            }
        }
        else
        {
            if (string.IsNullOrEmpty(targetSheetName))
            {
                if (dataSet.Tables.Count > 0)
                {
                    yield return (dataSet.Tables[0], dataSet.Tables[0].TableName);
                }
            }
            else
            {
                var table = dataSet.Tables[targetSheetName];
                if (table != null)
                {
                    yield return (table, targetSheetName);
                }
            }
        }
    }

    private static bool IsValidExcelFile(IFormFile formFile)
    {
        try
        {
            using var stream = formFile.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);
            return reader != null;
        }
        catch
        {
            return false;
        }
    }

    private static string SerializeRowAsJson(DataRow row)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        
        for (int i = 0; i < row.Table.Columns.Count; i++)
        {
            if (i > 0) sb.Append(',');
            
            var columnName = GetColumnName(i);
            var value = row[i];
            
            sb.Append('"');
            sb.Append(columnName);
            sb.Append("\":");
            
            if (value == DBNull.Value)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append(PgConverters.SerializeString(value.ToString() ?? ""));
            }
        }
        
        sb.Append('}');
        return sb.ToString();
    }

    private static string[] SerializeRowAsArray(DataRow row, string timeFormat, string dateFormat, string dateTimeFormat)
    {
        var values = new string[row.Table.Columns.Count];
        
        for (int i = 0; i < row.ItemArray.Length; i++)
        {
            var value = row[i];
            if (value == DBNull.Value || value == null)
            {
                values[i] = string.Empty;
            }
            else if (value is DateTime dateTime)
            {
                if (dateTime.TimeOfDay == TimeSpan.Zero)
                {
                    values[i] = dateTime.ToString(dateFormat);
                }
                else if (dateTime.Date == DateTime.MinValue.Date)
                {
                    values[i] = dateTime.ToString(timeFormat);
                }
                else
                {
                    values[i] = dateTime.ToString(dateTimeFormat);
                }
            }
            else
            {
                values[i] = value.ToString() ?? string.Empty;
            }
        }
        
        return values;
    }

    private static string GetColumnName(int columnIndex)
    {
        string columnName = "";
        while (columnIndex >= 0)
        {
            columnName = (char)('A' + columnIndex % 26) + columnName;
            columnIndex = columnIndex / 26 - 1;
        }
        return columnName;
    }
}