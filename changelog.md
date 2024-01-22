# Changelog

## Version 1.2.0 (2024-22-01)

### 1) Fix AOT Warnings

A couple of issues have caused AOT warnings, although AOT worked before. This is now fixed:

#### 1) JSONify Strings

```csharp
[JsonSerializable(typeof(string))]
internal partial class NpgsqlRestSerializerContext : JsonSerializerContext { }

private static readonly JsonSerializerOptions plainTextSerializerOptions = new()
{
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    TypeInfoResolver = new NpgsqlRestSerializerContext()
};

// 
// AOT works but still emmits warnings IL2026 and IL3050
// 
JsonSerializer.Serialize(str, plainTextSerializerOptions)
```

This is fixed with suppression:

```csharp
[UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
[UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamic",
        Justification = "Serializes only string type that have AOT friendly TypeInfoResolver")]
private static string SerializeString(ref string value) => 
    JsonSerializer.Serialize(value, plainTextSerializerOptions);
```

#### 2) HTTP file endpoint

HTTP file endpoint was mapped with `MapGet`, and although it worked fine with AOT it emitted AOT warnings.

This is replaced with a middleware interceptor:

```csharp
builder.Use(async (context, next) =>
{
    if (!(string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) && 
        string.Equals(context.Request.Path, path, StringComparison.Ordinal)))
    {
        await next(context);
        return;
    }
    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync(content.ToString());
});
```

This resolved the warning but also removed the need for a routing component, which can make AOT builds even leaner and smaller:

```csharp
var builder = WebApplication.CreateEmptyBuilder(new ());
builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();
// not needed any more
// builder.Services.AddRoutingCore();
```

#### 3) Configuration Get Value From String

Method `GetHost` used `app.Configuration.GetValue<string>("ASPNETCORE_URLS")` configuration read which issued AOT warnings but it worked since it uses a string. This is suppressed with `[UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode", Justification = "Configuration.GetValue only uses string type parameters")]`.

### 2) Fix Arrays Encodings In Http Files

Fixed arrays encoding in query string parameters of the generated HTTP file sample.

### 3) Changed Method Type Logic

Change in logic that determines the endpoint default method:

- Before: endpoint is GET if the routine name contains string `get`.
- Now: it's complicated.

The endpoint is now GET if the routine name either:
- Starts with `get_` (case insensitive).
- Ends with `_get` (case insensitive).
- Contains `_get_` (case insensitive).

Otherwise, the endpoint is POST if the volatility option is VOLATILE. This assumes that the routine is changing some data, and that is also how PostgREST works and therefore endpoint is POST. Otherwise, it is GET.

You can see the source code here: [/DefaultEndpoint.cs](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/DefaultEndpoint.cs#L65)

This change may be breaking change in some rare circumstances (all 88 tests are passing without a change), but luckily not too many people are using this library at this point.

### 4) URL Path Handling Changes

Generated URLs by the default [DefaultUrlBuilder](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRest/DefaultUrlBuilder.cs#L5) will no longer add a slash character (`/`) at the end of the line.

However, when middleware tries to match the requested path, both versions will be matched (with an ending slash and without an ending slash).

That means that function with the signature `public.`test_func()` will match successfully both of these paths:
- `/api/test-func`
- `/api/test-func/`

But only one will be generated in an HTTP file: `/api/test-func`.

### 5) Fix Sending Request Headers With Parameter

There was a bug with the endpoint option to send request headers JSON through a routine parameter. It seems that the parameter name was always the default. This is fixed and [tested](https://github.com/vb-consulting/NpgsqlRest/blob/master/NpgsqlRestTests/RequestHeadersTests.cs#L115).

### 6) Sending Request Headers Parameter Defaults

Sending request headers with a routine parameter doesn't require that parameter to have a default value defined anymore.

### 7) New Feature: Body Parameter Name

There is a new feature that allows binding an entire body content to a particular routine parameter.

This is an endpoint-level value called `BodyParameterName`, which is NULL by default. 

- When this value is NULL (default), no parameter is assigned from body content. The same rules apply as before, nothing is changed.
- When this value is not NULL then:
  - If the endpoint has `RequestParamType` set to `BodyJson` it will be changed automatically to `QueryString` and a warning log will be emitted.
  - A parameter named `BodyParameterName` (matches original and converted names) will be assigned from the entire request body content.
  - If body content is NULL (not set), the parameter value will be NULL, and it will be always assigned from a body.
  - All other parameters will be assigned from the query string values (`RequestParamType = RequestParamType.QueryString`).

Example:

```sql
create function example_function(_p1 int, _content text) 
returns void 
language plpgsql
as 
$$
begin
    -- do something with _p1, and _content parameters
    raise info 'received % from _p1 and % from content', _p1, _content;
end;
$$;
```

```csharp
//
// Configure endpoint from example_function function to bind parameter _content from request body
//
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreated = (routine, endpoint) =>
    {
        if (routine.Name == "example_function")
        {
            return endpoint with { BodyParameterName = "_content" };
        }
        return endpoint;
    }
});
```

Note that this would also work if we used converted parameter name `content` (assuming we use the default converter that converts names to lower camel case).

This value is also configurable with comment annotations:

```sql
comment on function example_function(int, text) is '
HTTP
body-param-name content
';
```

In this comment annotation a keyword that comes before the parameter name could be any of these: `BodyParameterMame`, `body-parameter-name`, `body_parameter_name`, `BodyParamMame`, `body-param-name` or `body_param_name`.


## Version 1.1.0 (2024-19-01)

### 1) RoutineEndpoint Type Change

`RoutineEndpoint` type changed to `readonly record struct`

This allows for the manipulation of the endpoint parameter in the `EndpointCreated` callback event. 

This is useful to force certain endpoint configurations from the code, rather than through comment annotations.

Example:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointCreated = (routine, endpoint) =>
    {
        if (routine.SecurityType == SecurityType.Definer)
        {
            // filter out routines that can run as the definer user (this is usually superuser)
            return null;
        }
        if (string.Equals(routine.Name, "get_data", StringComparison.Ordinal))
        {
            // override the default response content type to be csv and don't rquire authorization 
            return endpoint with
            { 
                RequiresAuthorization = false, 
                ResponseContentType = "text/csv"
            };
        }
        // require authorization for all endpoints and force GET method
        return endpoint with { RequiresAuthorization = true, Method = Method.GET };
    }
});
```

### 2) New Event Option EndpointsCreated

`EndpointsCreated` option event:

- If defined (not null) - will be executed after all endpoints have been created and are ready for execution. This happens during the build phase.

- Receives one immutable parameter array of routine and e+ndpioint tuples: `(Routine routine, RoutineEndpoint endpoint)[]`.

- The option has the following signature: `public Action<(Routine routine, RoutineEndpoint endpoint)[]>? EndpointsCreated { get; set; }`.

- Example:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    EndpointsCreated = (endpoints) =>
    {
        foreach (var (routine, endpoint) in endpoints)
        {
            Console.WriteLine($"{routine.Type} {routine.Schema}.{routine.Signature} is mapped to {endpoint.Method} {endpoint.Url}");
        }
    }
});
```

This is useful in situations when we want to generate and create source code files based on generated endpoints such as Typescript or C# interfaces for example. It enables further automatic code generation based on generated endpoints.

### 3) New Custom Logger Option

There is a new option called `Logger` with the following signature: `public ILogger? Logger { get; set; }`

When provided (not null), this is the logger that will be used by default for all logging. You can use this to provide a custom logging implementation:

```csharp
app.UseNpgsqlRest(new(connectionString)
{
    Logger = new EmptyLogger()
});

class EmptyLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // empty
    }
}
```