namespace NpgsqlRestTests;

public static partial class Database
{
    public static void Test_CaseVoid()
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
    public async Task Test_CaseVoid()
    {
        using var result = await test.Client.PostAsync("/api/case-void/", null);
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}