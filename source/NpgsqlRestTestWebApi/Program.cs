using Npgsql;
using NpgsqlRest;
using NpgsqlRestTestWebApi;

//var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions { });
//builder.WebHost.UseKestrelCore();
//builder.Services.AddRoutingCore();
var builder = WebApplication.CreateSlimBuilder(args);
//var builder = WebApplication.CreateBuilder();

var connectionString = Database.Init(builder.Configuration.GetConnectionString("Default") ?? throw new("Connection string not found"));

var app = builder.Build();
app.Urls.Add("http://localhost:5000");

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    HttpFileOptions = new(true) { Overwrite = true, FileNamePattern = "postgres" },
});

app.Run();
