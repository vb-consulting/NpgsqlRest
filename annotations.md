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

Note: to be able to use this feature authentication needs to be enabled and configured in your application, for example:

```csharp
                                                               
var builder = WebApplication.CreateBuilder(args);              
builder.Services.AddAuthentication().AddCookie();              
                                                               
```

The login-enabled endpoints must return a named record.

The program will result in the `ArgumentException` exception if the login-enabled routine is either of these:
- void
- returns simple value
- returns set of record unnamed records

The login operation will be interpreted as an unsuccessful login attempt and return the status code `401 Unauthorized` without creating a user identity if either:
- The routine returns an empty record.
- The returned record includes a [status column](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname), and the value of the status column name is:
  - False for boolean types.
  - Not 200 for numeric types.

The login operation will be interpreted as a successful login attempt and return the status code `200 OK` with creating a new user identity if either:
- The routine returns a record without status [status column](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname).
- The returned record includes a [status column](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname), and the value of the status column name is:
  - True for boolean types.
  - 200 for numeric types.

To authorize a different authorization scheme, return a [schema column name](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemacolumnname) with the value of that schema.

Any other records will be set as new claims for the created user identity on successful login, where:
- The column name is claim type. This type will by default try to match the constant name in the [ClaimTypes class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0) to retrieve the value. Use the [`UseActiveDirectoryFederationServicesClaimTypes` Option][https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseactivedirectoryfederationservicesclaimtypes] to control this behavior.
- The record value (as string) is the claim value.

## Logout

```console
logout
signout
```

This annotation will transform the routine into the endpoint that performs the logout or the sign-out operation.

If the routine doesn't return any data, the default authorization scheme is signed out. Any values returned will be interpreted as scheme names (converted to string) to sign out.