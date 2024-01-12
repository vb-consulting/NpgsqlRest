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
");
    }
}

[Collection("TestFixture")]
public class DefaultParametersTests(TestFixture test)
{
    [Fact]
    public async Task Test_CaseDefaultParams1()
    {
        using var content = new StringContent("{\"p1\": 11}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 2, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_CaseDefaultParams2()
    {
        using var content = new StringContent("{\"p1\": 11, \"p2\": 22}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_CaseDefaultParams3()
    {
        using var content = new StringContent("{\"p1\": 11, \"p2\": 22, \"p3\": 33}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_CaseDefaultParams4()
    {
        using var content = new StringContent("{\"p1\": 11, \"p2\": 22, \"p3\": 33, \"p4\": 44}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 44}");
    }

    [Fact]
    public async Task Test_CaseDefaultParams_MissingParam()
    {
        using var content = new StringContent("{\"p2\": 22}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-default-params/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_CaseGetDefaultParams1()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 2, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_CaseGetDefaultParams2()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11&p2=22");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 3, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_CaseGetDefaultParams3()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11&p2=22&p3=33");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 4}");
    }

    [Fact]
    public async Task Test_CaseGetDefaultParams4()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p1=11&p2=22&p3=33&p4=44");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("{\"p1\" : 11, \"p2\" : 22, \"p3\" : 33, \"p4\" : 44}");
    }

    [Fact]
    public async Task Test_CaseGetDefaultParams_MissingParam()
    {
        using var result = await test.Client.GetAsync("/api/case-get-default-params/?p2=11");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
