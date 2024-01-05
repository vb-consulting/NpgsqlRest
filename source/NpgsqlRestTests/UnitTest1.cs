using Microsoft.AspNetCore.Mvc.Testing;

namespace NpgsqlRestTests;

public class UnitTest1
{
    private readonly HttpClient client;

    public UnitTest1()
    {
        var application = new WebApplicationFactory<Program>();
        client = application.CreateClient();
    }

    [Fact]
    public async Task Test1()
    {
        var result = await client.GetStringAsync("/todos");//.Result.Should().Be("{\"message1\":\"Hello World 1!\"}");
        Assert.Equal("{\"message1\":\"Hello World 1!\"}", result);
    }
}