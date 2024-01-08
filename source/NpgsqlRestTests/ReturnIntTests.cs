namespace NpgsqlRestTests;

public static partial class Database
{
    public static void Test_CaseReturnInt()
    {
        script.Append(@"
create function case_return_int(_i int) 
returns int 
language plpgsql
as 
$$
begin
    raise info '_i = %', _i;
    return _i;
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnIntTests(TestFixture test)
{
    [Fact]
    public async Task Test_CaseReturnInt()
    {
        using var content = new StringContent("{\"i\":999}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-int/", content);

        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("999");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}