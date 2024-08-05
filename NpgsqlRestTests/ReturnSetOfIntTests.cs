namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnSetOfIntTests()
    {
        script.Append(@"
create function case_return_setof_int() 
returns setof int 
language plpgsql
as 
$$
begin
    return query select i from (values (1), (2), (3)) t(i);
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnSetOfIntTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_return_setof_int()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-int/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        response.Should().Be("[1,2,3]");
    }
}