namespace NpgsqlRestTests;

public static partial class Database
{
    public static void OverloadQueryStringTests()
    {
        script.Append(@"
create function case_get_overload() 
returns text 
language plpgsql
as 
$$
begin
    return 'no params';
end;
$$;

create function case_get_overload(_i int) 
returns text 
language plpgsql
as 
$$
begin
    return '1 param';
end;
$$;

create function case_get_overload(_i int, _t text) 
returns text 
language plpgsql
as 
$$
begin
    return '2 params';
end;
$$;

create function case_get_overload(_i int, _t text, _b boolean) 
returns text 
language plpgsql
as 
$$
begin
    return '3 params';
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class OverloadQueryStringTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_get_overload_NoParams()
    {
        using var result = await test.Client.GetAsync("/api/case-get-overload/");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("no params");
    }

    [Fact]
    public async Task Test_case_get_overload_OneParam()
    {
        var query = new QueryBuilder { { "i", "1" } };
        using var result = await test.Client.GetAsync($"/api/case-get-overload/{query}");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("1 param");
    }

    [Fact]
    public async Task Test_case_get_overload_TwoParams()
    {
        var query = new QueryBuilder { { "i", "1" }, { "t", "ABC" } };
        using var result = await test.Client.GetAsync($"/api/case-get-overload/{query}");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("2 params");
    }

    [Fact]
    public async Task Test_case_get_overload_ThreeParams()
    {
        var query = new QueryBuilder { { "i", "1" }, { "t", "ABC" }, { "b", "true" } };
        using var result = await test.Client.GetAsync($"/api/case-get-overload/{query}");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("3 params");
    }

    [Fact]
    public async Task Test_case_get_overload_WrongParams()
    {
        var query = new QueryBuilder { { "i", "1" }, { "t", "ABC" }, { "X", "true" } };
        using var result = await test.Client.GetAsync($"/api/case-get-overload/{query}");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}