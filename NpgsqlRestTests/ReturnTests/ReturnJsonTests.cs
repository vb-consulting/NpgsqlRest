#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnJsonTests()
    {
        script.Append(@"
create function case_return_json(_json json) 
returns json 
language plpgsql
as 
$$
begin
    raise info '_json = %', _json;
    return _json;
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnJsonTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_return_json()
    {
        using var content = new StringContent("{\"json\": {\"a\": 1, \"b\": \"c\"}}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-json/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var node = JsonNode.Parse(response);
        node["a"].ToJsonString().Should().Be("1");
        node["b"].ToJsonString().Should().Be("\"c\"");
    }
}