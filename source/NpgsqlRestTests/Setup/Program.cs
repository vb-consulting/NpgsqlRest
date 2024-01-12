using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace NpgsqlRestTests;

public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateEmptyBuilder(new());
        builder.WebHost.UseKestrelCore();

        try
        {
            var connectionString = Database.Create(addNamePrefix: true, recreate: true);

            var app = builder.Build();
            app.UseNpgsqlRest(new(connectionString));
            app.Run();
        }
        finally
        {
            Database.DropIfExists(create: false);
        }
    }
}
