using System.Net;
using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());
builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();
var app = builder.Build();
var connectionString = NpgsqlRestTests.Database.Create();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    LogCommands = true,
    HttpFileOptions = new() 
    { 
        FileOverwrite = true
    },
    
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
