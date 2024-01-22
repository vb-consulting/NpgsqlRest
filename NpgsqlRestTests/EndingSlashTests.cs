namespace NpgsqlRestTests;

public static partial class Database
{
    public static void EndingSlashTests()
    {
        script.Append(@"
create function ending_slash() 
returns text 
language sql
volatile
as $$
select 'ending_slash'
$$;
");
    }
}

[Collection("TestFixture")]
public class EndingSlashTests(TestFixture test)
{
    [Fact]
    public async Task Test_ending_slash()
    {
        using var response = await test.Client.PostAsync("/api/ending-slash/", null);
        response?.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_ending_slash_NoSlash()
    {
        using var response = await test.Client.PostAsync("/api/ending-slash", null);
        response?.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}