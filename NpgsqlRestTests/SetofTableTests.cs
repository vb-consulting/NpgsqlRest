namespace NpgsqlRestTests;

public static partial class Database
{
    public static void SetofTableTests()
    {
        script.Append(@"
create table test_table (
    id int,
    name text not null
);

insert into test_table values (1, 'one'), (2, 'two'), (3, 'three');

create function get_test_table() 
returns setof test_table
language sql
as 
$$
select * from test_table;
$$;
");
    }
}

[Collection("TestFixture")]
public class SetofTableTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_test_table()
    {
        using var response = await test.Client.GetAsync("/api/get-test-table");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"id\":1,\"name\":\"one\"},{\"id\":2,\"name\":\"two\"},{\"id\":3,\"name\":\"three\"}]");
    }
}