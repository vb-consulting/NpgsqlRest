using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NpgsqlRest.CrudSource;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using NpgsqlRest;

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
    static async Task ValidateAsync(ParameterValidationValues p)
    {
        if (p.Routine.Name == "case_jsonpath_param" && p.Parameter.Value?.ToString() == "XXX")
        {
            p.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await p.Context.Response.WriteAsync($"Paramater {p.ParamName} is not valid.");
        }

        if (string.Equals(p.Parameter.ParameterName, "_user_id", StringComparison.Ordinal))
        {
            if (p.Context?.User?.Identity?.IsAuthenticated is true)
            {
                p.Parameter.Value = p.Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                p.Parameter.Value = DBNull.Value;
            }
        }

        if (string.Equals(p.Parameter.ParameterName, "_user_roles", StringComparison.Ordinal))
        {
            if (p.Context?.User?.Identity?.IsAuthenticated is true)
            {
                p.Parameter.Value = p.Context.User?.Claims?.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];
            }
            else
            {
                p.Parameter.Value = DBNull.Value;
            }
        }
    }

    public static void Main()
    {
        var connectionString = Database.Create();
        // disable SQL rewriting to ensure that NpgsqlRest works with this option on.
        AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);

        var builder = WebApplication.CreateBuilder([]);

        builder
            .Services
            .AddAuthentication()
            //.AddBearerToken();
            .AddCookie();

        var app = builder.Build();

        app.MapGet("/login", () => Results.SignIn(new ClaimsPrincipal(new ClaimsIdentity(
            claims: new[]
            {
                new Claim(ClaimTypes.Name, "user"),
                new Claim(ClaimTypes.Role, "role1"),
                new Claim(ClaimTypes.Role, "role2"),
                new Claim(ClaimTypes.Role, "role3"),
            },
            authenticationType: CookieAuthenticationDefaults.AuthenticationScheme))));

        app.UseNpgsqlRest(new(connectionString)
        {
            //NameSimilarTo = "get_custom_param_query_1p",
            CommentsMode = CommentsMode.ParseAll,
            ValidateParametersAsync = ValidateAsync,
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
            },

            SourcesCreated = sources =>
            {
                //sources.Clear();
                sources.Add(new CrudSource());
                sources.Add(new TestSource());
            },

            CustomRequestHeaders = new()
            {
                { "custom-header1", "custom-header1-value" }
            }
        });
        app.Run();
    }
}
