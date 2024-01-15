using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace NpgsqlRestTests;

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
        var builder = WebApplication.CreateEmptyBuilder(new());
        builder.WebHost.UseKestrelCore();
        var connectionString = Database.Create();
        var app = builder.Build();
        app.UseNpgsqlRest(new(connectionString) { ValidateParameters = Validate });
        app.Run();
    }
}
