namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CustomTypeParametersTests()
    {
        script.Append(@"

        create type custom_type1 as (value text);
        
        create function get_custom_param_query_1p(
            _i int, 
            _p custom_type1, 
            _t text
        ) 
        returns text language sql as 'select _p.value';

        create function get_custom_param_query_2p(
            _p custom_type1
        ) 
        returns text language sql as 'select _p.value';

        create function get_custom_param_query_3p(
            _i int, 
            _t text,
            _p custom_type1
        ) 
        returns text language sql as 'select _p.value';

        create type custom_type2 as (value1 int, value2 text, value3 bool);

        create function get_custom_param_query_4p(
            _p custom_type2
        ) 
        returns text language sql as 'select _p.value2';

        create function get_custom_param_query_5p(
            _p1 custom_type1,
            _p2 custom_type2
        ) 
        returns text language sql as 'select _p2.value2';

        create function get_custom_param_query_6p(
            _p1 int,
            _p2 custom_type1,
            _p3 custom_type2
        ) 
        returns text language sql as 'select _p1::text || _p3.value2';

        create function get_custom_param_query_7p(
            _p2 custom_type1,
            _p1 int,
            _p3 custom_type2
        ) 
        returns text language sql as 'select _p1::text || _p3.value2';

        create function get_custom_param_query_8p(
            _p0 text,
            _p2 custom_type1,
            _p1 int,
            _p3 custom_type2,
            _p4 text
        ) 
        returns text language sql as 'select _p1::text || _p3.value2';


        create function get_custom_param_query_9p(
            _p2 custom_type1,
                _p3 custom_type2,
            _p0 text,
            _p1 int,
            _p4 text
        ) 
        returns text language sql as 'select _p1::text || _p3.value2';

");
    }
}

[Collection("TestFixture")]
public class CustomTypeParametersTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_custom_param_query_1p()
    {
        var query = new QueryBuilder
        {
            { "i", "1" },
            { "p.value", "test 123" },
            { "t", "test XYZ" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-1p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_1p_2()
    {
        var query = new QueryBuilder
        {
            { "p.value", "test 123" },
            { "i", "1" },
            { "t", "test XYZ" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-1p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_2p()
    {
        var query = new QueryBuilder
        {
            { "p.value", "test 123" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-2p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_3p()
    {
        var query = new QueryBuilder
        {
            { "p.value", "test 123" },
            { "i", "1" },
            { "t", "test XYZ" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-3p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_4p()
    {
        var query = new QueryBuilder
        {
            { "p.value1", "1" },
            { "p.value2", "test 123" },
            { "p.value3", "true" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-4p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_5p()
    {
        var query = new QueryBuilder
        {
            { "p2.value1", "1" },
            { "p2.value2", "test 123" },
            { "p2.value3", "true" },
            { "p1.value", "test XYZ" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-5p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_5p_2()
    {
        var query = new QueryBuilder
        {
            { "p2.value1", "1" },
            { "p1.value", "test XYZ" },
            { "p2.value3", "true" },
            { "p2.value2", "test 123" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-5p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_6p()
    {
        var query = new QueryBuilder
        {
            { "p1", "1" },
            { "p2.value", "test XYZ" },
            { "p3.value1", "1" },
            { "p3.value2", "test 123" },
            { "p3.value3", "true" },
            
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-6p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("1test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_6p_2()
    {
        var query = new QueryBuilder
        {
            { "p2.value", "test XYZ" },
            { "p3.value1", "1" },
            { "p1", "1" },
            { "p3.value2", "test 123" },
            { "p3.value3", "true" },

        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-6p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("1test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_7p()
    {
        var query = new QueryBuilder
        {
            { "p1", "1" },
            { "p2.value", "test XYZ" },
            { "p3.value1", "1" },
            { "p3.value2", "test 123" },
            { "p3.value3", "true" },

        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-7p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("1test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_8p()
    {
        var query = new QueryBuilder
        {
            { "p0", "AAA" },
            { "p1", "1" },
            { "p2.value", "test XYZ" },
            { "p3.value1", "1" },
            { "p3.value2", "test 123" },
            { "p3.value3", "true" },
            { "p4", "BBB" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-8p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("1test 123");
    }

    [Fact]
    public async Task Test_get_custom_param_query_9p()
    {
        var query = new QueryBuilder
        {
            { "p0", "AAA" },
            { "p1", "1" },
            { "p2.value", "test XYZ" },
            { "p3.value1", "1" },
            { "p3.value2", "test 123" },
            { "p3.value3", "true" },
            { "p4", "BBB" },
        };
        using var response = await test.Client.GetAsync($"/api/get-custom-param-query-9p/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("1test 123");
    }
}