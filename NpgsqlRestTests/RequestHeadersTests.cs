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
request_headers context
';

create function get_req_headers_parameter1(_headers text = null) returns text language sql as 'select _headers';
comment on function get_req_headers_parameter1(text) is '
HTTP
request_headers parameter
';

create function get_req_headers_parameter2(headers text = null) returns text language sql as 'select headers';
comment on function get_req_headers_parameter2(text) is '
HTTP
request_headers parameter
';

create function get_req_headers_parameter3(h text = null) returns text language sql as 'select h';
comment on function get_req_headers_parameter3(text) is '
HTTP
request_headers parameter
request_headers_parameter_name h
';

create function get_req_headers_param_not_default(_not_default text) returns text language sql as 'select _not_default';
comment on function get_req_headers_param_not_default(text) is '
HTTP
request_headers parameter
request_headers_parameter_name _not_default
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
request_headers parameter
request_headers_parameter_name h
';

create function req_headers_param_not_default(_not_default text) returns text language sql as 'select _not_default';
comment on function req_headers_param_not_default(text) is '
HTTP
request_headers parameter
request_headers_parameter_name _not_default
';
");
    }
}

[Collection("TestFixture")]
public class RequestHeadersTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_req_headers_ignore()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-ignore/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_get_req_headers_context()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-context/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\",\"custom-header1\":\"custom-header1-value\"}");
    }

    [Fact]
    public async Task Test_get_req_headers_parameter1()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter1/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\",\"custom-header1\":\"custom-header1-value\"}");
    }

    [Fact]
    public async Task Test_get_req_headers_parameter2()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter2/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\",\"custom-header1\":\"custom-header1-value\"}");
    }

    [Fact]
    public async Task Test_get_req_headers_parameter2_ValueProvided()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter2/?headers=abc");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("abc");
    }

    [Fact]
    public async Task Test_get_req_headers_parameter3()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-parameter3/");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\",\"custom-header1\":\"custom-header1-value\"}");
    }

    [Fact]
    public async Task Test_get_req_headers_param_not_default()
    {
        using var response = await test.Client.GetAsync("/api/get-req-headers-param-not-default");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_req_headers_parameter1()
    {
        using var response = await test.Client.PostAsync("/api/req-headers-parameter1/", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\",\"custom-header1\":\"custom-header1-value\"}");
    }

    [Fact]
    public async Task Test_req_headers_parameter2()
    {
        using var response = await test.Client.PostAsync("/api/req-headers-parameter2/", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\",\"custom-header1\":\"custom-header1-value\"}");
    }

    [Fact]
    public async Task Test_req_headers_parameter2_ValueProvided()
    {
        using var body = new StringContent("{\"headers\":\"abc\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/req-headers-parameter2/", body);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("abc");
    }

    [Fact]
    public async Task Test_req_headers_parameter3()
    {
        using var response = await test.Client.PostAsync("/api/req-headers-parameter3/", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"Host\":\"localhost\",\"custom-header1\":\"custom-header1-value\"}");
    }

    [Fact]
    public async Task Test_req_headers_param_not_default()
    {
        using var response = await test.Client.PostAsync("/api/req-headers-param-not-default", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}