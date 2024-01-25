//
//  aot build:
//  dotnet publish -r linux-x64 -c Release
//
using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());
builder.WebHost.UseKestrelCore();
var app = builder.Build();
app.Urls.Add("http://localhost:5000");
app.UseNpgsqlRest(new("Host=127.0.0.1;Port=5432;Database=perf_tests;Username=postgres;Password=postgres")
{
    //
    // PostgREST compatibility:
    // Leave the names intact as-is, and send request headers into the connection context.
    //
    NameConverter = n => n,
    RequestHeadersMode = RequestHeadersMode.Context
});
app.Run();
