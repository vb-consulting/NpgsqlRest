namespace NpgsqlRestTests;

public static partial class Database
{
    public static void OverloadTests()
    {
        script.Append(@"
create function case_overload() 
returns text 
language plpgsql
as 
$$
begin
    return 'no params';
end;
$$;

create function case_overload(_i int) 
returns text 
language plpgsql
as 
$$
begin
    return '1 param';
end;
$$;

create function case_overload(_i int, _t text) 
returns text 
language plpgsql
as 
$$
begin
    return '2 params';
end;
$$;

create function case_overload(_i int, _t text, _b boolean) 
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
public class OverloadTests(TestFixture test)
{
    [Fact]
    public async Task Test_Overload_NoParams1()
    {
        using var result = await test.Client.PostAsync("/api/case-overload/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        response.Should().Be("no params");
    }

    [Fact]
    public async Task Test_case_overload_NoParams2()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-overload/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        response.Should().Be("no params");
    }

    [Fact]
    public async Task Test_case_overload_OneParam()
    {
        using var content = new StringContent("{\"i\": 1}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-overload/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        response.Should().Be("1 param");
    }

    [Fact]
    public async Task Test_case_overload_Two_arams()
    {
        using var content = new StringContent("{\"i\": 1, \"t\": \"ABC\"}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-overload/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        response.Should().Be("2 params");
    }

    [Fact]
    public async Task Test_case_overload_ThreeParams()
    {
        using var content = new StringContent("{\"i\": 1, \"t\": \"ABC\", \"b\": true}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-overload/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        response.Should().Be("3 params");
    }

    [Fact]
    public async Task Test_case_overload_WrongParams()
    {
        using var content = new StringContent("{\"i\": 1, \"t\": \"ABC\", \"X\": true}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-overload/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}