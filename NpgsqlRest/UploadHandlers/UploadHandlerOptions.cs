namespace NpgsqlRest.UploadHandlers;

public class UploadHandlerOptions
{
    public int TextTestBufferSize { get; set; } = 4096;
    public int TextNonPrintableThreshold { get; set; } = 5;
    public AllowedImageTypes AllowedImageTypes { get; set; } = 
        AllowedImageTypes.Jpeg | AllowedImageTypes.Png | AllowedImageTypes.Gif | AllowedImageTypes.Bmp | AllowedImageTypes.Tiff | AllowedImageTypes.Webp;

    public bool LargeObjectEnabled { get; set; } = true;
    public string LargeObjectKey { get; set; } = "large_object";
    public string[]? LargeObjectIncludedMimeTypePatterns { get; set; } = null;
    public string[]? LargeObjectExcludedMimeTypePatterns { get; set; } = null;
    public int LargeObjectHandlerBufferSize { get; set; } = 8192;

    public bool FileSystemEnabled { get; set; } = true;
    public string FileSystemKey { get; set; } = "file_system";
    public string[]? FileSystemIncludedMimeTypePatterns { get; set; } = null;
    public string[]? FileSystemExcludedMimeTypePatterns { get; set; } = null;
    public string FileSystemHandlerPath { get; set; } = "./";
    public bool FileSystemHandlerUseUniqueFileName { get; set; } = true;
    public bool FileSystemHandlerCreatePathIfNotExists { get; set; } = false;
    public int FileSystemHandlerBufferSize { get; set; } = 8192;

    public bool CsvUploadEnabled { get; set; } = true;
    public string CsvUploadKey { get; set; } = "csv";
    public string[]? CsvUploadIncludedMimeTypePatterns { get; set; } = null;
    public string[]? CsvUploadExcludedMimeTypePatterns { get; set; } = null;
    public bool CsvUploadCheckFileStatus { get; set; } = true;
    public string CsvUploadDelimiterChars { get; set; } = ",";
    public bool CsvUploadHasFieldsEnclosedInQuotes { get; set; } = true;
    public bool CsvUploadSetWhiteSpaceToNull { get; set; } = true;
    public string CsvUploadRowCommand { get; set; } = "call process_csv_row($1,$2,$3,$4)";
}
