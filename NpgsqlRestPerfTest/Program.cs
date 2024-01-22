//dotnet publish -r linux-x64 -c Release
using NpgsqlRest;

var builder = WebApplication.CreateEmptyBuilder(new ());
builder.WebHost.UseKestrelCore();
var app = builder.Build();
app.UseNpgsqlRest(new("Host=127.0.0.1;Port=5432;Database=perf_tests;Username=postgres;Password=postgres"));
app.Run();
