using System.Collections.Frozen;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Npgsql;
using NpgsqlRest;
using NpgsqlRest.UploadHandlers;
using NpgsqlTypes;

namespace NpgsqlRestClient;

public class ExcelUploadOptions
{
    public bool ExcelUploadCheckFileStatus { get; set; } = true;
    public string? ExcelSheetName { get; set; } = null;
    public bool ExcelAllSheets { get; set; } = false;
    public string ExcelTimeFormat { get; set; } = "HH:mm:ss";
    public string ExcelDateFormat { get; set; } = "yyyy-MM-dd";
    public string ExcelDateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public bool ExcelRowDataAsJson { get; set; } = false;
    public string ExcelUploadRowCommand { get; set; } = "call process_excel_row($1,$2,$3,$4)";

    public static ExcelUploadOptions Instance = new();
}

public class ExcelUploadHandler(NpgsqlRestUploadOptions options, ILogger? logger) : UploadHandler, IUploadHandler
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

    private string _timeFormat = ExcelUploadOptions.Instance.ExcelTimeFormat;
    private string _dateFormat = ExcelUploadOptions.Instance.ExcelDateFormat;
    private string _dateTimeFormat = ExcelUploadOptions.Instance.ExcelDateTimeFormat;

    public async Task<string> UploadAsync(NpgsqlConnection connection, HttpContext context, Dictionary<string, string>? parameters)
    {
        var (includedMimeTypePatterns, excludedMimeTypePatterns, _) = ParseSharedParameters(options, parameters);
        bool checkFileStatus = ExcelUploadOptions.Instance.ExcelUploadCheckFileStatus;
        string rowCommand = ExcelUploadOptions.Instance.ExcelUploadRowCommand;
        string? targetSheetName = ExcelUploadOptions.Instance.ExcelSheetName;
        bool allSheets = ExcelUploadOptions.Instance.ExcelAllSheets;
        bool dataAsJson = ExcelUploadOptions.Instance.ExcelRowDataAsJson;

        if (parameters is not null)
        {
            if (TryGetParam(parameters, CheckFileParam, out var checkFileStatusStr) && bool.TryParse(checkFileStatusStr, out var checkFileStatusParsed))
            {
                checkFileStatus = checkFileStatusParsed;
            }
            if (TryGetParam(parameters, SheetNameParam, out var sheetNameStr) && sheetNameStr is not null)
            {
                targetSheetName = sheetNameStr;
            }
            if (TryGetParam(parameters, AllSheetsParam, out var allSheetsStr) && bool.TryParse(allSheetsStr, out var allSheetsParsed))
            {
                allSheets = allSheetsParsed;
            }
            if (TryGetParam(parameters, TimeFormatParam, out var timeFormatStr) && timeFormatStr is not null)
            {
                _timeFormat = timeFormatStr;
            }
            if (TryGetParam(parameters, DateFormatParam, out var dateFormatStr) && dateFormatStr is not null)
            {
                _dateFormat = dateFormatStr;
            }
            if (TryGetParam(parameters, DateTimeFormatParam, out var dateTimeFormatStr) && dateTimeFormatStr is not null)
            {
                _dateTimeFormat = dateTimeFormatStr;
            }
            if (TryGetParam(parameters, JsonRowDataParam, out var jsonRowDataStr) && bool.TryParse(jsonRowDataStr, out var jsonRowDataParsed))
            {
                dataAsJson = jsonRowDataParsed;
            }
            if (TryGetParam(parameters, RowCommandParam, out var rowCommandStr) && rowCommandStr is not null)
            {
                rowCommand = rowCommandStr;
            }
        }

        if (options.LogUploadParameters is true)
        {
#pragma warning disable CA2253 // Named placeholders should not be numeric values
            logger?.LogInformation("Upload for {0}: includedMimeTypePatterns={1}, excludedMimeTypePatterns={2}, checkFileStatus={3}, targetSheetName={4}, allSheets={5}, timeFormat={6}, dateFormat={7}, dateTimeFormat={8}, rowCommand={9}",
                _type, includedMimeTypePatterns, excludedMimeTypePatterns, checkFileStatus, targetSheetName, allSheets, _timeFormat, _dateFormat, _dateTimeFormat, rowCommand);
#pragma warning restore CA2253 // Named placeholders should not be numeric values
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
            using var stream = formFile.OpenReadStream();
            using var document = SpreadsheetDocument.Open(stream, false);

            var stringTableList =
                (document.WorkbookPart?.SharedStringTablePart?.SharedStringTable ?? throw new InvalidOperationException("No shared string table found in the worksheet."))
                .Select(item => item.InnerText)
                .ToArray()
                .AsMemory();
            FrozenSet<uint> dateFormatStyleIndices = BuildDateFormatCache(document.WorkbookPart?.WorkbookStylesPart?.Stylesheet);

            foreach (var (worksheet, sheetName) in EnumerateWorksheets(document.WorkbookPart, targetSheetName, allSheets))
            {
                if (fileId > 0)
                {
                    result.Append(',');
                }

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
                fileJson.Append(",\"sheet\":");
                fileJson.Append(PgConverters.SerializeDatbaseObject(sheetName));
                fileJson.Append(",\"contentType\":");
                fileJson.Append(PgConverters.SerializeString(formFile.ContentType));
                fileJson.Append(",\"size\":");
                fileJson.Append(formFile.Length);

                UploadFileStatus status = UploadFileStatus.Ok;
                if (formFile.ContentType.CheckMimeTypes(includedMimeTypePatterns, excludedMimeTypePatterns) is false)
                {
                    status = UploadFileStatus.InvalidMimeType;
                }
                if (status == UploadFileStatus.Ok && checkFileStatus is true)
                {
                    if (IsValidExcelFile(formFile) is false)
                    {
                        status = UploadFileStatus.InvalidFileFormat;
                    }
                }
                fileJson.Append(",\"success\":");
                fileJson.Append(status == UploadFileStatus.Ok ? "true" : "false");
                fileJson.Append(",\"status\":");
                fileJson.Append(PgConverters.SerializeString(status.ToString()));
                fileJson.Append('}');
                if (status != UploadFileStatus.Ok)
                {
                    logger?.FileUploadFailed(_type, formFile.FileName, formFile.ContentType, formFile.Length, status);
                    result.Append(fileJson);
                    fileId++;
                    continue;
                }

                SheetData sheetData = (worksheet?.Worksheet.GetFirstChild<SheetData>()) ??
                    throw new InvalidOperationException("No sheet data found in the worksheet.");

                int rowIndex = 1;
                object? commandResult = null;
                int maxColumnIndexFirstRow = 0;
                foreach (var row in sheetData.Elements<Row>())
                {
                    var span = stringTableList.Span;
                    object? values = null;

                    if (dataAsJson is true)
                    {
                        StringBuilder rowJson = new(100);
                        rowJson.Append('{');
                        foreach (var cell in row.Elements<Cell>())
                        {
                            if (rowJson.Length > 1)
                            {
                                rowJson.Append(',');
                            }
                            if (cell.CellReference?.Value is not null)
                            {
                                rowJson.Append('"');
                                rowJson.Append(cell.CellReference?.Value);
                                rowJson.Append('"');
                                rowJson.Append(':');
                                var value = GetCellValue(cell, span, dateFormatStyleIndices);
                                if (value is null)
                                {
                                    rowJson.Append("null");
                                }
                                else if (value.Length == 0)
                                {
                                    rowJson.Append("\"\"");
                                }
                                else
                                {
                                    rowJson.Append(PgConverters.SerializeString(value));
                                }
                            }
                        }
                        rowJson.Append('}');
                        values = rowJson.ToString();
                    }
                    else
                    {
                        var cells = row.Elements<Cell>().ToArray();
                        if (cells.Length > 0)
                        {
                            var maxColumnIndex = GetColumnIndex(cells[^1].CellReference?.Value);
                            if (rowIndex == 1)
                            {
                                maxColumnIndexFirstRow = maxColumnIndex;
                            }
                            else
                            {
                                if (maxColumnIndexFirstRow > maxColumnIndex)
                                {
                                    maxColumnIndex = maxColumnIndexFirstRow;
                                }
                            }
                            if (maxColumnIndex > 0)
                            {
                                string?[] array = new string?[maxColumnIndex];
                                for (int j = 0; j < cells.Length; j++)
                                {
                                    var cell = cells[j];
                                    int columnIndex = GetColumnIndex(cell.CellReference?.Value);
                                    if (columnIndex > 0 && columnIndex <= maxColumnIndex)
                                    {
                                        array[columnIndex - 1] = GetCellValue(cell, span, dateFormatStyleIndices);
                                    }
                                }
                                values = array;
                            }
                        }
                    }

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
                fileJson.Append(PgConverters.SerializeDatbaseObject(commandResult));
                fileJson.Append('}');

                if (options.LogUploadEvent)
                {
                    logger?.LogInformation("Uploaded file {fileName} ({contentType}, {length} bytes) from Excel sheet {sheetName} as Excel using command {command}",
                        formFile.FileName, formFile.ContentType, formFile.Length, sheetName, rowCommand);
                }
                result.Append(fileJson);
                fileId++;
            }
        }

        result.Append(']');
        return result.ToString();
    }

    public void OnError(NpgsqlConnection? connection, HttpContext context, Exception? exception)
    {
    }

    private static IEnumerable<(WorksheetPart? worksheet, string name)> EnumerateWorksheets(
        WorkbookPart? book, 
        string? targetSheetName, 
        bool allSheets)
    {
        if (book?.Workbook.Sheets is null)
        {
            yield break;
        }
        if (allSheets is false)
        {
            if (string.IsNullOrEmpty(targetSheetName))
            {
                var worksheet = book?.WorksheetParts.FirstOrDefault() ??
                    throw new InvalidOperationException("No worksheet parts data found in the document.");
                string relationshipId = book.GetIdOfPart(worksheet);

                string name =
                    (book.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault(s => string.Equals(s.Id?.Value, relationshipId, StringComparison.Ordinal))?.Name ??
                    relationshipId ??
                    "sheet")!;

                yield return (worksheet, name);
            }
            else
            {
                var sheet = book.Workbook.Sheets.Elements<Sheet>()
                    .FirstOrDefault(s => string.Equals(s.Name?.Value, targetSheetName, StringComparison.OrdinalIgnoreCase));
                if (sheet?.Id?.Value == null)
                {
                    throw new InvalidOperationException($"Sheet '{targetSheetName}' not found in the document.");
                }
                var worksheetPart = (WorksheetPart?)book.GetPartById(sheet.Id.Value);
                yield return worksheetPart == null
                    ? throw new InvalidOperationException($"Worksheet part for sheet '{targetSheetName}' not found in the document.")
                    : ((WorksheetPart? worksheet, string name))(worksheetPart, sheet.Name?.Value ?? targetSheetName);
            }
        }
        else foreach (var sheet in book.Workbook?.Sheets?.Elements<Sheet>() ?? [])
        {
            if (sheet.Id?.Value == null)
            {
                continue;
            }
            var worksheetPart = (WorksheetPart?)book.GetPartById(sheet.Id.Value);
            yield return (worksheetPart, sheet.Name?.Value ?? string.Empty);
        }
    }

    private static bool IsValidExcelFile(IFormFile formFile)
    {
        using var stream = formFile.OpenReadStream();
        try
        {
            using var document = SpreadsheetDocument.Open(stream, false);

            if (document.WorkbookPart == null)
            {
                return false;
            }
            var worksheetParts = document.WorkbookPart.WorksheetParts;
            if (!worksheetParts.Any())
            {
                return false;
            }
            var contentType = document.DocumentType;
            if (contentType != SpreadsheetDocumentType.Workbook &&
                contentType != SpreadsheetDocumentType.Template &&
                contentType != SpreadsheetDocumentType.MacroEnabledWorkbook &&
                contentType != SpreadsheetDocumentType.MacroEnabledTemplate)
            {
                return false;
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string? GetCellValue(Cell cell, Span<string> stringTable, FrozenSet<uint> dateFormatStyleIndices)
    {
        if (cell.CellValue?.Text == null)
        {
            return null;
        }
        string value = cell.CellValue.Text;

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (int.TryParse(value, out var index))
            {
                if (index < 0 || index >= stringTable.Length)
                {
                    return value;
                }
                return stringTable[index];
            }
        }
        else if (cell.DataType?.Value == CellValues.Boolean)
        {
            return value;
        }
        else if (cell.DataType?.Value == CellValues.Date)
        {
            if (double.TryParse(value, out var dateValue))
            {
                return DateTime.FromOADate(dateValue).ToString(_dateTimeFormat);
            }
        }

        if (cell.DataType == null && double.TryParse(value, out var numericValue))
        {
            if (cell.StyleIndex?.Value != null && dateFormatStyleIndices.Contains(cell.StyleIndex.Value))
            {
                try
                {
                    var dateTime = DateTime.FromOADate(numericValue);
                    if (numericValue < 1.0)
                    {
                        return dateTime.ToString(_timeFormat);
                    }
                    else if (numericValue % 1 != 0)
                    {
                        return dateTime.ToString(_dateTimeFormat);
                    }
                    else
                    {
                        return dateTime.ToString(_dateFormat);
                    }
                }
                catch
                {
                    return value;
                }
            }
            return value;
        }

        return value;
    }

    private static int GetColumnIndex(ReadOnlySpan<char> cellReference)
    {
        if (cellReference.IsEmpty)
        {
            return 0;
        }
        int columnIndex = 0;
        foreach (char c in cellReference)
        {
            if (c >= 'A' && c <= 'Z')
            {
                columnIndex = columnIndex * 26 + (c - 'A' + 1);
            }
            else if (c >= 'a' && c <= 'z')
            {
                columnIndex = columnIndex * 26 + (c - 'a' + 1);
            }
            else
            {
                break; // Hit number portion
            }
        }
        return columnIndex;
    }

    private static FrozenSet<uint> BuildDateFormatCache(Stylesheet? stylesheet)
    {
        var dateFormatStyleIndices = new HashSet<uint>();

        if (stylesheet?.CellFormats == null)
        {
            return dateFormatStyleIndices.ToFrozenSet();
        }

        var cellFormats = stylesheet.CellFormats.Elements<CellFormat>().ToArray();

        for (uint styleIndex = 0; styleIndex < cellFormats.Length; styleIndex++)
        {
            var cellFormat = cellFormats[styleIndex];
            if (cellFormat?.NumberFormatId?.Value == null)
                continue;

            uint numberFormatId = cellFormat.NumberFormatId.Value;
            if ((numberFormatId >= 14 && numberFormatId <= 22) || (numberFormatId >= 176 && numberFormatId <= 182))
            {
                dateFormatStyleIndices.Add(styleIndex);
                continue;
            }

            // Check custom number formats (≥164)
            if (numberFormatId >= 164)
            {
                var numberingFormat = stylesheet.NumberingFormats?.Elements<NumberingFormat>()
                    .FirstOrDefault(nf => nf.NumberFormatId?.Value == numberFormatId);

                if (numberingFormat?.FormatCode?.Value != null)
                {
                    string formatCode = numberingFormat.FormatCode.Value.ToLower();

                    // Check if format contains date/time indicators
                    if (formatCode.Contains("yyyy") || formatCode.Contains("mm") || formatCode.Contains("dd") ||
                        formatCode.Contains("hh") || formatCode.Contains("ss") || formatCode.Contains("am/pm") ||
                        formatCode.Contains("h:") || formatCode.Contains("m:") || formatCode.Contains("s:") ||
                        formatCode.Contains("yy") || formatCode.Contains("mmm") || formatCode.Contains("mmmm"))
                    {
                        dateFormatStyleIndices.Add(styleIndex);
                    }
                }
            }
        }
        return dateFormatStyleIndices.ToFrozenSet();
    }
}