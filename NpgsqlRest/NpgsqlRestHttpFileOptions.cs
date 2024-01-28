namespace NpgsqlRest;

public enum HttpFileOption { Disabled, File, Endpoint, Both }
public enum CommentHeader { None, Simple, Full }
public enum HttpFileMode { Database, Schema }

public class NpgsqlRestHttpFileOptions(
    HttpFileOption option = HttpFileOption.Both,
    string namePattern = "{0}{1}",
    CommentHeader commentHeader = CommentHeader.Simple,
    bool commentHeaderIncludeComments = true,
    HttpFileMode fileMode = HttpFileMode.Database,
    bool fileOverwrite = false)
{
    /// <summary>
    /// Options for HTTP file generation:
    /// Disabled - skip.
    /// File - creates a file on disk.
    /// Endpoint - exposes file content as endpoint.
    /// Both - creates a file on disk and exposes file content as endpoint.
    /// </summary>
    public HttpFileOption Option { get; set; } = option;
    /// <summary>
    /// The pattern to use when generating file names. {0} is database name, {1} is schema suffix with underline when FileMode is set to Schema.
    /// Use this property to set the custom file name.
    /// .http extension will be added automatically.
    /// </summary>
    public string NamePattern { get; set; } = namePattern;
    /// <summary>
    /// Adds comment header to above request based on PostgreSQL routine
    /// Set None to skip.
    /// Set Simple (default) to add name, parameters and return values to comment header.
    /// Set Full to add the entire routine code as comment header.
    /// </summary>
    public CommentHeader CommentHeader { get; set; } = commentHeader;
    /// <summary>
    /// When CommentHeader is set to Simple or Full, set to true to include routine comments in comment header.
    /// </summary>
    public bool CommentHeaderIncludeComments { get; set; } = commentHeaderIncludeComments;
    /// <summary>
    /// Set to Database to create one http file for entire database.
    /// Set to Schema to create new http file for every database schema.
    /// </summary>
    public HttpFileMode FileMode { get; set; } = fileMode;
    /// <summary>
    /// Set to true to overwrite existing files.
    /// </summary>
    public bool FileOverwrite { get; set; } = fileOverwrite;
}
