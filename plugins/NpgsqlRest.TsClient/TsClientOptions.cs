namespace NpgsqlRest.TsClient;

public class TsClientOptions(
    string filePath = default!,
    bool fileOverwrite = false,
    bool includeHost = false,
    string? customHost = null,
    CommentHeader commentHeader = CommentHeader.Simple)
{
    /// <summary>
    /// File path for the generated code. Set to null to skip the code generation.
    /// </summary>
    public string? FilePath { get; set; } = filePath;
    /// <summary>
    /// Force file overwrite.
    /// </summary>
    public bool FileOverwrite { get; set; } = fileOverwrite;
    /// <summary>
    /// Include current host information in the URL prefix.
    /// </summary>
    public bool IncludeHost { get; set; } = includeHost;
    /// <summary>
    /// Set the custom host prefix information.
    /// </summary>
    public string? CustomHost { get; set; } = customHost;
    /// <summary>
    /// Adds comment header to above request based on PostgreSQL routine
    /// Set None to skip.
    /// Set Simple (default) to add name, parameters and return values to comment header.
    /// Set Full to add the entire routine code as comment header.
    /// </summary>
    public CommentHeader CommentHeader { get; set; } = commentHeader;
}
