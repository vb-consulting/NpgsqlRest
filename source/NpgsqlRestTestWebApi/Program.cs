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


app.UseNpgsqlRest(new NpgsqlRestOptions(connectionString)
{
    //ConnectionFromServiceProvider = true,
    //SchemaSimilarTo = "public",
    NameConverter = name => name,
    HttpFileOptions = new(true) { Overwrite = true },
    EndpointMetaCallback = (routine, options, meta) =>
    {
        meta.HttpMethod = HttpMethod.Post;
        meta.Parameters = EndpointParameters.BodyJson;
        return meta;
    },
});

app.MapGet("/test", () => "Hello World!");

app.Run();
