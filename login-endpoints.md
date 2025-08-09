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
returns table (status int, id text, name text, roles text[])
language sql as 
$$
-- select query that returns status, id, name and roles array
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

- If the endpoint has a return type **named record** - and does return some records - only the first records will be read and parsed, the rest is discarded.

- Returned columns are parsed by the column name. Column name can be the original column name from PostgreSQL or, parsed by the [default name converter](https://vb-consulting.github.io/npgsqlrest/options/#nameconverter). This camel case name converter, by default (unless changed in options).
  
- Three special column names are interpreted differently: the **status** column, the **scheme** column and the **message** column. See below for more details.

- If the column is neither of those three, the column name is interpreted as the [security claim type](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claim?view=net-8.0) and and column value is the security claim value (for the given type). These are values such as user name, user id, etc...


### Claim Types

- Column names from login endpoint responses are converted directly to claim types without any transformation or mapping.
  
- Common claim types you might use include:
  - `id` for user identification
  - `name` for user display name  
  - `roles` roles (can be an array)
  - `email` for user email address
  - etc

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

## External Logins

External logins are functionality from the client application. They are configured in the following configuration section:

```jsonc
{
    //
    // ...
    //
    "Auth": {
        //
        // ...
        //
        "External": {
            //
            // ...
            //
            "LoginCommand": "select * from external_login($1,$2,$3)",
            //
            // ...
            //
        }
    }
}
```

See the [Client Application Default Config](https://vb-consulting.github.io/npgsqlrest/config/) for more info.

Return values from this command follow the same convention [described above](https://vb-consulting.github.io/npgsqlrest/login-endpoints/#login-endpoint-conventions).

This command can have up to three parameters maximum. The second and third parameters are optional. These are:

1) Email (text) received from the external provider.
2) Name (text) received from the external provider. If the external provider doesn't provide a name this is null.
3) Parameters (JSON). Includes a collection of parameter values received from the external provider, plus the original query strings if any (for example `/signin-google?param1=test`). This allows for different types of processing of external provider data (registration or just login for example).

