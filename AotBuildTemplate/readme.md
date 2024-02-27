## NpgsqlRest AOT Build Template

This directory contains a source code for a project used to build a native AOT executable. 
This source was used to build to publish the [latest releases of downloadable standalone executables](https://github.com/vb-consulting/NpgsqlRest/releases). 

Read more on native [AOT builds here](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)

You can use this project to customize your build.

To publish Windows AOT build use:

```console
dotnet publish -r win-x64 -c Release --output [output dir]
```

To publish Linux AOT build use:

```console
dotnet publish -r linux-x64 -c Release --output [output dir]
```
