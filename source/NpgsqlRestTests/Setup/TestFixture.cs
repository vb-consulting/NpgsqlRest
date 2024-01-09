namespace NpgsqlRestTests;

[CollectionDefinition("TestFixture")]
public class TestFixtureCollection : ICollectionFixture<TestFixture> { }

public class TestFixture : IDisposable
{
    private readonly WebApplicationFactory<Program> application;
    private readonly HttpClient client;

    public HttpClient Client => client;

    public TestFixture()
    {
        application = new WebApplicationFactory<Program>();
        client = application.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);
    }

    public void Dispose()
    {
        client.Dispose();
        application.Dispose();
    }
}