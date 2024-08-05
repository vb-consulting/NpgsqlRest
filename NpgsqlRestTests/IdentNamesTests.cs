namespace NpgsqlRestTests;

public static partial class Database
{
    public static void IdentNamesTests()
    {
        script.Append("""

create function "select"("group" int, "order" int) 
returns table (
    "from" int,
    "join" int
)
language plpgsql
as 
$$
begin
    return query 
    select t.*
    from (
        values 
        ("group", "order")
    ) t;
end;
$$;

""");
    }
}

[Collection("TestFixture")]
public class IdentNamesTests(TestFixture test)
{
    [Fact]
    public async Task Test_select_ident_names_function()
    {
        using var body = new StringContent("{\"group\": 1, \"order\": 2}", Encoding.UTF8);
        using var response = await test.Client.PostAsync("/api/select/", body);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"from\":1,\"join\":2}]");
    }
}
