namespace NpgsqlRest;

public enum CommentHeader { None, Simple, Full }
public enum HttpFileMode { Database, Schema }

public class NpgsqlRestHttpFileOptions(
    bool enabled = true,
    string fileNamePattern = "{0}{1}",
    CommentHeader commentHeader = CommentHeader.Simple,
    HttpFileMode fileMode = HttpFileMode.Database,
    bool overwrite = false,
    bool exposeAsTextEndpoint = false)
{
    /// <summary>
    /// Enables or disables the HttpFile feature.
    /// </summary>
    public bool Enabled { get; set; } = enabled;
    /// <summary>
    /// The pattern to use when generating file names. {0} is database name, {1} is schema suffix with underline when FileMode is set to Schema.
    /// Use this property to set the custom file name.
    /// .http extension will be added automatically.
    /// </summary>
    public string FileNamePattern { get; set; } = fileNamePattern;
    /// <summary>
    /// Adds comment header to above request based on PostrgeSQL routine
    /// Set None to skip.
    /// Set Simple (default) to add name, parameters and return values to comment header.
    /// Set Full to add the entire routine code as comment header.
    /// </summary>
    public CommentHeader CommentHeader { get; set; } = commentHeader;
    /// <summary>
    /// Set to Database to create one http file for entire database.
    /// Set to Schema to create new http file for every database schema.
    /// </summary>
    public HttpFileMode FileMode { get; set; } = fileMode;
    /// <summary>
    /// Set to true to overwrite existing files.
    /// </summary>
    public bool Overwrite { get; set; } = overwrite;
    /// <summary>
    /// Set to true to expose content of http files as endpoint instead of creating file on disk.
    /// </summary>
    public bool ExposeAsTextEndpoint { get; set; } = exposeAsTextEndpoint;
}
