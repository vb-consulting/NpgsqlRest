# Changelog

Note: The changelog for the older version can be found here: [Changelog Archive](https://github.com/vb-consulting/NpgsqlRest/blob/master/changelog-old.md)

---

## Version [2.28.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.28.0) (2025-06-12)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.27.0...2.28.0)

### Core Library

#### Improved error handling.

- All errors are handled properly now. Non-PostgreSQL errors will return 501 and all relevant data will be sent to the default logger.

#### Big Improvements in Upload Handlers

- All Upload Handlers will always return a JSON metadata which will be array of objects. Each object have following signature:

```ts
interface UploadMetadata {
  type: string; // handler key: large_object, file_system or csv
  fileName: string;
  contentType: string;
  size: number;
  success: boolean; // will be true for only for status=Ok
  status: string; // Empty, ProbablyBinary, InvalidImage, InvalidFormat, NoNewLines, InvalidMimeType, Ignored, Ok
  [key: string]: string | number | boolean; // depends on a handler type
}
```

There are 8 shared parameters:

- `stop_after_first_success`: Set to true to stop the upload after the first succesuful upload when multiple handlers are used. Subsequent files will be ignored (status: `Ignored`). Default is false.
- `included_mime_types`: CSV string containing mime type patters to include, set to null to ignore. Default is null.
- `excluded_mime_types`: CSV string containing mime type patters to exclude, set to null to ignore. Default is null.
- `buffer_size`: Size of a buffer in bytes when uploading a raw content to handlers like `large_object` and `file_system`. Default is null.
- `check_text`: Boolean value to check is a file a valid textual file or a binary. Set to true to accpet only text file. Default is false.
- `check_image`: Boolean value to check is a file a valid image. Set to true to accpet only images. It can also be a CSV text with allowed image types, such as `jpg`, `png`, `gif`, `bmp`, `tiff` or `webp`. Default is false.
- `test_buffer_size`: Buffer size in bytes when checking text files. Default is 4096.
- `non_printable_threshold`: Maximum count of non prinatble charactes allowed in test buffer to consider a valid text file. Default is 5.

Upload handler `large_object` accepts the following parameters:

- `stop_after_first_success`
- `included_mime_types`
- `excluded_mime_types`
- `buffer_size`
- `oid`
- `check_text`
- `check_image`
- `test_buffer_size`
- `non_printable_threshold`
- `large_object_included_mime_types`
- `large_object_excluded_mime_types`
- `large_object_buffer_size`
- `large_object_oid`
- `large_object_check_text`
- `large_object_check_image`
- `large_object_test_buffer_size`
- `large_object_non_printable_threshold`

Upload handler `file_system` accepts the following parameters:

- `stop_after_first_success`
- `included_mime_types`
- `excluded_mime_types`
- `buffer_size`
- `path`
- `file`
- `unique_name`
- `create_path`
- `check_text`
- `check_image`
- `test_buffer_size`
- `non_printable_threshold`
- `file_system_included_mime_types`
- `file_system_excluded_mime_types`
- `file_system_buffer_size`
- `file_system_path`
- `file_system_file`
- `file_system_unique_name`
- `file_system_create_path`
- `file_system_check_text`
- `file_system_check_image`
- `file_system_test_buffer_size`
- `file_system_non_printable_threshold`

Upload handler `csv` accepts the following parameters:

- `stop_after_first_success`
- `included_mime_types`
- `excluded_mime_types`
- `check_format`
- `test_buffer_size`
- `non_printable_threshold`
- `delimiters`
- `has_fields_enclosed_in_quotes`
- `set_white_space_to_null`
- `row_command`
- `csv_included_mime_types`
- `csv_excluded_mime_types`
- `csv_check_format`
- `csv_test_buffer_size`
- `csv_non_printable_threshold`
- `csv_delimiters`
- `csv_has_fields_enclosed_in_quotes`
- `csv_set_white_space_to_null`
- `csv_row_command`

### NpgsqlRest Client

#### Added UseCryptographicAlgorithms option to DataProtection

```jsonc
{
  // ...

  //
  // Data protection settings. Encryption keys for Auth Cookies and Antiforgery tokens.
  //
  "DataProtection": {

    // ...

    //
    // When disabled, data protection keys will be stored in an unencrypted form
    //
    "UseCryptographicAlgorithms": {
      "Enabled": false,
      // AES_128_CBC, AES_192_CBC, AES_256_CBC, AES_128_GCM, AES_192_GCM, AES_256_GCM
      "EncryptionAlgorithm": "AES_256_CBC",
      // HMACSHA256, HMACSHA512
      "ValidationAlgorithm": "HMACSHA256"
    }
  },

  // ...
}
```

#### Improved CORS Handling 

- Two more options: `"AllowCredentials": true` and `"PreflightMaxAgeSeconds": 600`

#### Excel Upload Handler 

Excel Upload Handler Implemented only in the Client Application. It is using a lightwieght ExcelDataReader library to handle Excel files.

It can accept any of these parameters:

- `stop_after_first_success`
- `included_mime_types`
- `excluded_mime_types`
- `sheet_name`
- `all_sheets`
- `time_format`
- `date_format`
- `datetime_format`
- `row_is_json`
- `row_command`
- `excel_included_mime_types`
- `excel_excluded_mime_types`
- `excel_sheet_name`
- `excel_all_sheets`
- `excel_time_format`
- `excel_date_format`
- `excel_datetime_format`
- `excel_row_is_json`
- `excel_row_command`

#### New Upload Options

- Defualt configuration now looks like this:

```jsonc
{
  // ...

  //
  // Data protection settings. Encryption keys for Auth Cookies and Antiforgery tokens.
  //
  "NpgsqlRest": {

    // ...

    "UploadOptions": {
      "Enabled": false,
      "LogUploadEvent": true,
      "LogUploadParameters": false,
      //
      // Handler that will be used when upload handler or handlers are not specified.
      //
      "DefaultUploadHandler": "large_object",
      //
      // Gets or sets a value indicating whether the default upload metadata parameter should be used.
      //
      "UseDefaultUploadMetadataParameter": false,
      //
      // Name of the default upload metadata parameter. This parameter is used to pass metadata to the upload handler. The metadata is passed as a JSON object.
      //
      "DefaultUploadMetadataParameterName": "_upload_metadata",
      //
      // Gets or sets a value indicating whether the default upload metadata context key should be used.
      //
      "UseDefaultUploadMetadataContextKey": false,
      //
      // Name of the default upload metadata context key. This key is used to pass the metadata to the upload handler. The metadata is passed as a JSON object.
      //
      "DefaultUploadMetadataContextKey": "request.upload_metadata",
      //
      // Upload handlers specific settings.
      //
      "UploadHandlers": {
        //
        // General settings for all upload handlers
        //
        "StopAfterFirstSuccess": false,
        // csv string containing mime type patters, set to null to ignore
        "IncludedMimeTypePatterns": null,
        // csv string containing mime type patters, set to null to ignore
        "ExcludedMimeTypePatterns": null,
        "BufferSize": 8192, // Buffer size for the upload handlers file_system and large_object, in bytes. Default is 8192 bytes (8 KB).
        "TextTestBufferSize": 4096, // Buffer sample size for testing textual content, in bytes. Default is 4096 bytes (4 KB).
        "TextNonPrintableThreshold": 5, // Threshold for non-printable characters in the text buffer. Default is 5 non-printable characters.
        "AllowedImageTypes": "jpeg, png, gif, bmp, tiff, webp", // Comma-separated list of allowed image types when checking images".

        //
        // Enables upload handlers for the NpgsqlRest endpoints that uses PostgreSQL Large Objects API
        //
        "LargeObjectEnabled": true,
        "LargeObjectKey": "large_object",
        "LargeObjectCheckText": false,
        "LargeObjectCheckImage": false,

        //
        // Enables upload handlers for the NpgsqlRest endpoints that uses file system
        //
        "FileSystemEnabled": true,
        "FileSystemKey": "file_system",
        "FileSystemPath": "/tmp/uploads",
        "FileSystemUseUniqueFileName": true,
        "FileSystemCreatePathIfNotExists": true,
        "FileSystemCheckText": false,
        "FileSystemCheckImage": false,

        //
        // Enables upload handlers for the NpgsqlRest endpoints that uploads CSV files to a row command
        //
        "CsvUploadEnabled": true,
        "CsvUploadCheckFileStatus": true,
        "CsvUploadDelimiterChars": ",",
        "CsvUploadHasFieldsEnclosedInQuotes": true,
        "CsvUploadSetWhiteSpaceToNull": true,
        //
        // $1 - row index (1-based), $2 - parsed value text array, $3 - result of previous row command, $4 - json metadata for upload
        //
        "CsvUploadRowCommand": "call process_csv_row($1,$2,$3,$4)",

        //
        // Enables upload handlers for the NpgsqlRest endpoints that uploads Excel files to a row command
        //
        "ExcelUploadEnabled": true,
        "ExcelKey": "excel",
        "ExcelSheetName": null, // null to use the first available
        "ExcelAllSheets": false,
        "ExcelTimeFormat": "HH:mm:ss",
        "ExcelDateFormat": "yyyy-MM-dd",
        "ExcelDateTimeFormat": "yyyy-MM-dd HH:mm:ss",
        "ExcelRowDataAsJson": false,
        //
        // $1 - row index (1-based), $2 - parsed value text array, $3 - result of previous row command, $4 - json metadata for upload
        //
        "ExcelUploadRowCommand": "call process_excel_row($1,$2,$3,$4)"
      }
    },

    // ...
  },

  // ...
}
```


## Version [2.27.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.27.0) (2025-05-19)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.26.0...2.27.0)

This is a big release that features several important changes.

### AuthenticationOptions Changes

Authentication options have been expanded to include new parameters. Specifically, to be able to bind user claims to parameters (user id, user name, user roles or custom) or to send them as PostgreSQL context.

Another important change is that beside `PasswordVerificationFailedCommand` option, there is also `PasswordVerificationSucceededCommand`. And both of these options contain a command expression that receives three optional text parameters:
- Authentication scheme used for the login
- User id used for the login.
- User name used for the login.

Parameters are positional: first is scheme, second is user id, and third is user name. User id and user name have switched places from the last version, since user id is usually more important.

Class `NpgsqlRestAuthenticationOptions` now looks like this:

```csharp
/// <summary>
/// Authentication options for the NpgsqlRest middleware.
/// </summary>
public class NpgsqlRestAuthenticationOptions
{
    /// <summary>
    /// Authentication type used with the Login endpoints to set the authentication type for the new `ClaimsIdentity` created by the login.
    ///
    /// This value must be set to non-null when using login endpoints, otherwise, the following error will raise: `SignInAsync when principal.Identity.IsAuthenticated is false is not allowed when AuthenticationOptions.RequireAuthenticatedSignIn is true.`
    /// 
    /// If the value is not set and the login endpoint is present, it will automatically get the database name from the connection string.
    /// </summary>
    public string? DefaultAuthenticationType { get; set; } = null;

    /// <summary>
    /// The default column name in the data reader which will be used to read the value to determine the success or failure of the login operation.
    /// 
    /// - If this column is not present, the success is when the endpoint returns any records.
    /// - If this column is present, it must be either a boolean to indicate success or a numeric value to indicate the HTTP Status Code to return.
    /// - If this column is present and retrieves a numeric value, that value is assigned to the HTTP Status Code and the login will authenticate only when this value is 200.
    /// </summary>
    public string? StatusColumnName { get; set; } = "status";

    /// <summary>
    /// The default column name in the data reader which will be used to read the value of the authentication scheme of the login process.
    /// 
    /// If this column is not present in the login response the default authentication scheme is used. Return new value to use a different authentication scheme with the login endpoint.
    /// </summary>
    public string? SchemeColumnName { get; set; } = "scheme";

    /// <summary>
    /// The default column name in the data reader which will return a text message with the login status.
    /// </summary>
    public string? MessageColumnName { get; set; } = "message";

    /// <summary>
    /// Any columns retrieved from the reader during login, which don't have a name in `StatusColumnName` or `SchemeColumnName` will be used to create a new identity  `Claim`:
    /// 
    /// Column name will be interpreted as the claim type and the associated reader value for that column will be the claim value.
    /// 
    /// When this value is set to true (default) column name will try to match the constant name in the [ClaimTypes class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0) to retrieve the value.
    /// 
    /// For example, column name `NameIdentifier` or `name_identifier` (when transformed by the default name transformer) will match the key `NameIdentifier` which translates to this: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
    /// </summary>
    public bool UseActiveDirectoryFederationServicesClaimTypes { get; set; } = true;

    /// <summary>
    /// Default claim type for user id.
    /// </summary>
    public string DefaultUserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    private string _defaultNameClaimType = ClaimTypes.Name;
    internal bool UsingDefaultNameClaimType = true;

    /// <summary>
    /// Default claim type for user name.
    /// </summary>
    public string DefaultNameClaimType
    {
        get => _defaultNameClaimType;
        set
        {
            _defaultNameClaimType = value;
            UsingDefaultNameClaimType = string.Equals(value, ClaimTypes.Name, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Default claim type for user roles.
    /// </summary>
    public string DefaultRoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>
    /// If true, return any response from auth endpoints (login and logout) if response hasn't been written by auth handler.
    /// For cookie auth, this will return full record to response as returned by the routine.
    /// For bearer token auth, this will be ignored because bearer token auth writes it's own response (with tokens).
    /// This option will also be ignored if message column is present (see MessageColumnName option).
    /// Default is false.
    /// </summary>
    public bool SerializeAuthEndpointsResponse { get; set; } = false;

    /// <summary>
    /// Don't write real parameter values when logging parameters from auth endpoints and obfuscate instead.
    /// This prevents user credentials including password to end up in application logs.
    /// Default is true.
    /// </summary>
    public bool ObfuscateAuthParameterLogValues { get; set; } = true;

    /// <summary>
    /// The default column name in the data reader which will be used to read the value of the hash of the password.
    /// If this column is present, the value will be used to verify the password from the password parameter.
    /// Password parameter is the first parameter which name contains the value of PasswordParameterNameContains.
    /// If verification fails, the login will fail and the HTTP Status Code will be set to 404 Not Found.
    /// </summary>
    public string HashColumnName { get; set; } = "hash";

    /// <summary>
    /// The default name of the password parameter. The first parameter which name contains this value will be used as the password parameter.
    /// This is used to verify the password from the password parameter when login endpoint returns a hash of the password (see HashColumnName).
    /// </summary>
    public string PasswordParameterNameContains { get; set; } = "pass";

    /// <summary>
    /// Default password hasher object. Inject custom password hasher object to add default password hasher.
    /// </summary>
    public IPasswordHasher? PasswordHasher { get; set; } = new PasswordHasher();

    /// <summary>
    /// Command that is executed when the password verification fails. There are three text parameters:
    ///     - authentication scheme used for the login (if exists)
    ///     - user id used for the login (if exists)
    ///     - user name used for the login (if exists)
    /// Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).
    /// </summary>
    public string? PasswordVerificationFailedCommand { get; set; } = null;

    /// <summary>
    /// Command that is executed when the password verification succeeds. There are three text parameters:
    ///     - authentication scheme used for the login (if exists)
    ///     - user id used for the login (if exists)
    ///     - user name used for the login (if exists)
    /// Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).
    /// </summary>
    public string? PasswordVerificationSucceededCommand { get; set; } = null;

    /// <summary>
    /// Set user context to true for all requests. 
    /// When this is set to true, user information (user id, user name and user roles) will be set to the context variables.
    /// You can set this individually for each request.
    /// </summary>
    public bool UseUserContext { get; set; } = false;

    /// <summary>
    /// User id context key that is used to set context variable for the user id.
    /// </summary>
    public string? UserIdContextKey { get; set; } = "request.user_id";

    /// <summary>
    /// User name context key that is used to set context variable for the user name.
    /// </summary>
    public string? UserNameContextKey { get; set; } = "request.user_name";

    /// <summary>
    /// User roles context key that is used to set context variable for the user roles.
    /// </summary>
    public string? UserRolesContextKey { get; set; } = "request.user_roles";

    /// <summary>
    /// IP address context key that is used to set context variable for the IP address.
    /// </summary>
    public string? IpAddressContextKey { get; set; } = "request.ip_address";

    /// <summary>
    /// When this value is set and user context is used, all user claims will be serialized to JSON value and set to the context variable with this name.
    /// </summary>
    public string? UserClaimsContextKey { get; set; } = null;

    /// <summary>
    /// Set user parameters to true for all requests. 
    /// When this is set to true, user information (user id, user name and user roles) will be set to the matching parameter names if available.
    /// You can set this individually for each request.
    /// </summary>
    public bool UseUserParameters { get; set; } = false;

    /// <summary>
    /// User id parameter name that is used to set parameter value for the user id.
    /// Parameter name can be original or converted and it has to be the text type.
    /// </summary>
    public string? UserIdParameterName { get; set; } = "_user_id";

    /// <summary>
    /// User name parameter name that is used to set parameter value for the user name.
    /// Parameter name can be original or converted and it has to be the text type.
    /// </summary>
    public string? UserNameParameterName { get; set; } = "_user_name";

    /// <summary>
    /// User roles parameter name that is used to set parameter value for the user roles.
    /// Parameter name can be original or converted and it has to be the text array type.
    /// </summary>
    public string? UserRolesParameterName { get; set; } = "_user_roles";

    /// <summary>
    /// IP address parameter name that is used to set parameter value for the IP address.
    /// Parameter name can be original or converted and it has to be the text type.
    /// </summary>
    public string? IpAddressParameterName { get; set; } = "_ip_address";

    /// <summary>
    /// All user claims will be serialized to JSON value and set to the parameter with this name.
    /// </summary>
    public string? UserClaimsParameterName { get; set; } = "_user_claims";
}
```

Previously, this logic was handled only in client application and inefficiently so. That means that there is a new configuration section in the client application for handling authentication:

```jsonc
{
  // ...
  "NpgsqlRest": {
    // ...

    //
    // Authentication options for NpgsqlRest endpoints
    //
    "AuthenticationOptions": {
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsdefaultauthenticationtype
      //
      "DefaultAuthenticationType": null,

      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsstatuscolumnname
      //
      "StatusColumnName": "status",

      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemecolumnname
      //
      "SchemeColumnName": "scheme",

      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsmessagecolumnname
      //
      "MessageColumnName": "message",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseactivedirectoryfederationservicesclaimtypes
      //
      "UseActiveDirectoryFederationServicesClaimTypes": true,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsdefaultuseridclaimtype
      //
      "DefaultUserIdClaimType": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsdefaultnameclaimtype
      //
      "DefaultNameClaimType": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsdefaultroleclaimtype
      //
      "DefaultRoleClaimType": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsserializeauthendpointsresponse
      //
      "SerializeAuthEndpointsResponse": false,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsobfuscateauthparameterlogvalues
      //
      "ObfuscateAuthParameterLogValues": true,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionshashcolumnname
      //
      "HashColumnName": "hash",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionspasswordparameternamecontains
      //
      "PasswordParameterNameContains": "pass",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionspasswordverificationfailedcommand
      //
      "PasswordVerificationFailedCommand": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionspasswordverificationsucceededcommand
      //
      "PasswordVerificationSucceededCommand": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseusercontext
      //
      "UseUserContext": false,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseridcontextkey
      //
      "UserIdContextKey": "request.user_id",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsusernamecontextkey
      //
      "UserNameContextKey": "request.user_name",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuserrolescontextkey
      //
      "UserRolesContextKey": "request.user_roles",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsipaddresscontextkey
      //
      "IpAddressContextKey": "request.ip_address",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuserclaimscontextkey
      //
      "UserClaimsContextKey": null,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseruserparameters
      //
      "UseUserParameters": false,
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseridparametername
      //
      "UserIdParameterName": "_user_id",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsusernameparametername
      //
      "UserNameParameterName": "_user_name",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuserrolesparametername
      //
      "UserRolesParameterName": "_user_roles",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsipaddressparametername
      //
      "IpAddressParameterName": "_ip_address",
      //
      // See https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuserclaimsparametername
      //
      "UserClaimsParameterName": "_user_claims",
      //
      // Url path that will be used for the login endpoint. If NULL, the login endpoint will not be created.
      // See more on login endpoints at https://vb-consulting.github.io/npgsqlrest/login-endpoints
      //
      "LoginPath": null,
      //
      // Url path that will be used for the logout endpoint. If NULL, the logout endpoint will not be created.
      // See more on logout endpoints at https://vb-consulting.github.io/npgsqlrest/annotations/#logout
      //
      "LogoutPath": null,
      //
      // Routines that have this values set to true, will parse the response before it is returned to the client by using name placeholders from this section.
      //
      // Use curly braces to define the name placeholders in the response. For example, {user_id} will be replaced. 
      // The replacement value in this example is the value that would be assigned to he UserIdParameterName parameter if UseUserParameters was used. That means user id.
      //
      "ParseResponseUsingUserParameters": false
    }

    // ...
}
```

### UploadOptions Changes

Upload options in core library have also been improved. New options include:
- `LogUploadEvent` - should we log upload events.
- `DefaultUploadMetadataParameterName` and `UseDefaultUploadMetadataParameter` - set the default upload metadata parameter once for all.
- `DefaultUploadMetadataContextKey` and `UseDefaultUploadMetadataContextKey` - set metadata with PostgreSQL context first instead of parameter.

Here is the new `NpgsqlRestUploadOptions` class:

```csharp
/// <summary>
/// Upload options for the NpgsqlRest middleware.
/// </summary>
public class NpgsqlRestUploadOptions
{
    public bool Enabled { get; set; } = true;
    public bool LogUploadEvent { get; set; } = true;

    /// <summary>
    /// Default upload handler name. This value is used when the upload handlers are not specified.
    /// </summary>
    public string DefaultUploadHandler { get; set; } = "large_object";

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
    public Dictionary<string, Func<ILogger?, IUploadHandler>>? UploadHandlers { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether the default upload metadata parameter should be used.
    /// </summary>
    public bool UseDefaultUploadMetadataParameter { get; set; } = false;

    /// <summary>
    /// Name of the default upload metadata parameter. 
    /// This parameter will be automatically assigned with the upload metadata JSON string when the upload is completed if UseDefaultUploadMetadataParameter is set to true.
    /// </summary>
    public string DefaultUploadMetadataParameterName { get; set; } = "_upload_metadata";

    /// <summary>
    /// Gets or sets a value indicating whether the default upload metadata context key should be used.
    /// </summary>
    public bool UseDefaultUploadMetadataContextKey { get; set; } = false;

    /// <summary>
    /// Name of the default upload metadata context key.
    /// This context key will be automatically assigned to context with the upload metadata JSON string when the upload is completed if UseDefaultUploadMetadataContextKey is set to true.
    /// </summary>
    public string DefaultUploadMetadataContextKey { get; set; } = "request.upload_metadata";
}
```

This also required change in client application configuration:

```jsonc
{
  // ...
  "NpgsqlRest": {
    // ...

    "UploadOptions": {
      "Enabled": true,
      "LogUploadEvent": true,
      //
      // Handler that will be used when upload handler or handlers are not specified.
      //
      "DefaultUploadHandler": "large_object",
      //
      // Gets or sets a value indicating whether the default upload metadata parameter should be used.
      //
      "UseDefaultUploadMetadataParameter": false,
      //
      // Name of the default upload metadata parameter. This parameter is used to pass metadata to the upload handler. The metadata is passed as a JSON object.
      //
      "DefaultUploadMetadataParameterName": "_upload_metadata",
      //
      // Gets or sets a value indicating whether the default upload metadata context key should be used.
      //
      "UseDefaultUploadMetadataContextKey": false,
      //
      // Name of the default upload metadata context key. This key is used to pass the metadata to the upload handler. The metadata is passed as a JSON object.
      //
      "DefaultUploadMetadataContextKey": "request.upload_metadata",
      //
      // Upload handlers specific settings.
      //
      "UploadHandlers": {
        //
        // Enables upload handlers for the NpgsqlRest endpoints that uses PostgreSQL Large Objects API
        //
        "LargeObjectEnabled": true,
        // csv string containing mime type patters, set to null to ignore
        "LargeObjectIncludedMimeTypePatterns": null,
        // csv string containing mime type patters, set to null to ignore
        "LargeObjectExcludedMimeTypePatterns": null,
        "LargeObjectKey": "large_object",
        "LargeObjectHandlerBufferSize": 8192,

        //
        // Enables upload handlers for the NpgsqlRest endpoints that uses file system
        //
        "FileSystemEnabled": true,
        // csv string containing mime type patters, set to null to ignore
        "FileSystemIncludedMimeTypePatterns": null,
        // csv string containing mime type patters, set to null to ignore
        "FileSystemExcludedMimeTypePatterns": null,
        "FileSystemKey": "file_system",
        "FileSystemHandlerPath": "/tmp/uploads",
        "FileSystemHandlerUseUniqueFileName": true,
        "FileSystemHandlerCreatePathIfNotExists": true,
        "FileSystemHandlerBufferSize": 8192,

        //
        // Enables upload handlers for the NpgsqlRest endpoints that uploads CSV files to a row command
        //
        "CsvUploadEnabled": true,
        // csv string containing mime type patters, set to null to ignore
        "CsvUploadIncludedMimeTypePatterns": null,
        // csv string containing mime type patters, set to null to ignore
        "CsvUploadExcludedMimeTypePatterns": null,
        "CsvUploadCheckFileStatus": true,
        "CsvUploadTestBufferSize": 4096,
        "CsvUploadNonPrintableThreshold": 5,
        "CsvUploadDelimiterChars": ",",
        "CsvUploadHasFieldsEnclosedInQuotes": true,
        "CsvUploadSetWhiteSpaceToNull": true,
        //
        // $1 - row index (1-based), $2 - parsed value text array, $3 - result of previous row command, $4 - json metadata for upload
        //
        "CsvUploadRowCommand": "call process_csv_row($1,$2,$3,$4)"
      }
    }

    // ...
}
```

### NpgsqlRest Client App Static File Parser

This is basically conceptual fix to the Client application static file parser. Files being parsed that are cached are defining the purpose so cache needs to be disabled. This is done with new static file configuration setting in the Client application:

```jsonc
{
    //...
    "StaticFiles": {
        //...
        "ParseContentOptions": {
            //...
            "Headers": [ "Cache-Control: no-store, no-cache, must-revalidate", "Pragma: no-cache", "Expires: 0" ],
        }
    }
    //...
}
```

### Comment Annotation System Changes

Comment annotation has seen some big changes. Added new parameters and annotations to reflect changes in Authentication and Upload options.

Breaking change: Comment annotations now support only snake_case naming. This is consistent with PostgreSQL naming convention and it reduces the number of annotations needed to support. 

### Fixes and Other Improvements

- Fixed a bug when endpoint was returning empty JSON array. Adequate parser wasn't invoked in case of JSON empty array and it returned `{}`. This is fixed to return `[]`.
- All commands and parameters are now using `MemberwiseClone` performance trick to reduce unnecessary allocations.

## Version [2.26.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.26.0) (2025-05-11)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.25.0...2.26.0)

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

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.24.0...2.25.0)

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

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.23.0...2.24.0)

### Core NpgsqlRest Library

- Added support for callback command when password verification fails on login.

### NpgsqlRest Client App

- Added support for callback command when password verification fails on login.
- Fixed data parameter for external login to send actual retrieved data.
- Fixed Docker image to have valid certificate.

---

## Version [2.23.0](https://github.com/vb-consulting/NpgsqlRest/tree/2.23.0) (2025-04-28)

[Full Changelog](https://github.com/vb-consulting/NpgsqlRest/compare/2.22.0...2.23.0)

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

- If verification fails, login will: 
  - Automatically return status 404 with no message.
  - Relevant information will be logged as a warning to default logger: `Password verification failed for attempted login: path={path} userId={userId}, username={userName}`.
  - If the option `AuthenticationOptions.PasswordVerificationFailedCommand` is defined, this command will be executed with following optional parameters:
    - 1) scheme (text)
    - 2) user name (text)
    - 3) user id (text)

- New `AuthenticationOptions` values to support this feature are these:

1) `AuthenticationOptions.HashColumnName`

    - Type: `string`
    - Default: `"hash"`

    The default column name in the data reader that will contain password hash. If this column is present, value will be verified with the default hasher against the password parameter.

2) `AuthenticationOptions.PasswordParameterNameContains`

- Type: `string`
- Default: `"pass"`

When the login endpoint tries to verify supplied hash with the password parameter, this value will be used to locate the password parameter from the parameter collection. That will be the first parameter that contains this string in parameter name (either original or translated). 

3) `AuthenticationOptions.PasswordVerificationFailedCommand`

Text command with three optional parameters that is executed when automatic hash verification fails. Executing this command gives you chance to update appswrod attempts in database for example.

Command accepts three optional and positional parameters:
1) scheme (text)
2) user name (text)
3) user id (text)

Execution will be skipped if this option is null or empty.

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