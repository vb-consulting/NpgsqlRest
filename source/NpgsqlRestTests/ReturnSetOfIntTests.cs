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
    public async Task Test_CaseReturnSetOfInt()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-int/", null);

        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("[1,2,3]");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}