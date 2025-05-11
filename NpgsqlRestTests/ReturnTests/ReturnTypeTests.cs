
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

        create function get_test_return_type() 
        returns test_return_type 
        language sql as 'select row(1, ''test1'', ''test2'')::test_return_type';

        create function get_setof_test_return_types() 
        returns setof test_return_type 
        language sql as 'select row(1, ''test1'', ''test2'')::test_return_type union all select row(2, ''test3'', ''test4'')::test_return_type';

        create or replace function get_test_return_types_table()
        returns table (
            t test_return_type
        )
        language sql as 
        $$
        select row(1, 'test1', 'test2')::test_return_type 
        union all 
        select row(2, 'test3', 'test4')::test_return_type
        $$;

        create or replace function get_test_return_types_table2()
        returns table (
            t1 text,
            t2 test_return_type
        )
        language sql as 
        $$
        select 'text1', row(1, 'test1', 'test2')::test_return_type 
        union all 
        select 'text2', row(2, 'test3', 'test4')::test_return_type
        $$;

        create or replace function get_test_return_types_table3()
        returns table (
            t1 test_return_type,
            t2 text
        )
        language sql as 
        $$
        select row(1, 'test1', 'test2')::test_return_type, 'text1'
        union all 
        select row(2, 'test3', 'test4')::test_return_type, 'text2'
        $$;

        create or replace function get_test_return_types_table4()
        returns table (
            t1 text,
            t2 test_return_type,
            t3 text
        )
        language sql as 
        $$
        select 'text1', row(1, 'test1', 'test2')::test_return_type, 'text2'
        union all 
        select 'text3', row(2, 'test3', 'test4')::test_return_type, 'text4'
        $$;
");
    }
}

[Collection("TestFixture")]
public class ReturnSetTypeTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_test_return_type()
    {
        using var response = await test.Client.GetAsync($"/api/get-test-return-type");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"}");
    }

    [Fact]
    public async Task Test_get_setof_test_return_types()
    {
        using var response = await test.Client.GetAsync($"/api/get-setof-test-return-types");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"},{\"id\":2,\"value\":\"test3\",\"value2\":\"test4\"}]");
    }

    [Fact]
    public async Task Test_get_test_return_types_table()
    {
        using var response = await test.Client.GetAsync($"/api/get-test-return-types-table");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"},{\"id\":2,\"value\":\"test3\",\"value2\":\"test4\"}]");
    }

    [Fact]
    public async Task Test_get_test_return_types_table2()
    {
        using var response = await test.Client.GetAsync($"/api/get-test-return-types-table2");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"t1\":\"text1\",\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"},{\"t1\":\"text2\",\"id\":2,\"value\":\"test3\",\"value2\":\"test4\"}]");
    }

    [Fact]
    public async Task Test_get_test_return_types_table3()
    {
        using var response = await test.Client.GetAsync($"/api/get-test-return-types-table3");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\",\"t2\":\"text1\"},{\"id\":2,\"value\":\"test3\",\"value2\":\"test4\",\"t2\":\"text2\"}]");
    }

    [Fact]
    public async Task Test_get_test_return_types_table4()
    {
        using var response = await test.Client.GetAsync($"/api/get-test-return-types-table4");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"t1\":\"text1\",\"id\":1,\"value\":\"test1\",\"value2\":\"test2\",\"t3\":\"text2\"},{\"t1\":\"text3\",\"id\":2,\"value\":\"test3\",\"value2\":\"test4\",\"t3\":\"text4\"}]");
    }
}
