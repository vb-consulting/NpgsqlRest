using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
namespace NpgsqlRestTests;

public class EmptyLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
}

public class Program
{
    static void Validate(ParameterValidationValues p)
    {
        if (string.Equals(p.Context.Request.Path, "/api/case-jsonpath-param/", StringComparison.Ordinal))
        {
            if (p.Parameter.Value is not null)
            {
                if (string.Equals(p.Parameter.Value.ToString(), "XXX", StringComparison.Ordinal))
                {
                    p.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }
        }
    }

    public static void Main()
    {
        var connectionString = Database.Create();
        // disable SQL rewriting to ensure that NpgsqlRest works with this option on.
        AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);

        var builder = WebApplication.CreateEmptyBuilder(new());
        builder.WebHost.UseKestrelCore();
        var app = builder.Build();
        app.UseNpgsqlRest(new(connectionString)
        {
            ValidateParameters = Validate,
            Logger = new EmptyLogger(),
            CommandCallbackAsync = async p =>
            {
                if (string.Equals(p.routine.Name , "get_csv_data"))
                {
                    p.context.Response.ContentType = "text/csv";
                    await using var reader = await p.command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var line = $"{reader[0]},{reader[1]},{reader.GetDateTime(2):s},{reader.GetBoolean(3).ToString().ToLowerInvariant()}\n";
                        await p.context.Response.WriteAsync(line);
                    }
                }
            }
        });
        app.Run();
    }
}
