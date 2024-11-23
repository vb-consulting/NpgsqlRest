namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ArrayParametersTests()
    {
        script.Append("""
create function case_return_int_params_array(_a int[]) 
returns int[]
language plpgsql
as 
$$
begin
    return _a;
end;
$$;

create function case_return_text_params_array(_a text[]) 
returns text[]
language plpgsql
as 
$$
begin
    return _a;
end;
$$;

create function case_get_int_params_array(_a int[]) 
returns int[]
language plpgsql
as 
$$
begin
    return _a;
end;
$$;

create function case_get_text_params_array(_a text[]) 
returns text[]
language plpgsql
as 
$$
begin
    return _a;
end;
$$;
""");
    }
}

[Collection("TestFixture")]
public class ArrayParametersTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_return_int_params_array()
    {
        using var content = new StringContent("{\"a\":[1,2,3,4,5,6]}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-int-params-array/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[1,2,3,4,5,6]");
    }

    [Fact]
    public async Task Test_case_return_int_params_array_NullValue()
    {
        using var content = new StringContent("{\"a\":[1,2,3,null]}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-int-params-array/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[1,2,3,null]");
    }

    [Fact]
    public async Task Test_case_return_text_params_array()
    {
        using var content = new StringContent("{\"a\":[\"abc\",\"xyz\"]}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-text-params-array/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[\"abc\",\"xyz\"]");
    }

    [Fact]
    public async Task Test_case_return_text_params_array_NullValue()
    {
        using var content = new StringContent("{\"a\":[\"abc\",null,\"null\",\"NULL\",\"xyz\"]}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-text-params-array/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[\"abc\",null,\"null\",\"NULL\",\"xyz\"]");
    }

    [Fact]
    public async Task Test_case_get_int_params_array()
    {
        using var result = await test.Client.GetAsync("/api/case-get-int-params-array/?a=123&a=456&a=789");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[123,456,789]");
    }

    [Fact]
    public async Task Test_case_get_int_params_array_NullValue()
    {
        using var result = await test.Client.GetAsync("/api/case-get-int-params-array/?a=999&a=&a=666");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[999,null,666]");
    }

    [Fact]
    public async Task Test_case_get_text_params_array()
    {
        using var result = await test.Client.GetAsync("/api/case-get-text-params-array/?a=abc&a=xyz&a=foobar");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[\"abc\",\"xyz\",\"foobar\"]");
    }
}
