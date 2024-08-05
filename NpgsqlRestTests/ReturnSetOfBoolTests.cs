namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnSetOfBoolTests()
    {
        script.Append(@"
create function case_return_setof_bool() 
returns setof boolean 
language plpgsql
as 
$$
begin
    return query select j from (
        values (true), (false), (true), (false)
    ) t(j);
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnSetOfBoolTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_return_setof_bool()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-bool/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        response.Should().Be("[true,false,true,false]");
    }
}