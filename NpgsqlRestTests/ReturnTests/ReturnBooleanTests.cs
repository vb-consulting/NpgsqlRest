namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnBooleanTests()
    {
        script.Append(@"
create function return_true() 
returns boolean 
language sql
as $$
select true;
$$;

create function return_false() 
returns boolean 
language sql
as $$
select false;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnBooleanTests(TestFixture test)
{
    [Fact]
    public async Task Test_return_true()
    {
        using var result = await test.Client.PostAsync("/api/return-true/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("t");
    }

    [Fact]
    public async Task Test_return_false()
    {
        using var result = await test.Client.PostAsync("/api/return-false/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("f");
    }
}