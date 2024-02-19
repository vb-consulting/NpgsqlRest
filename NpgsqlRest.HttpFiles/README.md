# NpgsqlRest.HttpFiles.csproj

Plug-in for the `NpgsqlRest` library that provides support for the [HTTP files](https://learn.microsoft.com/en-us/aspnet/core/test/http-files?view=aspnetcore-8.0).

Minimal Usage:

```csharp
using NpgsqlRest.HttpFiles;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreateHandlers = [new HttpFile(new())],
});
app.Run();
```

`HttpFile` options:

```csharp
using NpgsqlRest.HttpFiles;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreateHandlers = [
        new HttpFile(new HttpFileOptions
        {
            /// <summary>
            /// Options for HTTP file generation:
            /// Disabled - skip.
            /// File - creates a file on disk.
            /// Endpoint - exposes file content as endpoint.
            /// Both - creates a file on disk and exposes file content as endpoint.
            /// </summary>
            Option = GetEnum<HttpFileOption>("Option", httpFileOptions),
            /// <summary>
            /// The pattern to use when generating file names. {0} is database name, {1} is schema suffix with underline when FileMode is set to Schema.
            /// Use this property to set the custom file name.
            /// .http extension will be added automatically.
            /// </summary>
            NamePattern = GetStr("NamePattern", httpFileOptions) ?? "{0}{1}",
            /// <summary>
            /// Adds comment header to above request based on PostgreSQL routine
            /// Set None to skip.
            /// Set Simple (default) to add name, parameters and return values to comment header.
            /// Set Full to add the entire routine code as comment header.
            /// </summary>
            CommentHeader = GetEnum<CommentHeader>("CommentHeader", httpFileOptions),
            /// <summary>
            /// When CommentHeader is set to Simple or Full, set to true to include routine comments in comment header.
            /// </summary>
            CommentHeaderIncludeComments = GetBool("CommentHeaderIncludeComments", httpFileOptions),
            /// <summary>
            /// Set to Database to create one http file for entire database.
            /// Set to Schema to create new http file for every database schema.
            /// </summary>
            FileMode = GetEnum<HttpFileMode>("FileMode", httpFileOptions),
            /// <summary>
            /// Set to true to overwrite existing files.
            /// </summary>
            FileOverwrite = GetBool("FileOverwrite", httpFileOptions),
            /// <summary>
            /// The connection string to the database used in NpgsqlRest.
            /// Used to get the name of the database for the file name.
            /// If Name property is set, this property is ignored.
            /// </summary>
            ConnectionString = connectionString,
            /// <summary>
            /// File name. If not set, the database name will be used if connection string is set.
            /// If neither ConnectionString nor Name is set, the file name will be "npgsqlrest".
            /// </summary>
            ConnectionString = connectionString
        })
    ],
});
app.Run();
```