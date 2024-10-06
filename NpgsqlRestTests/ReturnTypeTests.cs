/*
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnSetTypeTests()
    {
        script.Append(@"
        create type test_return_type as (
            id int,
            value text,
            value2 text
        );

        create function test_return_type() 
        returns test_return_type 
        language sql as 'select row(1, ''test1'', ''test2'')::test_return_type';

        create function test_return_setof_type() 
        returns setof test_return_type 
        language sql as 'select row(1, ''test1'', ''test2'')::test_return_type union all select row(2, ''test3'', ''test4'')::test_return_type';
");
    }
}

[Collection("TestFixture")]
public class ReturnSetTypeTests(TestFixture test)
{
    [Fact]
    public async Task Test_test_return_type()
    {
        using var response = await test.Client.PostAsync($"/api/test-return-type", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"}");
    }
}
*/