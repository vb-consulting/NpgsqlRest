﻿# NpgsqlRest.HttpFiles

**Automatic HTTP File Client Code Generation for NpgsqlRest**

**Metadata plug-in** for the `NpgsqlRest` library. 

Provides support for the generation of the **[HTTP Client Files](https://learn.microsoft.com/en-us/aspnet/core/test/http-files?view=aspnetcore-8.0).**

## Overview

Outputs a file or multiple files with the `.http` extension containing the HTTP request definitions for endpoints generated by NpgsqlRest. 

The `.http` file format and editor were inspired by the [Visual Studio Code REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client). 

Example of the generated file for the `hello_world` function:


```console
@host=http://localhost:5000

###

// function public.hello_world()
// returns text
POST {{host}}/api/my-schema/hello-world

```

All generated `.http` files will have randomly filled parameter values and they are ready to run and test.

```console
@host=http://localhost:5000

###

// function public.test_func_1(
//     _p1 integer,
//     _p2 integer
// )
// returns text
POST {{host}}/api/test-func-1
content-type: application/json

{
    "p1": 1,
    "p2": 2
}

###

// function public.test_func_2(
//     _p1 integer,
//     _p2 integer
// )
// returns text
GET {{host}}/api/test-func-2?p1=1&p2=2

```

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

#### Library Dependencies

- NpgsqlRest 2.0.0

## Contributing

Contributions from the community are welcomed.
Please make a pull request with a description if you wish to contribute.

## License

This project is licensed under the terms of the MIT license.
