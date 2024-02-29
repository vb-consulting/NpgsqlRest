namespace NpgsqlRest.TsClient;

public class TsClientOptions(
    string filePath = default!,
    bool fileOverwrite = false,
    bool includeHost = false,
    string? customHost = null,
    CommentHeader commentHeader = CommentHeader.Simple)
{
    public string FilePath { get; set; } = filePath;
    public bool FileOverwrite { get; set; } = fileOverwrite;
    public bool IncludeHost { get; set; } = includeHost;
    public string? CustomHost { get; set; } = customHost;
    public CommentHeader CommentHeader { get; set; } = commentHeader;
}
