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

        create or replace function test_return_mixed_table_records_3()
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

        create or replace function test_return_mixed_table_records_4()
        returns table (
            value1 text,
            value2 test_return_table,
            value3 text
        )
        language sql as 
        $$
        select 
            'test1',
            row(t.*)::test_return_table,
            'test2' 
        from 
            test_return_table t;
        $$;

        create table my_table (
            id int,
            text_value text,
            flag boolean
        );

        create function my_table_service(
            request my_table
        )
        returns void
        language plpgsql as 
        $$
        begin
            raise info 'id: %, text_value: %, flag: %', request.id, request.text_value, request.flag;
        end;
        $$;

        create function get_my_table_service(
            request my_table
        )
        returns my_table
        language sql as 
        $$
        select request;
        $$;

        create function get_setof_my_tables(
            request my_table
        )
        returns setof my_table
        language sql as 
        $$
        select request union all select request;
        $$;

        create function get_table_of_my_tables(
            request my_table
        )
        returns table (
            req my_table
        )
        language sql as 
        $$
        select request union all select request;
        $$;

        create function get_mixed_table_of_my_tables(
            request my_table
        )
        returns table (
            a text,
            req my_table,
            b text
        )
        language sql as 
        $$
        select 'a1', request, 'b1'
        union all 
        select 'a2', request, 'b2'
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

    [Fact]
    public async Task Test_test_return_mixed_table_records_3()
    {
        using var response = await test.Client.PostAsync($"/api/test-return-mixed-table-records-3", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"value1\":\"test1\",\"id\":1,\"value\":\"test1\",\"value2\":\"test2\"},{\"value1\":\"test1\",\"id\":2,\"value\":\"test3\",\"value2\":\"test4\"}]");
    }

    [Fact]
    public async Task Test_test_return_mixed_table_records_4()
    {
        using var response = await test.Client.PostAsync($"/api/test-return-mixed-table-records-4", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"value1\":\"test1\",\"id\":1,\"value\":\"test1\",\"value2\":\"test2\",\"value3\":\"test2\"},{\"value1\":\"test1\",\"id\":2,\"value\":\"test3\",\"value2\":\"test4\",\"value3\":\"test2\"}]");
    }

    [Fact]
    public async Task Test_my_table_service()
    {
        using var body = new StringContent("""
        {  
            "requestId": 1,
            "requestTextValue": "test",
            "requestFlag": true
        }
        """, Encoding.UTF8, "application/json");

        using var response = await test.Client.PostAsync("/api/my-table-service", body);
        response?.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Test_get_my_table_service()
    {
        var query = new QueryBuilder
        {
            { "requestId", "1" },
            { "requestTextValue", "test" },
            { "requestFlag", "true" },
        };
        using var response = await test.Client.GetAsync($"/api/get-my-table-service/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("{\"id\":1,\"textValue\":\"test\",\"flag\":true}");
    }

    [Fact]
    public async Task Test_get_setof_my_tables()
    {
        var query = new QueryBuilder
        {
            { "requestId", "1" },
            { "requestTextValue", "test" },
            { "requestFlag", "true" },
        };
        using var response = await test.Client.GetAsync($"/api/get-setof-my-tables/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("[{\"id\":1,\"textValue\":\"test\",\"flag\":true},{\"id\":1,\"textValue\":\"test\",\"flag\":true}]");
    }

    [Fact]
    public async Task Test_get_table_of_my_tables()
    {
        var query = new QueryBuilder
        {
            { "requestId", "1" },
            { "requestTextValue", "test" },
            { "requestFlag", "true" },
        };
        using var response = await test.Client.GetAsync($"/api/get-table-of-my-tables/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("[{\"id\":1,\"textValue\":\"test\",\"flag\":true},{\"id\":1,\"textValue\":\"test\",\"flag\":true}]");
    }

    [Fact]
    public async Task Test_get_mixed_table_of_my_tables()
    {
        var query = new QueryBuilder
        {
            { "requestId", "1" },
            { "requestTextValue", "test" },
            { "requestFlag", "true" },
        };
        using var response = await test.Client.GetAsync($"/api/get-mixed-table-of-my-tables/{query}");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("[{\"a\":\"a1\",\"id\":1,\"textValue\":\"test\",\"flag\":true,\"b\":\"b1\"},{\"a\":\"a2\",\"id\":1,\"textValue\":\"test\",\"flag\":true,\"b\":\"b2\"}]");
    }
}