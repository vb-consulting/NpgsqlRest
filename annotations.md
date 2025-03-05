# Annotations Guide

## BufferRows

```console
                                                           
bufferrows number                                          
buffer_rows number                                         
buffer-rows number                                         
buffer number                                              
                                                           
```

Sets the buffered amount of rows before they are written to the response for this endpoint.

## Tags

```console
                                                           
for tag1, tag2, tag3 [, ...]                               
tag tag1, tag2, tag3 [, ...]                               
tags tag1, tag2, tag3 [, ...]                              
                                                           
```

All annotations in lines below this line apply only to tags in the argument list.

## Disabled

```console
                                                           
disabled                                                   
disabled [ tag1, tag2, tag3 [, ...] ]                      
                                                           
```

The endpoint is disabled. Optional tag list disabled only for tags in the argument list.

## Enabled

```console
                                                           
enabled                                                    
enabled [ tag1, tag2, tag3 [, ...] ]                       
                                                           
```

The endpoint is enabled. Optional tag list enabled only for tags in the argument list.

## HTTP

```console
                                                           
HTTP                                                       
HTTP [ GET | POST | PUT | DELETE ]                         
HTTP [ GET | POST | PUT | DELETE ] path                    
                                                           
```

HTTP settings:
- Use HTTP annotation to enable when running in `CommentsMode.OnlyWithHttpTag` option.
- Change the HTTP method with the optional second argument.
- Change the HTTP path with the optional third argument.

## RequestParamType

```console
requestparamtype [ [ querystring | query_string | query-string | query ]   |   [ bodyjson | body_json | body-json | json | body ] ]
paramtype  [ [ querystring | query_string | query-string | query ]   |   [ bodyjson | body_json | body-json | json | body ] ]
request_param_type  [ [ querystring | query_string | query-string | query ]   |   [ bodyjson | body_json | body-json | json | body ] ]
param_type  [ [ querystring | query_string | query-string | query ]   |   [ bodyjson | body_json | body-json | json | body ] ]
request-param-type  [ [ querystring | query_string | query-string | query ]   |   [ bodyjson | body_json | body-json | json | body ] ]
param-type  [ [ querystring | query_string | query-string | query ]   |   [ bodyjson | body_json | body-json | json | body ] ]
```

Set how request parameters are sent - query string or JSON body.

## RequiresAuthorization

```console
                                                           
requiresauthorization                                      
authorize                                                  
requires_authorization                                     
requires-authorization                                     
requiresauthorization [ role1, role2, role2 [, ...] ]      
authorize [ role1, role2, role2 [, ...] ]                  
requires_authorization [ role1, role2, role2 [, ...] ]     
requires-authorization [ role1, role2, role2 [, ...] ]     
                                                           
```

Require authorization for this endpoint.

- If the user is not authorized and authorization is required, the endpoint will return the status code `401 Unauthorized`.
- If the user is authorized but not in any of the roles required by the authorization, the endpoint will return the status code `403 Forbidden`.
  
## AllowAnonymous

```console
                                                             
allowanonymous                                               
allow_anonymous                                              
allow-anonymous                                              
anonymous                                                    
anon                                                         
                                                             
```

Allow anonymous access with no authorization to this endpoint. 

## CommandTimeout

```console
                                                             
commandtimeout seconds                                       
command_timeout seconds                                      
command-timeout seconds                                      
timeout seconds                                              
                                                             
```

Set the command execution timeout in seconds.

## RequestHeadersMode

```console
                                                             
requestheadersmode [ ignore | context | parameter ]          
request_headers_mode [ ignore | context | parameter ]        
request-headers-mode [ ignore | context | parameter ]        
requestheaders [ ignore | context | parameter ]              
request_headers [ ignore | context | parameter ]             
request-headers [ ignore | context | parameter ]             
                                                             
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

## ResponseNullHandling

```console
                                                
responsenullhandling [ emptystring | nullliteral | nocontent ]         
response_null_handling [ emptystring | nullliteral | nocontent ]       
response-null-handling [ emptystring | nullliteral | nocontent ]       
                                                
```

Sets the response NULL handling option for a single function results other than JSON (text, number, etc...):

- `EmptyString`: Empty content response. This is the default.
- `NullLiteral`: Content is the `null` string.
- `NoContent`: Response is `HTTP 204 No Content` status response code.

## QueryStringNullHandling

```console
                                                
querystringnullhandling [ emptystring | nullliteral | ignore ]        
query_string_null_handling [ emptystring | nullliteral | ignore ]     
query-string-null-handling [ emptystring | nullliteral | ignore ]     
                                                
```

Sets the response NULL handling option for the query string parameters:

- `EmptyString`: Empty query string is interpreted as the NULL value parameter.
- `NullLiteral`: The literal value `null` (case insensitive) in the query string is interpreted as the NULL value parameter.
- `Ignore`: Doesn't handle NULL parameters in query strings. This is the default.

## Headers

```console
                                                
key: value                                      
                                                
```

Any line containing a`:` character is interpreted as the request header key and value where the key is the left side and the value is the right side string. For example: `Content-Type: text/html`

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

## Raw

```console
                                                
raw                                             
                                                
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

## Separator

```console
                                                
separator [ separator_value ]                   
                                                
```

Defines a standard separator between raw values. It only applies when `raw` is on.

## NewLine

```console
                                                
newline [ newline ]                   
                                                
```

Defines a standard separator between raw value columns. It only applies when `raw` is on.

## ColumnNames

```console
                                                
columnnames                                     
column_names                                    
column-names                                    
                                                
```

If this option is set to true - and if the endpoint is int the "raw" mode - the endpoint will contain a header names. If separators are applied, they will be used also.

## Cached

```console
                                                
cached                                          
cached [ param1, param2, param3 [, ...] ]       
cached [ param1 param2 param3 [...] ]           
                                                
```

If the routine returns a single value of any type, result will be cached in memory and retrieved from memory on next call. Use the optional list of parameter names (original or converted) to be used as additional cache keys.


## CacheExpiresIn

```
                                                
cacheexpires [ time_span_value ]                
cacheexpiresin [ time_span_value ]              
cache-expires [ time_span_value ]               
cache-expires-in [ time_span_value ]            
cache_expires [ time_span_value ]               
cache_expires_in [ time_span_value ]            
                                                
```

Sets the cache expiration time window if the routine was cached. 

Value is a simplified PostgreSQL interval value, for example `10s` or `10sec` for 10 seconds, `5d` is for 5 days an so on. For example `3h`, `3hours`, `3 h` and `3 hours` are the same. Valid abbreviations are:

| abbr | meaning |
| ---- | ------------------------------- |
| `s`, `sec`, `second` or `seconds` | value is expressed in seconds |
| `m`, `min`, `minute` or `minutes` | value is expressed in minutes |
| `h`, `hour`, `hours` | value is expressed in hours |
| `d`, `day`, `days` | value is expressed in days |

## ParseResponse

```
                                                
parse                                           
parseresponse                                   
parse_response                                  
parse-response                                  
                                                
```

Enable response parsing for this routine. Requires injectiong a default parser. See the `DefaultResponseParser` option.