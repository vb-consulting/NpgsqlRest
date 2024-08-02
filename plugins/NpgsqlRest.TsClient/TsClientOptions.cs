﻿namespace NpgsqlRest.TsClient;

public class TsClientOptions(
    string filePath = default!,
    bool fileOverwrite = false,
    bool includeHost = false,
    string? customHost = null,
    CommentHeader commentHeader = CommentHeader.Simple,
    bool commentHeaderIncludeComments = true,
    bool includeStatusCode = false,
    bool bySchema = false,
    bool createSeparateTypeFile = true,
    string? importBaseUrlFrom = null,
    string? importParseQueryFrom = null,
    bool includeParseUrlParam = false,
    bool includeParseRequestParam = false,
    string[]? skipRoutineNames = null,
    string[]? skipFunctionNames = null,
    string[]? skipPaths = null,
    string defaultJsonType = "string",
    bool useRoutineNameInsteadOfEndpoint = false)
{
    /// <summary>
    /// File path for the generated code. Set to null to skip the code generation. Use {0} to set schema name when BySchema is true
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

    /// <summary>
    /// When CommentHeader is set to Simple or Full, set to true to include routine comments in comment header.
    /// </summary>
    public bool CommentHeaderIncludeComments { get; set; } = commentHeaderIncludeComments;

    /// <summary>
    /// Set to true to include status code in response: {status: response.status, response: model}
    /// </summary>
    public bool IncludeStatusCode { get; set; } = includeStatusCode;

    /// <summary>
    /// Create files by PostgreSQL schema. File name will use formatted FilePath where {0} is is the schema name in the pascal case.
    /// </summary>
    public bool BySchema { get; set; } = bySchema;

    /// <summary>
    /// Create separate file with global types {name}Types.d.ts
    /// </summary>
    public bool CreateSeparateTypeFile { get; set; } = createSeparateTypeFile;

    /// <summary>
    /// Lines to add to each header. {0} format placeholder is current timestamp
    /// </summary>
    public List<string> HeaderLines { get; set; } = ["// autogenerated at {0}", "", ""];

    /// <summary>
    /// Module name to import "baseUrl" constant, instead of defining it in a module.
    /// </summary>
    public string? ImportBaseUrlFrom { get; set; } = importBaseUrlFrom;

    /// <summary>
    /// Module name to import "pasreQuery" function, instead of defining it in a module.
    /// </summary>
    public string? ImportParseQueryFrom { get; set; } = importParseQueryFrom;

    /// <summary>
    /// Include optional parameter `parseUrl: (url: string) => string = url=>url` that will parse constructed url.
    /// </summary>
    public bool IncludeParseUrlParam { get; set; } = includeParseUrlParam;

    /// <summary>
    /// Include optional parameter `parseRequest: (request: RequestInit) => RequestInit = request=>request` that will parse constructed request.
    /// </summary>
    public bool IncludeParseRequestParam { get; set; } = includeParseRequestParam;

    /// <summary>
    /// Array of routine names to skip (without schema)
    /// </summary>
    public string[] SkipRoutineNames { get; set; } = skipRoutineNames ?? [];

    /// <summary>
    /// Array of generated function names to skip (without schema)
    /// </summary>
    public string[] SkipFunctionNames { get; set; } = skipFunctionNames ?? [];

    /// <summary>
    /// Array of url paths to skip
    /// </summary>
    public string[] SkipPaths { get; set; } = skipPaths ?? [];

    /// <summary>
    /// Array of schema names to skip
    /// </summary>
    public string[] SkipSchemas { get; set; } = skipPaths ?? [];

    /// <summary>
    /// Default TypeScript type for JSON types.
    /// </summary>
    public string DefaultJsonType { get; set; } = defaultJsonType;

    /// <summary>
    /// Use routine name instead of endpoint name when generating a function name.
    /// </summary>
    public bool UseRoutineNameInsteadOfEndpoint { get; set; } = useRoutineNameInsteadOfEndpoint;
}
