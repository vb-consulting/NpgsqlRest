using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());

builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();
// needed for HttpFileOption.Both or HttpFileOption.Endpoint
builder.Services.AddRoutingCore();

var connectionString = NpgsqlRestTests.Database.Create();

var app = builder.Build();


app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    HttpFileOptions = new() 
    { 
        FileOverwrite = true
    }
});

app.Run();
