namespace NpgsqlRestTests;

public static partial class Database
{
    public static void Test_CaseReturnJson()
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
    public async Task Test_CaseReturnJson()
    {
        using var content = new StringContent("{\"json\": {\"a\": 1, \"b\": \"c\"}}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-json/", content);

        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("{\r\n  \"a\": 1,\r\n  \"b\": \"c\"\r\n}");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}