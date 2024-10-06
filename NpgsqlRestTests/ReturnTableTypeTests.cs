namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnTableTypeTests()
    {
        script.Append(@"

        create table test_return_table(
            id int generated always as identity primary key,
            value text,
            value2 text
        );

        insert into test_return_table
        (value, value2) values('test1', 'test2'), 
        ('test3', 'test4');

        create function test_return_table_record() 
        returns test_return_table 
        language sql as 'select * from test_return_table where id = 1';

        create function test_return_all_table_records() 
        returns setof test_return_table 
        language sql as 'select * from test_return_table';

        create or replace function test_return_mixed_table_records()
        returns table (
            value1 text,
            value2 test_return_table
        )
        language sql as 
        $$
        select 
            'test1',
            row(t.*)::test_return_table
        from 
            test_return_table t;
        $$;

        create or replace function test_return_mixed_table_records_2()
        returns table (
            value1 test_return_table,
            value2 text
        )
        language sql as 
        $$
        select 
            row(t.*)::test_return_table,
            'test1'
        from 
            test_return_table t;
        $$;
");
    }
}

[Collection("TestFixture")]
public class ReturnTableTypeTests(TestFixture test)
{
    [Fact]
    public async Task Test_test_return_table_record()
    {
        using var response = await test.Client.PostAsync($"/api/test-return-table-record", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"}");
    }

    [Fact]
    public async Task Test_test_return_all_table_records()
    {
        using var response = await test.Client.PostAsync($"/api/test-return-all-table-records", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"},{\"id\":2,\"value\":\"test3\",\"value2\":\"test4\"}]");
    }

    [Fact]
    public async Task Test_test_return_mixed_table_records()
    {
        using var response = await test.Client.PostAsync($"/api/test-return-mixed-table-records", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"value1\":\"test1\",\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"},{\"value1\":\"test1\",\"id\":2,\"value\":\"test3\",\"value2\":\"test4\"}]");
    }

    [Fact]
    public async Task Test_test_return_mixed_table_records_2()
    {
        using var response = await test.Client.PostAsync($"/api/test-return-mixed-table-records-2", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"id\":1,\"value\":\"test1\",\"value2\":\"test2\",\"value2\":\"test1\"},{\"id\":2,\"value\":\"test3\",\"value2\":\"test4\",\"value2\":\"test1\"}]");
    }
}