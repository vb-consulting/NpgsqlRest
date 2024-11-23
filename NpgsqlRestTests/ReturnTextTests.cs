namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnTextTests()
    {
        script.Append(@"
create function hello_world() 
returns text 
language sql
as $$
select 'Hello World'
$$;

create function case_return_text(_t text) 
returns text 
language plpgsql
as 
$$
begin
    raise info '_t = %', _t;
    return _t;
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnTextTests(TestFixture test)
{
    [Fact]
    public async Task Test_hello_world()
    {
        using var result = await test.Client.PostAsync("/api/hello-world/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("Hello World");
    }

    [Fact]
    public async Task Test_case_return_text()
    {
        using var content = new StringContent("{\"t\":\"Hello World\"}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-text/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("Hello World");
    }
}