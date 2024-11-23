namespace NpgsqlRestTests;

public static partial class Database
{
    public static void NonPublicSchemaTests()
    {
        script.Append(@"
create schema if not exists my_schema;

create function my_schema.hello_world() 
returns text 
language sql
as $$
select 'Hello World'
$$;

create function my_schema.case_return_text(_t text) 
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
public class NonPublicSchemaTests(TestFixture test)
{
    [Fact]
    public async Task Test_my_schema__hello_world()
    {
        using var result = await test.Client.PostAsync("/api/my-schema/hello-world/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("Hello World");
    }

    [Fact]
    public async Task Test_my_schema__case_return_text()
    {
        using var content = new StringContent("{\"t\":\"Hello World\"}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/my-schema/case-return-text/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("Hello World");
    }
}