namespace NpgsqlRestTests;

public static partial class Database
{
    public static void VoidUnitTests()
    {
        script.Append(@"
create function case_void() 
returns void 
language plpgsql
as 
$$
begin
    raise info 'case_void';
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class VoidUnitTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_void()
    {
        using var result = await test.Client.PostAsync("/api/case-void/", null);
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}