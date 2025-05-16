# Annotations Guide

This guide describes all available annotations that can be used in PostgreSQL function or procedure comments to customize REST API behavior.

## AllowAnonymous

```console
allowanonymous
allow_anonymous
allow-anonymous
anonymous
anon
```

Allow anonymous access with no authorization to this endpoint.

## Authorize (RequiresAuthorization)

```console
requiresauthorization
authorize
requires_authorization
requires-authorization
requiresauthorization [role1, role2, role3 [, ...]]
authorize [role1, role2, role3 [, ...]]
requires_authorization [role1, role2, role3 [, ...]]
requires-authorization [role1, role2, role3 [, ...]]
```

Require authorization for this endpoint.

- If the user is not authorized and authorization is required, the endpoint will return the status code `401 Unauthorized`.
- If the user is authorized but not in any of the roles required by the authorization, the endpoint will return the status code `403 Forbidden`.

## BodyParameterName

```console
bodyparametername name
body-parameter-name name
body_parameter_name name
bodyparamname name
body-param-name name
body_param_name name
```

Set the name of the existing parameter which is sent as body content. When the `BodyParameterName` is set, all other parameters are sent by the query string.

## BufferRows

```console
bufferrows number
buffer_rows number
buffer-rows number
buffer number
```

Sets the buffered amount of rows before they are written to the response for this endpoint.

This value can also be set using custom parameters by setting number values to parameters with same name:
- `bufferrows`
- `buffer_rows`
- `buffer-rows`
- `buffer`

## Cached

```console
cached
cached [param1, param2, param3 [, ...]]
cached [param1 param2 param3 [...]]
```

If the routine returns a single value of any type, result will be cached in memory and retrieved from memory on next call. Use the optional list of parameter names (original or converted) to be used as additional cache keys.

## CacheExpiresIn

```console
cacheexpires [time_span_value]
cacheexpiresin [time_span_value]
cache-expires [time_span_value]
cache-expires-in [time_span_value]
cache_expires [time_span_value]
cache_expires_in [time_span_value]
```

Sets the cache expiration time window if the routine was cached.

Value is a simplified PostgreSQL interval value, for example `10s` or `10sec` for 10 seconds, `5d` is for 5 days and so on. For example `3h`, `3hours`, `3 h` and `3 hours` are the same. Valid abbreviations are:

| abbr | meaning |
| ---- | ------------------------------- |
| `s`, `sec`, `second` or `seconds` | value is expressed in seconds |
| `m`, `min`, `minute` or `minutes` | value is expressed in minutes |
| `h`, `hour`, `hours` | value is expressed in hours |
| `d`, `day`, `days` | value is expressed in days |

## ColumnNames

```console
columnnames
column_names
column-names
```

If this option is set to true - and if the endpoint is in the "raw" mode - the endpoint will contain header names. If separators are applied, they will be used also.

This value can also be set using custom parameters by setting true or false value to parameters with these names:
- `columnnames`
- `column_names`
- `column-names`

## CommandTimeout

```console
commandtimeout seconds
command_timeout seconds
command-timeout seconds
timeout seconds
```

Set the command execution timeout in seconds.

## ConnectionName

```console
connection [name]
connectionname [name]
connection_name [name]
connection-name [name]
```

Defines an alternative connection name. The name must be an existing key in a `ConnectionStrings` dictionary option. This is useful when some routines have to read from read-only replicas. However, the same routine also has to exist on the primary connection to be able to build the necessary metadata.

This value can also be set using custom parameters by setting text value to parameters with these names:
- `connection`
- `connectionname`
- `connection_name`
- `connection-name`

## Custom Parameters

```console
name = value
```

Any line containing `=` character is interpreted as the parameter name and value where the name is the left side and the value is the right side string. 

For example: `path = ./my_path`

To be custom parameter name, name part must consist of alphanumerics or `_`, `-` characters.

Custom parameters are used in different handlers that execute various code, depending on the request. Currently, only upload handlers are using custom parameters system:

- `large_object` upload handler have following parameters:

1) `oid`: set the custom `oid` number to be used. By default, new `oid` is assigned on each upload.
2) `buffer_size`: set the custom buffer size. Default is 8192 or 8K buffer size

- `file_system` upload handler have following parameters:

1) `path`: set the custom path for upload. Default is current path `./`
2) `file`: set the custom file name for upload. Default is whatever name is received in request.
3) `unique_name`: boolean field that, if true, will automatically set file name to be unique (name is GUID and extension is the same). Can only have true or false values (case insensitive). Default is true.
4) `create_path`: boolean field that, if true, will check if the path exists, and create it if it doesn't. Can only have true or false values (case insensitive). Default is false.
5) `buffer_size`: set the custom buffer size. Default is 8192 or 8K buffer size.

## Disabled

```console
disabled
disabled [tag1, tag2, tag3 [, ...]]
```

The endpoint is disabled. Optional tag list disabled only for tags in the argument list.

## Enabled

```console
enabled
enabled [tag1, tag2, tag3 [, ...]]
```

The endpoint is enabled. Optional tag list enabled only for tags in the argument list.

## HTTP

```console
HTTP
HTTP [GET | POST | PUT | DELETE]
HTTP [GET | POST | PUT | DELETE] path
HTTP path
```

HTTP settings:
- Use HTTP annotation to enable when running in `CommentsMode.OnlyWithHttpTag` option.
- Change the HTTP method with the optional second argument.
- Change the HTTP path with the optional third argument.
- Change the HTTP path with second argument if the second argument doesn't match any valid VERB (GET, POST, etc).

## Login

```console
login
signin
```

This annotation will transform the routine into the authentication endpoint that performs the sign-in operation.

See more information on how the login endpoints work on the [login endpoints documentation page](https://vb-consulting.github.io/npgsqlrest/login-endpoints).

## Logout

```console
logout
signout
```

This annotation will transform the routine into the endpoint that performs the logout or the sign-out operation.

If the routine doesn't return any data, the default authorization scheme is signed out. Any values returned will be interpreted as scheme names (converted to string) to sign out.

For more information on the login and the logout see the [login endpoints documentation page](https://vb-consulting.github.io/npgsqlrest/login-endpoints).

## NewLine

```console
newline [newline_value]
new_line [newline_value]
new-line [newline_value]
rawnewline [newline_value]
raw_new_line [newline_value]
raw-new-line [newline_value]
```

Defines a standard separator between raw value rows. It only applies when `raw` is on.

This value can also be set using custom parameters by setting text value to parameters with these names:
- `newline`
- `new_line`
- `new-line`
- `rawnewline`
- `raw_new_line`
- `raw-new-line`

## Parameter Hash

```console
param param_name1 is hash of param_name2
```

Hashes value of the `param_name1` with the value of `param_name2` parameter by using default hasher.

## Parameter Upload Metadata

```console
param param_name is upload metadata
upload param_name as metadata
```

Set the upload metadata parameter name.

## ParseResponse

```console
parse
parseresponse
parse_response
parse-response
```

Enable response parsing for this routine. Requires injecting a default parser. See the `DefaultResponseParser` option.

## Path

```console
PATH path
```

Sets the custom HTTP path.

## QueryStringNullHandling

```console
querystringnullhandling [emptystring | nullliteral | ignore]
query_string_null_handling [emptystring | nullliteral | ignore]
query-string-null-handling [emptystring | nullliteral | ignore]
```

Sets the response NULL handling option for the query string parameters:

- `EmptyString`: Empty query string is interpreted as the NULL value parameter.
- `NullLiteral`: The literal value `null` (case insensitive) in the query string is interpreted as the NULL value parameter.
- `Ignore`: Doesn't handle NULL parameters in query strings. This is the default.

## Raw

```console
raw
rawmode
raw_mode
raw-mode
rawresults
raw_results
raw-results
```

Sets response to a "raw" mode. HTTP response is written exactly as it is received from PostgreSQL (raw mode).

This is useful for creating CSV responses automatically. For example:

```sql
create function raw_csv_response1() 
returns setof text
language sql
as 
$$
select trim(both '()' FROM sub::text) || E'\n' from (
values 
    (123, '2024-01-01'::timestamp, true, 'some text'),
    (456, '2024-12-31'::timestamp, false, 'another text')
)
sub (n, d, b, t)
$$;
comment on function raw_csv_response1() is '
raw
Content-Type: text/csv
';
```

Produces the following response:

```
HTTP/1.1 200 OK                                                  
Connection: close
Content-Type: text/csv
Date: Tue, 08 Aug 2024 14:25:26 GMT
Server: Kestrel
Transfer-Encoding: chunked

123,"2024-01-01 00:00:00",t,"some text"
456,"2024-12-31 00:00:00",f,"another text"
```

This value can also be set using custom parameters by setting true or false value to parameters with these names:
- `raw`
- `rawmode`
- `raw_mode`
- `raw-mode`
- `rawresults`
- `raw_results`
- `raw-results`

## RequestHeadersMode

```console
requestheadersmode [ignore | context | parameter]
request_headers_mode [ignore | context | parameter]
request-headers-mode [ignore | context | parameter]
requestheaders [ignore | context | parameter]
request_headers [ignore | context | parameter]
request-headers [ignore | context | parameter]
```

Set how request parameters are handled:
- Ignore: do not send request headers.
- Context: Request headers are set as the session variable under `request.headers` key.
- Parameter: Request headers are set as a default parameter defined with `RequestHeadersParameterName`.

## RequestHeadersParameterName

```console
requestheadersparametername name
requestheadersparamname name
request_headers_parameter_name name
request_headers_param_name name
request-headers-parameter-name name
request-headers-param-name name
```

When `RequestHeadersMode` is set to send request headers as a parameter, set the existing parameter name. The default is `headers`.

## RequestParamType

```console
requestparamtype [[querystring | query_string | query-string | query] | [bodyjson | body_json | body-json | json | body]]
paramtype [[querystring | query_string | query-string | query] | [bodyjson | body_json | body-json | json | body]]
request_param_type [[querystring | query_string | query-string | query] | [bodyjson | body_json | body-json | json | body]]
param_type [[querystring | query_string | query-string | query] | [bodyjson | body_json | body-json | json | body]]
request-param-type [[querystring | query_string | query-string | query] | [bodyjson | body_json | body-json | json | body]]
param-type [[querystring | query_string | query-string | query] | [bodyjson | body_json | body-json | json | body]]
```

Set how request parameters are sent - query string or JSON body.

## Response Headers

```console
key: value
```

Any line containing `:` character is interpreted as the request header key and value where the key is the left side and the value is the right side string. For example: `Content-Type: text/html`

To be valid header key, key part must consist of alphanumerics or `_`, `-` characters.

## ResponseNullHandling

```console
responsenullhandling [emptystring | nullliteral | nocontent]
response_null_handling [emptystring | nullliteral | nocontent]
response-null-handling [emptystring | nullliteral | nocontent]
```

Sets the response NULL handling option for a single function results other than JSON (text, number, etc...):

- `EmptyString`: Empty content response. This is the default.
- `NullLiteral`: Content is the `null` string.
- `NoContent`: Response is `HTTP 204 No Content` status response code.

## SecuritySensitive

```console
securitysensitive
sensitive
security
security_sensitive
security-sensitive
```

Marks the endpoint as security sensitive which will obfuscate any parameters before sending it to log.

## Separator

```console
separator [separator_value]
rawseparator [separator_value]
raw_separator [separator_value]
raw-separator [separator_value]
```

Defines a standard separator between raw value columns. It only applies when `raw` is on.

This value can also be set using custom parameters by setting text value to parameters with these names:
- `separator`
- `rawseparator`
- `raw_separator`
- `raw-separator`

## Tags

```console
for tag1, tag2, tag3 [, ...]
tag tag1, tag2, tag3 [, ...]
tags tag1, tag2, tag3 [, ...]
```

All annotations in lines below this line apply only to tags in the argument list.

## Upload

```console
upload
upload for handler_name1, handler_name2 [, ...]
upload param_name as metadata
```

Marks routines as Upload endpoint.

Optionally, set handler name (or multiple handlers names) - or set the upload metadata parameter name.

## UserContext

```console
usercontext
user_context
user-context
```

Enables user context for this endpoint.

## UserParameters

```console
userparameters
userparams
user_parameters
user_params
user-parameters
user-params
```

Enables user parameters for this endpoint.

# Tags

Tags are applied by different routine sources that can generate a valid endpoint. They are used with following comment annotations:

- Tag: `for tag1, tag2, tag3 [, ...]`
- Enabled: `enabled [tag1, tag2, tag3 [, ...]]`
- Disabled: `disabled [tag1, tag2, tag3 [, ...]]`

## Function or Procedure Source

This source generates tags based on routine volatility:

- `volatile`
- `stable`
- `immutable`
- `other`

## Table or View CRUD Source

### Select Routines

- `select`
- `read`
- `get`

### Update Returning Routines

- `update`
- `post`
- `updatereturning`
- `update-returning`
- `update_returning`
- `returning`

### Delete Routines

- `delete`

### Delete Returning Routines

- `delete`
- `deletereturning`
- `delete-returning`
- `delete_returning`
- `returning`

### Insert Routines

- `insert`
- `put`
- `create`

### Insert On Conflict Do Nothing Routines

- `insert`
- `put`
- `create`
- `insertonconflictdonothing`
- `insert-on-conflict-do-nothing`
- `insert_on_conflict_do_nothing`
- `onconflictdonothing`
- `on-conflict-do-nothing`
- `on_conflict_do_nothing`
- `onconflict`
- `on_conflict`
- `on-conflict`

### Insert On Conflict Do Nothing Returning Routines

- `insert`
- `put`
- `create`
- `insertonconflictdonothingreturning`
- `insert-on-conflict-do-nothing-returning`
- `insert_on_conflict_do_nothing-returning`
- `onconflictdonothing`
- `on-conflict-do-nothing`
- `on_conflict_do_nothing`
- `onconflict`
- `on_conflict`
- `on-conflict`
- `returning`

### Insert On Conflict Do Update Routines

- `insert`
- `put`
- `create`
- `insertonconflictdoupdate`
- `insert-on-conflict-do-update`
- `insert_on_conflict_do_update`
- `onconflictdoupdate`
- `onconflict`
- `on_conflict`
- `on-conflict`
- `on-conflict-do-update`
- `on_conflict_do_update`

### Insert On Conflict Do Update Returning Routines

- `insert`
- `put`
- `create`
- `insertonconflictdoupdatereturning`
- `insert-on-conflict-do-update-returning`
- `insert_on_conflict_do_update_returning`
- `onconflictdoupdate`
- `on-conflict-do-update`
- `on_conflict_do_update`
- `onconflict`
- `on_conflict`
- `on-conflict`
- `returning`