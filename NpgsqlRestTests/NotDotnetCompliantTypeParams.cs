namespace NpgsqlRestTests;

public static partial class Database
{
    public static void NotDotnetCompliantTypeParams()
    {
        script.Append(@"
create function case_jsonpath_param(
    _jsonpath jsonpath
) 
returns text
language plpgsql
as 
$$
begin
    return _jsonpath::text;
end;
$$;
");
    }
}


[Collection("TestFixture")]
public class NotDotnetCompliantTypeParams(TestFixture test)
{
    [Fact]
    public async Task Test_ValidateParameter()
    {
        string body = """
        {  
            "jsonpath": "XXX"
        }
""";
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-jsonpath-param/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
