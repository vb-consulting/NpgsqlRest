namespace NpgsqlRestTests;

public static partial class Database
{
    public static void TimestampTests()
    {
        script.Append(@"
create function get_timestamp() returns timestamp language sql as 
$$
select '2021-01-31 12:34:56.789'::timestamp;
$$;

create function get_timestamp_array() returns timestamp[] language sql as 
$$
select array['2021-01-30 12:34:56.789'::timestamp, '2021-01-31 12:34:56.789'::timestamp];
$$;

create function get_timestamp_setof() returns setof timestamp language sql as 
$$
select '2021-01-30 12:34:56.789'::timestamp
union
select '2021-01-31 12:34:56.789'::timestamp;
$$;

create function get_timestamp_table() returns table (ts timestamp) language sql as 
$$
select '2021-01-30 12:34:56.789'::timestamp
union
select '2021-01-31 12:34:56.789'::timestamp;
$$;
");
    }
}

[Collection("TestFixture")]
public class TimestampTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_timestamp()
    {
        using var response = await test.Client.GetAsync("/api/get-timestamp");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        content.Should().Be("2021-01-31 12:34:56.789");
    }
    
    [Fact]
    public async Task Test_get_timestamp_array()
    {
        using var response = await test.Client.GetAsync("/api/get-timestamp-array");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        content.Should().Be("[\"2021-01-30T12:34:56.789\",\"2021-01-31T12:34:56.789\"]");
    }
    
    [Fact]
    public async Task Test_get_timestamp_setof()
    {
        using var response = await test.Client.GetAsync("/api/get-timestamp-setof");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        content.Should().Be("[\"2021-01-30T12:34:56.789\",\"2021-01-31T12:34:56.789\"]");
    }
    
    [Fact]
    public async Task Test_get_timestamp_table()
    {
        using var response = await test.Client.GetAsync("/api/get-timestamp-table");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        content.Should().Be("[{\"ts\":\"2021-01-30T12:34:56.789\"},{\"ts\":\"2021-01-31T12:34:56.789\"}]");
    }
}