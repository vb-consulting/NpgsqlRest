# NpgsqlRest.HttpFiles

**Metadata plug-in** for the `NpgsqlRest` library. 

Provides support for the **[HTTP files](https://learn.microsoft.com/en-us/aspnet/core/test/http-files?view=aspnetcore-8.0).**

## Install 

```console
dotnet add package NpgsqlRest.HttpFiles --version 1.0.0
```

## Minimal Usage 

Initialize `EndpointCreateHandlers` options property as an array containing an `HttpFile` plug-in instance:

```csharp
using NpgsqlRest;
using NpgsqlRest.HttpFiles;

var app = builder.Build();
app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    EndpointCreateHandlers = [new HttpFile()],
});
app.Run();
```

## HttpFile options

```csharp
using NpgsqlRest;
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
            Option = HttpFileOption.Both,
            /// <summary>
            /// The pattern to use when generating file names. {0} is database name, {1} is schema suffix with underline when FileMode is set to Schema.
            /// Use this property to set the custom file name.
            /// .http extension will be added automatically.
            /// </summary>
            NamePattern = "{0}{1}",
            /// <summary>
            /// Adds comment header to above request based on PostgreSQL routine
            /// Set None to skip.
            /// Set Simple (default) to add name, parameters and return values to comment header.
            /// Set Full to add the entire routine code as comment header.
            /// </summary>
            CommentHeader = CommentHeader.Simple,
            /// <summary>
            /// When CommentHeader is set to Simple or Full, set to true to include routine comments in comment header.
            /// </summary>
            CommentHeaderIncludeComments = true,
            /// <summary>
            /// Set to Database to create one http file for entire database.
            /// Set to Schema to create new http file for every database schema.
            /// </summary>
            FileMode = HttpFileMode.Database,
            /// <summary>
            /// Set to true to overwrite existing files.
            /// </summary>
            FileOverwrite = false,
            /// <summary>
            /// The connection string to the database used in NpgsqlRest.
            /// Used to get the name of the database for the file name.
            /// If Name property is set, this property is ignored.
            /// </summary>
            ConnectionString = null,
            /// <summary>
            /// File name. If not set, the database name will be used if connection string is set.
            /// If neither ConnectionString nor Name is set, the file name will be "npgsqlrest".
            /// </summary>
            Name = null
        })
    ],
});
app.Run();
```