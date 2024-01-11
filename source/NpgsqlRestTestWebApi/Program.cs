using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());

builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();
builder.Services.AddRoutingCore();

var connectionString = NpgsqlRestTests.Database.Create(addNamePrefix: false, recreate: true);

var app = builder.Build();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    NameSimilarTo = "%array%",
    HttpFileOptions = new() 
    { 
        FileOverwrite = true
    },
});

app.Run();
