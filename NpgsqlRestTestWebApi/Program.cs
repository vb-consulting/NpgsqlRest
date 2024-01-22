using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());
builder.Logging.AddConsole();
builder.WebHost.UseKestrelCore();
var app = builder.Build();
var connectionString = NpgsqlRestTests.Database.Create();

app.UseNpgsqlRest(new()
{
    ConnectionString = connectionString,
    //NameSimilarTo = "case_get_int_params_array",
    HttpFileOptions = new() 
    { 
        FileOverwrite = true
    }
});

app.Run();
