using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());
builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();
var app = builder.Build();
var connectionString = NpgsqlRestTests.Database.Create();

app.UseNpgsqlRest(new(connectionString)
{
    ConnectionString = connectionString,
    HttpFileOptions = new() 
    { 
        FileOverwrite = true
    }
});

app.Run();
