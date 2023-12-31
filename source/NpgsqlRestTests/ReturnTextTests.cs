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
    public async Task Test_HelloWordl()
    {
        using var result = await test.Client.PostAsync("/api/hello-world/", null);

        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("Hello World");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_CaseReturnText()
    {
        using var content = new StringContent("{\"t\":\"Hello World\"}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-text/", content);

        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("Hello World");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}