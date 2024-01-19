# Changelog

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

There is a new option called `Logger` with the following signature: `public ILogger? Logger { get; set; } `

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