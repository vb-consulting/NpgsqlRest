using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            Logger = new EmptyLogger()
        });
        app.Run();
    }
}
