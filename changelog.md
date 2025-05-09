# Changelog

Note: The changelog for the older version can be found here: [Changelog Archive](https://github.com/vb-consulting/NpgsqlRest/blob/master/changelog-old.md)

---

## Version [2.25.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.25.0) (2025-05-06)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.25.0...2.24.0)

### Core NpgsqlRest Library

#### Support for Custom Parameters

Custom parameters support for created endpoint has been added.

For now, only upload handlers are using custom parameters:

- `large_object` upload handler have following parameters:

1) `oid`: set the custom `oid` number to be used. By default, new `oid` is assigned on each upload.
2) `buffer_size`: set the custom buffer size. Default is 8192 or 8K buffer size

- `file_system` upload handler ave following parameters:

1) `path`: set the custom path for upload. Default is current path `./`
2) `file`: set the custom file name for upload. Default is whatever name is received in request.
3) `unique_name`: boolean field that, if true, will automatically set file name to be unique (name is GUID and extension is the same). Can only have true or false values (case insensitive). Default is true.
4) `create_path`: boolean field that, if true, will check if the path exists, and create it if it doesn't. Can only have true or false values (case insensitive). Default is false.
5) `buffer_size`: set the custom buffer size. Default is 8192 or 8K buffer size.

- Custom parameters can be set programmatically in `CustomParameters` dictionary for each endpoint:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreated = endpoint =>
    {
        if (endpoint?.Routine.Name == "upload")
        {
            endpoint.CustomParameters = new() { ["unique_name"] = "false"};
        }
    }
});
```

- Custom parameters can be set by using comment annotations.
- Each comment annotations line that:
  - has equal character `=`
  - first part, before equal character is word (alphanumerics, `_`, `-`)
- For example:

```sql
create function my_upload(
    _meta json = null
)
returns json 
language plpgsql
as 
$$
begin
    -- return same upload metadata
    return _meta;
end;
$$;

comment on function my_upload(json) is '
upload for file_system
param _meta is upload metadata
path = ./uploads
unique_name = false
create_path = false
';
```

- Custom parameters annotations can be set from the parameter value, using default formatter (name enclosed with `{` and `}`). Parameter name can be original or parsed. For example:

```sql
create function my_upload(
    _path text,
    _file text,
    _unique_name boolean,
    _create_path boolean,
    _meta json = null
)
returns json 
language plpgsql
as 
$$
begin
    -- return same upload metadata
    return _meta;
end;
$$;

comment on function my_upload(text, text, boolean, boolean, json) is '
upload for file_system
param _meta is upload metadata
path = {_path}
file = {_file}
unique_name = {_unique_name}
create_path = {_create_path}
';
```

- Note: parameter name can be original name or parsed name. Default name parser is camel case, so the names like `path`, `file`, `uniqueName` or `createPath` are equally valid.
- This example can be used like this: 

```csharp
// csvContent is the string content
var fileName = "test-data.csv";
var contentBytes = Encoding.UTF8.GetBytes(csvContent);
using var formData = new MultipartFormDataContent();
using var byteContent = new ByteArrayContent(contentBytes);
byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
formData.Add(byteContent, "file", fileName);

var query = new QueryBuilder
{
    { "path", "./test" },
    { "file", fileName },
    { "uniqueName", "false" },
    { "createPath", "true" },
};

using var result = await test.Client.PostAsync($"/api/my-upload/{query}", formData);

//
// upload is saved as ./test/test-data.csv
//
```

#### Other Improvements

- Improved parameter parsing for header values set by comment annotations
- Now using high speed and well tested template parser to parse `{name}` formats
- When using header values set by comment annotations, header name must be a single word (alphanumerics, `_`, `-`).
- When using header values set by comment annotations that should be parsed from parameter values, parameter name can also be parsed name:

```sql
create function header_template_response1(_type text, _file text) 
returns table(n numeric, d timestamp, b boolean, t text)
language sql
as 
$$
select sub.* 
from (
values 
    (123, '2024-01-01'::timestamp, true, 'some text'),
    (456, '2024-12-31'::timestamp, false, 'another text')
)
sub (n, d, b, t)
$$;

comment on function header_template_response1(text, text) is '
raw
separator ,
newline \n
Content-Type: {_type}
Content-Disposition: attachment; filename={_file}
```

This is also valid, when using default name parser:

```sql
comment on function header_template_response1(text, text) is '
raw
separator ,
newline \n
Content-Type: {type}
Content-Disposition: attachment; filename={file}
```

### NpgsqlRest Client App

#### Added Data Protection

- Data protection mechanism is included from this version.
- Data protection helps securely store encryption keys for encrypted cookies and antiforgery tokens.
- When data protection is not enabled, and application is using authentication cookies, user will be invalidated (signed out) every time application restarts (redeployment scenario).
- Three modes are supported:
1) `Default`: Windows only.
2) `FileSystem`: Requires setting path in `FileSystemPath` for storing keys. Note: when using Docker, this path must be volume path to persist after restarts.
3) `Database`: Store keys in database require setting `GetAllElementsCommand` (expected to return rows with a single column of type text and have no parameters) and `StoreElementCommand` (receives two parameters: name and data of type text. Doesn't return anything).

- New configuration section:

```jsonc
{
  //
  // Data protection settings. Encryption keys for Auth Cookies and Antiforgery tokens.
  //
  "DataProtection": {
    "Enabled": true,
    //
    // Set to null to use the current "ApplicationName"
    //
    "CustomApplicationName": null,
    //
    // Sets the default lifetime in days of keys created by the data protection system.
    //
    "DefaultKeyLifetimeDays": 90,
    //
    // Data protection location: "Default", "FileSystem" or "Database"
    //
    // Note: When running on Linux, using Default keys will not be persisted. 
    // When keys are lost on restarts, Cookie Auth and Antiforgery tokens will also not work on restart.
    // Linux users should use FileSystem or Database storage.
    //
    "Storage": "Default",
    //
    // FileSystem storage path. Set to a valid path when using FileSystem.
    // Note: When running in Docker environment, the path must be a Docker volume path.
    //
    "FileSystemPath": "./data-protection-keys",
    //
    // GetAllElements database command. Expected to rows with a single column of type text.
    //
    "GetAllElementsCommand": "select get_data_protection_keys()",
    //
    // StoreElement database command. Receives two parameters: name and data of type text. Doesn't return anything.
    //
    "StoreElementCommand": "call store_data_protection_keys($1,$2)"
  }
}
```

#### Other Improvements

- Fixed `Auth.CookieValidDays` bug which prevented setting validation different than 90 days.

## Version [2.24.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.24.0) (2025-04-29)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.24.0...2.23.0)

### Core NpgsqlRest Library

- Added support for callback command when password verification fails on login.

### NpgsqlRest Client App

- Added support for callback command when password verification fails on login.
- Fixed data parameter for external login to send actual retrieved data.
- Fixed Docker image to have valid certificate.

## Version [2.23.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.23.0) (2025-04-28)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.23.0...2.22.0)

### TsClient Plugin Fixes

- Fixed TsClient plugin to handle booleans correctly.
- Added JsCode style comments for parameters and return values in TsClient plugin.
- Added upload support for TS and JS client.
- Added support for XsrfTokenHeaderName if used. This is used by the Upload endpoints.
- Smaller fixes in the TsClient plugin to handle some edge cases.

### Core NpgsqlRest Library

#### Simplified EndpointCreated Option Event

- From now on, this event doesn't require returning an endpoint, since it receives the endpoint instance as the parameter:

```csharp
/// <summary>
/// Callback function that is executed just after the new endpoint is created. Set the RoutineEndpoint to null to disable endpoint.
/// </summary>
public Action<RoutineEndpoint?>? EndpointCreated { get; set; } = null;
```

To change endpoint properties, simply change them on the parameter instance directly or set to null to disable if necessary:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreated = endpoint => 
    {
        if (endpoint?.Routine.Name == "restricted")
        {
            // disable the endpoint
            endpoint = null;
        }
    }
});
```

#### Added PATH to Comment Annotation Parser 

- New comment annotation: `PATH path`.
- Ability to set just HTTP path without method by using `PATH /my-path/`
- If HTTP tag has only two params and second param is not valid VERB (GET, POST, etc) - it is treated as the path.

#### Added SecuritySensitive Routine Option

- New Endpoint Option and Comment Annotation:
    `securitysensitive`
    `sensitive`
    `security`
    `security_sensitive`
    `security-sensitive`

- This will manually obfuscate all parameter values before sending them to log.

#### Hashing Capabilities

- This is a completely new feature. There is a default hasher class that can be injected into the system with the new option called `PasswordHasher`.

```csharp
/// <summary>
/// Default password hasher object. Inject custom password hasher object to add default password hasher.
/// </summary>
public IPasswordHasher PasswordHasher { get; set; } = new PasswordHasher();
```

- Default implementation in `PasswordHasher` class is using PBKDF2 (Password-Based Key Derivation Function 2) with SHA-256 and it incorporates 128-bit salt with 600,000 iterations (OWASP-recommended as of 2025). So it's secure, but there is an option to inject a different one.

##### Mark Parameter as Hash

- Ability to mark the parameter as hash and the value will be pre-hashed with the default hasher.

- New comment annotations: 
    - `param param_name1 is hash of param_name2` - the first parameter `param_name1` will have the hashed value of `param_name2`.
    - `param param_name is hash of param_name` - this parameter `param_name` will have the original value hashed.
    - Typical usage: `param _hashed_password is hash of _password` or just `param _password is hash of _password`.

- This can be set programmatically also: 

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreated = endpoint => 
    {
        if (endpoint?.Routine.Name == "login")
        {
            // Set the second parameter to contain the hash instead of plain text
            endpoint.Routine.Parameters[1].HashOf = endpoint.Routine.Parameters[1];
        }
    }
});
```

##### Login Endpoints Can Verify Hashes

- New ability of the Login endpoints to compare and verify returned password hashes.

- Login endpoints can now return field `hash` (configurable) and when they do, it will be verified against a parameter that contains "pass" (configurable).

- If verification fails, login will automatically return status 404 with no message and relevant information will be logged as a warning to default logger: `Password verification failed for attempted login: path={path} userId={userId}, username={userName}`

- New `AuthenticationOptions` values to support this feature are these:

1) `AuthenticationOptions.HashColumnName`

    - Type: `string`
    - Default: `"hash"`

    The default column name in the data reader that will contain password hash. If this column is present, value will be verified with the default hasher against the password parameter.

2) `AuthenticationOptions.PasswordParameterNameContains`

- Type: `string`
- Default: `"pass"`

When the login endpoint tries to verify supplied hash with the password parameter, this value will be used to locate the password parameter from the parameter collection. That will be the first parameter that contains this string in parameter name (either original or translated). 

#### Upload Support

- There is robust and flexible UPLOAD endpoint support from this version.

- There are three new options to support this feature:

```csharp
/// <summary>
/// Default upload handler options. 
/// Set this option to null to disable upload handlers or use this to modify upload handler options.
/// </summary>
public UploadHandlerOptions DefaultUploadHandlerOptions { get; set; } = new UploadHandlerOptions();

/// <summary>
/// Upload handlers dictionary map. 
/// When the endpoint has set Upload to true, this dictionary will be used to find the upload handlers for the current request. 
/// Handler will be located by the key values from the endpoint UploadHandlers string array property if set or by the default upload handler (DefaultUploadHandler option).
/// Set this option to null to use default upload handler from the UploadHandlerOptions property.
/// Set this option to empty dictionary to disable upload handlers.
/// Set this option to a dictionary with one or more upload handlers to enable your own custom upload handlers.
/// </summary>
public Dictionary<string, Func<IUploadHandler>>? UploadHandlers { get; set; } = null;

/// <summary>
/// Default upload handler name. This value is used when the upload handlers are not specified.
/// </summary>
public string DefaultUploadHandler { get; set; } = "large_object";
```

- Endpoints also have two new properties:

```csharp
public bool Upload { get; set; } = true;
public string[]? UploadHandlers { get; set; } = null;
```

- When the endpoint has Upload set to true, the request will first try to locate appropriate handlers.

- Endpoint can specify one or more handlers with `UploadHandlers` property (keys in `UploadHandlers` dictionary).

- When endpoint `UploadHandlers` property is null, Upload handler will use the one from the `DefaultUploadHandler` option ("large_object" by default).

- Option `Dictionary<string, Func<IUploadHandler>>? UploadHandlers` is initialized from the `DefaultUploadHandlerOptions` option which has these defaults:

```csharp
public bool UploadsEnabled { get; set; } = true;
public bool LargeObjectEnabled { get; set; } = true;
public string LargeObjectKey { get; set; } = "large_object";
public int LargeObjectHandlerBufferSize { get; set; } = 8192;
public bool FileSystemEnabled { get; set; } = true;
public string FileSystemKey { get; set; } = "file_system";
public string FileSystemHandlerPath { get; set; } = "./";
public bool FileSystemHandlerUseUniqueFileName { get; set; } = true;
public bool FileSystemHandlerCreatePathIfNotExists { get; set; } = true;
public int FileSystemHandlerBufferSize { get; set; } = 8192;
```

- Each upload handler returns a string text by convention, which usually represents JSON metadata for the upload.
  
- This metadata is then assigned to a routine parameter that has `UploadMetadata` set to true.

- That routine is executed on PostgreSQL after successful upload (handlers execution).
  
- If the routine fails, upload handlers automatically perform upload cleanup.
  
- There are currently two upload handlers implemented in the library:

1) PostgreSQL Large Object Upload Handler

- Key: `large_object`
- Description: uses [PostgreSQL Large Object API](https://www.postgresql.org/docs/current/largeobjects.html) to upload content directly to database.
- Metadata example: `{"type": "large_object", "fileName": "test.txt", "fileType": "text/plain", "size": 100, "oid": 1234}`

2) File System Upload Handler

- Key: `file_system`
- Description: Uploads files to the file system
- Metadata example: `{"type": "file_system", "fileName": "test.txt", "fileType": "text/plain", "size": 100, "filePath": "/tmp/uploads/ADF3B177-D0A5-4AA0-8805-FB63F8504ED8.txt"}`

- If the endpoint has multiple upload handlers, metadata parameter is expected to be array of text or array of JSON.

- Example of programmatic usage:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    DefaultUploadHandlerOptions = new() { FileSystemHandlerPath = "./images/" },
    EndpointCreated = endpoint =>
    {
        if (endpoint?.Url.EndsWith("upload") is true)
        {
            endpoint.Upload = true;

            if (endpoint?.Url.Contains("image") is true)
            {
                endpoint.UploadHandlers = ["file_system"];
            }
            else if (endpoint?.Url.Contains("csv") is true)
            {
                endpoint.UploadHandlers = ["large_object"];
            }
        }
    }
});
```

- This example will enable upload for all URL paths that end with "upload" text. 
- If the URL path contains "image", it will upload them to file system and to configured `./images/` path. 
- If the URL path contains "csv", it will be uploaded as the PostgreSQL large object.

- There is also robust comment annotation support for this feature:
  - `upload` - mark routine as upload (uses default handlers)
  - `upload for handler_name1, handler_name2 [, ...]` mark routine as upload and set handler key to be used (e.g. `upload for file_system`).
  - `upload param_name as metadata` mark routine as upload (uses default handlers) and sets `param_name` as metadata parameter. 
  - Note: mixing these two is not (yet) possible, `upload for file_system param_name as metadata` or `upload param_name as metadata for file_system` will not work.
  - `param param_name1 is upload metadata` set `param_name1` as the upload metadata.

- Examples:

```sql
create procedure simple_upload(
    _meta json = null
)
language plpgsql
as 
$$
begin
    raise info 'upload metadata: %', _meta;
end;
$$;

comment on procedure simple_upload(json) is 'upload'; -- does not send _meta parameter
-- or --
comment on procedure simple_upload(json) is 'upload _meta as metadata' -- sends _meta parameter
-- or --
comment on procedure simple_upload(json) is '
upload
param _meta is upload metadata
'; 
-- or --
comment on procedure simple_upload(json) is '
upload for file_system
param _meta is upload metadata
';
-- or --
comment on procedure simple_upload(json) is '
upload for large_object
param _meta is upload metadata
';
```

- In case of multiple handlers, parameter has to be an array:

```sql
create procedure simple_upload(
    _meta[] json = null
)
language plpgsql
as 
$$
begin
    raise info 'upload metadata: %', _meta;
end;
$$;

-- multiple handlers
comment on procedure simple_upload(json) is '
upload for large_object, file_system
param _meta is upload metadata
';
```

#### Other Improvements

1) Fixed issue with endpoint with default parameters. When they receive non-existing parameters in same number as default parameters, the endpoint now returns 404 instead of 200 error as it should be.

2) Fixed serialization of binary data in the response. From now on, endpoints that return either:
 - single value of type bytea (binary)
 - single column of type setof bytea (binary)
 - will be written raw directly to response. 
 - This allows, for example, displaying images directly from the database.

### NpgsqlRest Client App

#### External Login Fixes and Improvements

- External login was fundamentally broken