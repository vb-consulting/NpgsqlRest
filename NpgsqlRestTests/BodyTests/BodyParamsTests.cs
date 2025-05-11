#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void BodyParamsTests()
    {
        script.Append(@"
        create function body_params_test1(
            _i int,
            _t text
        ) 
        returns text
        language sql as $$select 1 || '-' || _t;$$;
");
    }
}

[Collection("TestFixture")]
public class BodyParamsTests(TestFixture test)
{
    [Fact]
    public async Task Test_body_params_test1()
    {
        string body = """
        {
            "i": 666,
            "t": "numberofthebeast"
        }
        """;
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/body-params-test1/", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Be("1-numberofthebeast");
    }

    [Fact]
    public async Task Test_body_params_test1_reverse()
    {
        string body = """
        {
            "t": "numberofthebeast",
            "i": 666
        }
        """;
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/body-params-test1/", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Be("1-numberofthebeast");
    }
}