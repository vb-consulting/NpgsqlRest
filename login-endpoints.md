# Login Endpoints

Endpoints can be labeled as Login endpoints to act as authentication endpoints and perform user sign-in operations.

## How To Label Login Endpoint

1) By using `EndpointCreated` callback:

```csharp
app.UseNpgsqlRest(new NpgsqlRestOptions
{
    EndpointCreated = (routine, endpoint) =>
    {
        if (routine.Name == "login")
        {
            endpoint.Login = true;
        }
        return endpoint;
    }
});
```

2) By using the [`login` endpoint annotation](https://vb-consulting.github.io/npgsqlrest/annotations/#login):

```sql
create function auth.login(_username text, _password text) 
returns table (status int, name_identifier text, name text, role text[])
language sql as 
$$
-- select query that returns status, name_identifier, name and role array
$$;

comment on function auth.login(text, text) is '
HTTP POST
Login
';
```

## Login Endpoint Conventions

- The login-enabled endpoints must return a **named record (table)**.
  
- If the return type is not named record and it is one of these instead (`void` type, a simple value like `int` or `text`, or unnamed record) the endpoint will return an **unauthorized status (code 401)**.
  
- If the endpoint has a return type **named record** - but it returns an empty record the endpoint will return an **unauthorized status (code 401)**.

- If the endpoint has a return type **named record** - and it reruns some records, only the first records will be read and parsed, the rest is discarded.

- Returned columns are parsed by the column name. Column name can be the original column name from PostgreSQL or, parsed by the [default name converter](https://vb-consulting.github.io/npgsqlrest/options/#nameconverter). This camel case name converter, by default (unless changed in options).
  
- Three special column names are interpreted differently: the **status** column, the **scheme** column and the **message** column. See below for more details.

- If the column is neither of those three, the column name is interpreted as the [security claim type](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claim?view=net-8.0) and and column value is the security claim value (for the given type). These are values such as user name, user id, etc...


### Claim Types

- By default, if the column name interpreted as the security claim type, matches one of the Active Directory Federation Services Claim Types names, it will use that AD Federation Services Claim Type URI. The table can be seen here: [ClaimTypes Class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0#fields).
  
- That means that if the column name is either of these `NameIdentifier` - or the `nameidentifier` (case is ignored), or the `name_identifier` (for the camel case converted names, which is the default), the actual security claim type is `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` according to this table. 
  
- If the name is not found in this table, security is the column name as-is but parsed by the default name converter. 

- In this example column name `name_identifier` will be the security claim type the `nameIdentifier`. 

- This behavior that uses AD Federation Services Claim Type can be turned off with the [AuthenticationOptions.UseActiveDirectoryFederationServicesClaimTypes option](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsuseactivedirectoryfederationservicesclaimtypes).

### Status Columns

- The status column name is set by the option [AuthenticationOptions.StatusColumnName](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsstatuscolumnname). The default is `status`.

- This column can only be a boolean or numeric type. If it is neither boolean nor numeric, the endpoint will return status `500 InternalServerError` and you'll have to check logs.

- When this field is boolean, and it is true, the login process will continue with security claims set by other fields (which usually ends up in `200 OK` if authentication is configured). If it is false, the endpoint will return `404 NotFound` and the login attempt will not continue.

- When this field is numeric, and it is 200, the login process will continue with security claims set by other fields (which usually ends up in `200 OK` if authentication is configured). If it is not 200, the endpoint will return the status code the same as the value of this field, and the login attempt will not continue.

### Scheme Columns

- The scheme column name is set by the option [AuthenticationOptions.SchemeColumnName](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsschemecolumnname). The default is `scheme`.

- The textual value of this field will set the authentication scheme name for the sign-in operation.

- This is useful when using multiple authentication schemes. Returning any value from the logout routine will cause a logout from only that scheme. So for example, if you have the Cookie scheme and the Bearer Token scheme configured, you can handle them separately for login and logout.

### Message Columns

- The status column name is set by the option [AuthenticationOptions.MessageColumnName](https://vb-consulting.github.io/npgsqlrest/options/#authenticationoptionsmessagecolumnname). The default is `message`.

- This is the textual message that is returned in the response body.

- Note: this message is only returned in a case when the configured authentication scheme doesn't write anything into the response body on a sign-in operation.

- For example, the Cookie authentication scheme doesn't write anything into the body and this message is safely written to the response body. On the other hand, the Bearer Token schemes will always write the response body and this message will not be written to the response body.
