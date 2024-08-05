namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnSetOfJsonTests()
    {
        script.Append(@"
create function case_return_setof_json() 
returns setof json 
language plpgsql
as 
$$
begin
    return query select j from (
        values 
            (json_build_object('A', 1)),
            (json_build_object('B', 'XY')),
            (json_build_object('C', true)),
            (json_build_object('D', null))
    ) t(j);
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnSetOfJsonTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_return_setof_json()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-json/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        response.Should().Be("[{\"A\" : 1},{\"B\" : \"XY\"},{\"C\" : true},{\"D\" : null}]");
    }
}