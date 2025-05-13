# Changelog

Note: The changelog for the older version can be found here: [Changelog Archive](https://github.com/vb-consulting/NpgsqlRest/blob/master/changelog-old.md)

---

  "StaticFiles": {
    "Enabled": false,
    "RootPath": "wwwroot",
    //
    // List of static file patterns that will require authorization.
    // File paths are relative to the RootPath property and pattern matching is case-insensitive.
    // Pattern can include wildcards or question marks. For example: *.html, /user/*, etc
    // 
    "AutorizePaths": [],
    "UnauthorizedRedirectPath": "/",
    "UnautorizedReturnToQueryParameter": "return_to",


## Version [2.26.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.26.0) (2025-05-11)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.26.0...2.25.0)

### Core NpgsqlRest Library

#### Upload Handlers Improvements

##### CSV Upload Handler

New CSV Upload Handler with key `csv` is implemented and added to available handlers (Large Object and File System). It has following parameters:

- `included_mime_types`: List of allowed MIME type patterns. Pattern may include `*` or `?`. If empty or null, all MIME types are allowed. Default is null, this parameter is not used.
- `excluded_mime_types`: List of disallowed MIME type patterns. Pattern may include `*` or `?`. Default is null, this parameter is not used.
- `check_file`: Boolean flag indicating whether to perform content verification. Content verification looks for binary file markers in test buffer and does it contain new line delimiters. Default is true.
- `test_buffer_size`: Size of the buffer to read for content verification. Default is 8192 bytes.
- `non_printable_threshold`: Maximum number of non-printable characters allowed in the test buffer. Everything above is considered likely binary content. Default is 5.
- `delimiters`: Text where each character is considered as valid value delimiter. Default is comma character (`,`). Note: use standard escape sequence special characters like `\t` for tab character.
- `has_fields_enclosed_in_quotes`: Boolean flag indicating whether CSV fields might be enclosed in quotes. Default is true.
- `set_white_space_to_null`: Boolean flag indicating whether whitespace-only values should be converted to NULL when calling the row command. Default is true.
- `row_command`: The SQL command to execute for each CSV row. Required when parsing CSV content. Default is `call process_csv_row($1,$2,$3,$4)`

Row command is executed under transaction for every CSV row. It accepts 4 optional and positional parameters:
1) Parameter `$1` is always row number, type `integer`, starting from 1. If the CSV contains a header row, it will always be row number 1.
2) Parameter `$2` is always text array (type `text[]`) that contains entire row being processed.
3) Parameter `$3` is always the single result from the execution of the previous row command. Type is whatever type the row command returns. If the row command doesn't return anything, this parameter will always be NULL. If the row command does return value, this parameter will always be NULL in the first row.
4) Parameter `$4` is the JSON object containing metadata for this upload containing following fields:
- `type`: contains upload type key, default is `csv`
- `fileName`: upload file name
- `contentType`: upload mime type
- `size`: upload size in bytes
- `status`: result of content verification. It can be `Ok`, `Empty`, `ProbablyBinary` or `NoNewLines`, but in the row command context it is always `Ok` (even when verification is not used).

Same metadata JSON is sent to the main endpoint command parameter marked as upload metadata, with some differences:
1) Metadata is array of objects.
2) Field `status` can be any allowed value (`Ok`, `Empty`, `ProbablyBinary` or `NoNewLines`).
3) Contains one extra field: `lastResult`. This field will contain value of the last row command (if any).

Example:

```sql
-- table for uploads
create table csv_example_upload_table (
    index int,
    id int,
    name text,
    value int
);

-- row command
create procedure csv_example_upload_table_row(
    _index int,
    _row text[]
)
language plpgsql
as 
$$
begin
    insert into csv_example_upload_table (
        index,
        id, 
        name, 
        value
    ) 
    values (
        _index,
        _row[1]::int,
        _row[2],
        _row[3]::int
    );
end;
$$;

-- HTTP POST endpoint
create function csv_example_upload(
    _meta json = null
)
returns json 
language plpgsql
as 
$$
begin
    -- do something with metadata or raise exception to rollback this upload
    return _meta;
end;
$$;

comment on function csv_example_upload(json) is '
HTTP POST
upload for csv
param _meta is upload metadata
delimiters = ,;
row_command = call csv_example_upload_table_row($1,$2)
';
```

And now we can upload CSV using this upload endpoint:

```csharp
var fileName = "example-csv-upload.csv";
var sb = new StringBuilder();
sb.AppendLine("11,XXX,333");
sb.AppendLine("12;YYY;666");
sb.AppendLine("13;;999");
sb.AppendLine("14,,,");
var csvContent = sb.ToString();
var contentBytes = Encoding.UTF8.GetBytes(csvContent);
using var formData = new MultipartFormDataContent();
using var byteContent = new ByteArrayContent(contentBytes);
byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
formData.Add(byteContent, "file", fileName);

using var result = await test.Client.PostAsync("/api/csv-example-upload/", formData);
result.StatusCode.Should().Be(HttpStatusCode.OK);
```

This CSV upload uses two delimiters: `,` and `;`. Resulting table `csv_example_upload_table` after uploading content from this example will look like this:

| index | id | name | value |
| ----- | -- | ---- | ---- |
| 1 | 11 | XXX | 333 |
| 2 | 12 | YYY | 666 |
| 3 | 13 | NULL | 999 |
| 4 | 14 | NULL | NULL |

See the Test projects for more examples.

##### New Upload Parameters

All upload handlers now support additional common parameters:

- `included_mime_types`: List of allowed MIME type patterns. Pattern may include `*` or `?`. If empty or null, all MIME types are allowed. Default is null, this parameter is not used.
- `excluded_mime_types`: List of disallowed MIME type patterns. Pattern may include `*` or `?`. Default is null, this parameter is not used.

#### New Custom Parameters

New custom parameters were added to support existing functionalities. These values can be set with old comment annotation system. These are:

- Buffer rows parameters. Accept number.
`bufferrows`
`buffer_rows`
`buffer-rows`
`buffer`

- Raw parameters. Accept boolean.
`raw`
`rawmode`
`raw_mode`
`raw-mode`
`rawresults`
`raw_results`
`raw-results`

- Separator parameters. Accept text.
`separator`
`rawseparator`
`raw_separator`
`raw-separator`

- New line parameters. Accept text.
`newline`
`new_line`
`new-line`
`rawnewline`
`raw_new_line`
`raw-new-line`

- Column names parameters. Accept boolean.
`columnnames`
`column_names`
`column-names`

- Connection name parameters. Accept text.
`connection`
`connectionname`
`connection_name`
`connection-name`

#### Improved Error Handling

Added two more entries to `PostgreSqlErrorCodeToHttpStatusCodeMapping` options:

```csharp
public Dictionary<string, int> PostgreSqlErrorCodeToHttpStatusCodeMapping { get; set; } = new()
{
    { "57014", 205 }, //query_canceled -> 205 Reset Content
    { "P0001", 400 }, // raise_exception -> 400 Bad Request
    { "P0004", 400 }, // assert_failure -> 400 Bad Request
};
```

This configuration is now returning 400 Bad Request response on:
- Custom Pl/pgSQL exceptions (raise exception)
- Custom Pl/pgSQL asserts (assert failure)

Also, if endpoint is returning 400 Bad Request due the exception (raise exception, assert failure), the response string will contain a full message (exception message or assertion message) without trailing error code.

#### Fixes

- Fixed bug with wrong parameter default value detection when using stored procedures.
- Fixes on upload handlers: 
  - improved upload rollback and cleanup on error
  - fixed handling of multiple uploads
  - fixed upload logging
  - fixed issue with metadata parameter when all parameters using default value

### NpgsqlRest Client App

#### Fixes

- Fixed `CacheParsedFile` option in `StaticFiles.ParseContentOptions` client configuration. Previously, it was always set to true, even if set to false in the configuration.

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

---

## Version [2.24.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.24.0) (2025-04-29)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.24.0...2.23.0)

### Core NpgsqlRest Library

- Added support for callback command when password verification fails on login.

### NpgsqlRest Client App

- Added support for callback command when password verification fails on login.
- Fixed data parameter for external login to send actual retrieved data.
- Fixed Docker image to have valid certificate.

---

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

When the