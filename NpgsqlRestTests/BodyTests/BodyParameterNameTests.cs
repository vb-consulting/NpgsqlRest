namespace NpgsqlRestTests;

public static partial class Database
{
    public static void BodyParameterNameTests()
    {
        script.Append(@"
create function body_param_name_1p(_p text) 
returns text language sql as 'select _p';
comment on function body_param_name_1p(text) is '
HTTP
body_param_name p
';

create function body_param_name_2p(_i int, _p text) 
returns text language sql as 'select _i::text || '' '' || _p';
comment on function body_param_name_2p(int, text) is '
HTTP
body_param_name _p
';

create function body_param_name_int(_int int) 
returns text language sql as 'select _int';
comment on function body_param_name_int(int) is '
HTTP
body_param_name int';
");
    }
}

[Collection("TestFixture")]
public class BodyParameterNameTests(TestFixture test)
{
    [Fact]
    public async Task Test_body_param_name_1p()
    {
        using var body = new StringContent("test 123", Encoding.UTF8);
        using var response = await test.Client.PostAsync("/api/body-param-name-1p/", body);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_body_param_name_2p()
    {
        using var body = new StringContent("test ABC", Encoding.UTF8);
        using var response = await test.Client.PostAsync("/api/body-param-name-2p/?i=123", body);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("123 test ABC");
    }

    [Fact]
    public async Task Test_body_param_name_int()
    {
        using var body = new StringContent("123", Encoding.UTF8);
        using var response = await test.Client.PostAsync("/api/body-param-name-int", body);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("123");
    }
}