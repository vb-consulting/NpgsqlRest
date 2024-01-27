namespace NpgsqlRestTests;

public static partial class Database
{
    public static void StrictFunctionsTests()
    {
        script.Append(@"
create function strict_function(_p1 int, _p2 int) 
returns text 
strict
language plpgsql
as 
$$
begin
    raise info '_p1 = %, _p2 = %', _p1, _p2;
    return 'strict';
end;
$$;

create function returns_null_on_null_input_function(_p1 int, _p2 int) 
returns text 
returns null on null input
language plpgsql
as 
$$
begin
    raise info '_p1 = %, _p2 = %', _p1, _p2;
    return 'returns null on null input';
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class StrictFunctionsTests(TestFixture test)
{
    [Fact]
    public async Task Test_strict_function_NoNulls()
    {
        using var body = new StringContent("{\"p1\": 1, \"p2\": 2}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/strict-function", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        content.Should().Be("strict");
    }
    
    [Fact]
    public async Task Test_strict_function_OneNull()
    {
        using var body = new StringContent("{\"p1\": 1, \"p2\": null}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/strict-function", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        content.Should().Be("");
    }
    
    [Fact]
    public async Task Test_strict_function_TwoNulls()
    {
        using var body = new StringContent("{\"p1\": null, \"p2\": null}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/strict-function", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        content.Should().Be("");
    }
    
    
    [Fact]
    public async Task Test_returns_null_on_null_input_function_NoNulls()
    {
        using var body = new StringContent("{\"p1\": 1, \"p2\": 2}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/returns-null-on-null-input-function", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        content.Should().Be("returns null on null input");
    }
    
    [Fact]
    public async Task Test_returns_null_on_null_input_function_OneNull()
    {
        using var body = new StringContent("{\"p1\": 1, \"p2\": null}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/returns-null-on-null-input-function", body);
        var content = await response.Content.ReadAsStringAsync();
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        content.Should().Be("");
    }
    
    [Fact]
    public async Task Test_returns_null_on_null_input_function_TwoNulls()
    {
        using var body = new StringContent("{\"p1\": null, \"p2\": null}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/returns-null-on-null-input-function", body);
        var content = await response.Content.ReadAsStringAsync();
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        content.Should().Be("");
    }
}