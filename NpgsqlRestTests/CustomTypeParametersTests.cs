namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CustomTypeParametersTests()
    {
        script.Append(@"

create type custom_type1 as (value text);
        
create function get_custom_param_query_1p(_p custom_type1) 
returns text language sql as 'select _p.value';

");
    }
}

[Collection("TestFixture")]
public class CustomTypeParametersTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_custom_param_query_1p()
    {
        //var query = new QueryBuilder
        //{
        //    { "_p", "test 123" },
        //};
        //using var response = await test.Client.GetAsync($"/api/get-custom-param-query-1p/{query}");
        //var content = await response.Content.ReadAsStringAsync();

        //response?.StatusCode.Should().Be(HttpStatusCode.OK);
        //content.Should().Be("test 123");
    }
}