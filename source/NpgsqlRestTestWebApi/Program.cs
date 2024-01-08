using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());

builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();

var connectionString = NpgsqlRestTests.Database.Create(addNamePrefix: false, recreate: true);

var app = builder.Build();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    HttpFileOptions = new(true) { Overwrite = true, FileNamePattern = "postgres" },
});

app.Run();
