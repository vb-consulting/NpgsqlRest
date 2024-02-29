namespace NpgsqlRestTests;

public static partial class Database
{
    public static void NullHandlingTests()
    {
        script.Append(
            """
        create function get_null1() returns text language sql as 'select null';

        create function get_null2() returns text language sql as 'select null';
        comment on function get_null2() is 'response-null-handling emptystring';

        create function get_null3() returns text language sql as 'select null';
        comment on function get_null3() is 'response-null-handling nullliteral';

        create function get_null4() returns text language sql as 'select null';
        comment on function get_null4() is 'response-null-handling nocontent';

        create function get_nullable_param1(_t text) returns text language sql as 'select _t';
        comment on function get_nullable_param1(text) is 'response-null-handling nocontent';

        create function get_nullable_param2(_t text) returns text language sql as 'select _t';
        comment on function get_nullable_param2(text) is '
        response-null-handling nocontent
        query-string-null-handling ignore
        ';

        create function get_nullable_param3(_t text) returns text language sql as 'select _t';
        comment on function get_nullable_param3(text) is '
        response-null-handling nocontent
        query-string-null-handling EmptyString
        ';

        create function get_nullable_param4(_t text) returns text language sql as 'select _t';
        comment on function get_nullable_param4(text) is '
        response-null-handling nocontent
        query-string-null-handling NullLiteral
        ';
        """);
    }
}

[Collection("TestFixture")]
public class NullHandlingTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_null1()
    {
        using var response = await test.Client.GetAsync($"/api/get-null1/");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_get_null2()
    {
        using var response = await test.Client.GetAsync($"/api/get-null2/");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_get_null3()
    {
        using var response = await test.Client.GetAsync($"/api/get-null3/");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("null");
    }

    [Fact]
    public async Task Test_get_null4()
    {
        using var response = await test.Client.GetAsync($"/api/get-null4/");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Test_get_nullable_param1()
    {
        using var response = await test.Client.GetAsync($"/api/get-nullable-param1/?t=");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_get_nullable_param2()
    {
        using var response = await test.Client.GetAsync($"/api/get-nullable-param2/?t=");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_get_nullable_param3()
    {
        using var response = await test.Client.GetAsync($"/api/get-nullable-param3/?t=");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Test_get_nullable_param4()
    {
        using var response = await test.Client.GetAsync($"/api/get-nullable-param4/?t=null");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}