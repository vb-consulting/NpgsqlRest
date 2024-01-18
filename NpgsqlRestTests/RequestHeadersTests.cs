namespace NpgsqlRestTests;

public static partial class Database
{
    public static void RequestHeadersTests()
    {
        script.Append(@"
create function get_req_headers_ignore() returns text language sql as 'select current_setting(''request.headers'', true)';

create function get_req_headers_context() returns text language sql as 'select current_setting(''request.headers'', true)';
comment on function get_req_headers_context() is '
HTTP
RequestHeaders Context
';



create function get_req_headers_parameter1(_headers text = null) returns text language sql as 'select _headers';
comment on function get_req_headers_parameter1(text) is '
HTTP
request-headers parameter
';

create function get_req_headers_parameter2(headers text = null) returns text language sql as 'select headers';
comment on function get_req_headers_parameter2(text) is '
HTTP
request-headers parameter
';

create function get_req_headers_parameter3(h text = null) returns text language sql as 'select h';
comment on function get_req_headers_parameter3(text) is '
HTTP
request-headers parameter
request-headers-parameter-name h
';



create function req_headers_parameter1(_headers text = null) returns text language sql as 'select _headers';
comment on function req_headers_parameter1(text) is '
HTTP
request_headers parameter
';

create function req_headers_parameter2(headers text = null) returns text language sql as 'select headers';
comment on function req_headers_parameter2(text) is '
HTTP
request_headers parameter
';

create function req_headers_parameter3(h text = null) returns text language sql as 'select h';
comment on function req_headers_parameter3(text) is '
HTTP
request-headers parameter
request_headers_parameter_name h
';
");
    }
}

[Collection("TestFixture")]
public class RequestHeadersTests(TestFixture test)
{
    [Fact]
    public async Task Test_GetReqHeadersIgnore()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-ignore/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_GetReqHeadersContext()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-context/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\"}");
    }

    [Fact]
    public async Task Test_GetReqHeadersParameter1()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter1/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\"}");
    }

    [Fact]
    public async Task Test_GetReqHeadersParameter2()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter2/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\"}");
    }

    [Fact]
    public async Task Test_GetReqHeadersParameter2_ValueProvided()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter2/?headers=abc");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("abc");
    }

    [Fact]
    public async Task Test_GetReqHeadersParameter3()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter2/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\"}");
    }

    [Fact]
    public async Task Test_ReqHeadersParameter1()
    {
        using var response = await test.Client.PostAsync("/api/req-headers-parameter1/", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\"}");
    }

    [Fact]
    public async Task Test_ReqHeadersParameter2()
    {
        using var response = await test.Client.PostAsync("/api/req-headers-parameter2/", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\"}");
    }

    [Fact]
    public async Task Test_ReqHeadersParameter2_ValueProvided()
    {
        using var body = new StringContent("{\"headers\":\"abc\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/req-headers-parameter2/", body);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("abc");
    }

    [Fact]
    public async Task Test_ReqHeadersParameter3()
    {
        using var response = await test.Client.PostAsync("/api/req-headers-parameter2/", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\"}");
    }
}