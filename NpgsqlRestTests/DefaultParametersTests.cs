namespace NpgsqlRestTests;

public static partial class Database
{
    public static void DefaultParametersTests()
    {
        script.Append(@"
create function case_default_params(
    _p1 int,
    _p2 int = 2,
    _p3 int = 3,
    _p4 int = 4
) 
returns json 
language plpgsql
as 
$$
begin
    return json_build_object(
        'p1', _p1,
        'p2', _p2,
        'p3', _p3,
        'p4', _p4
    );
end;
$$;

create function case_get_default_params(
    _p1 int,
    _p2 int = 2,
    _p3 int = 3,
    _p4 int = 4
) 
returns json 
language plpgsql
as 
$$
begin
    return json_build_object(
        'p1', _p1,
        'p2', _p2,
        'p3', _p3,
        'p4', _p4
    );
end;
$$;

create function case_single_default_params(_p text = 'xyz') 
returns text 
language sql
as 
$$
select _p;
$$;


create function get_two_default_params(_p1 text = 'abc', _p2 text = 'xyz') 
returns text 
language sql
as 
$$
select _p1 || _p2;
$$;
");
    }
}

[Collection("TestFixture")]
public class DefaultParametersTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_default_params__1()
    {
        using var content = new StringContent("{\"p1\": 11}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 2, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_case_default_params__2()
    {
        using var content = new StringContent("{\"p1\": 11, \"p2\": 22}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_case_default_params__3()
    {
        using var content = new StringContent("{\"p1\": 11, \"p2\": 22, \"p3\": 33}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_case_default_params__4()
    {
        using var content = new StringContent("{\"p1\": 11, \"p2\": 22, \"p3\": 33, \"p4\": 44}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 44}");
    }

    [Fact]
    public async Task Test_case_default_params_MissingParam()
    {
        using var content = new StringContent("{\"p2\": 22}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_case_get_default_params__1()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 2, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_case_get_default_params__2()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11&p2=22");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_case_get_default_params__3()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11&p2=22&p3=33");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_case_get_default_params__4()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11&p2=22&p3=33&p4=44");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 44}");
    }

    [Fact]
    public async Task Test_case_get_default_params_MissingParam()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p2=11");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_case_single_default_params()
    {
        using var content = new StringContent("{\"p\": \"abc\"}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-single-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("abc");
    }

    [Fact]
    public async Task Test_case_single_default_params_EmptyBody()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-single-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("xyz");
    }

    [Fact]
    public async Task Test_case_single_default_params_NullBody()
    {
        using var result = await test.Client.PostAsync("/api/case-single-default-params/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("xyz");
    }

    [Fact]
    public async Task Test_get_two_default_params__1()
    {
        using var result = await test.Client.GetAsync("/api/get-two-default-params/?p1=aa&p2=bb");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("aabb");
    }

    [Fact]
    public async Task Test_get_two_default_params__2()
    {
        using var result = await test.Client.GetAsync("/api/get-two-default-params/?p1=aa");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("aaxyz");
    }

    [Fact]
    public async Task Test_get_two_default_params__3()
    {
        using var result = await test.Client.GetAsync("/api/get-two-default-params/?p2=bb");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("abcbb");
    }

    [Fact]
    public async Task Test_get_two_default_params__4()
    {
        using var result = await test.Client.GetAsync("/api/get-two-default-params/");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("abcxyz");
    }
}
