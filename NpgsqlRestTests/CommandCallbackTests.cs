namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CommandCallbackTests()
    {
        script.Append(@"
create function get_csv_data() 
returns table (id int, name text, date timestamp, status boolean)
language sql as 
$$
select * from (
    values 
    (1, 'foo', '2024-01-31'::timestamp, true), 
    (2, 'bar', '2024-01-29'::timestamp, true), 
    (3, 'xyz', '2024-01-25'::timestamp, false)
) t;
$$;
");
    }
}

[Collection("TestFixture")]
public class CommandCallbackTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_csv_data()
    {
        using var response = await test.Client.GetAsync("/api/get-csv-data/");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        content.Should().Be(string.Join('\n', [
            "1,foo,2024-01-31T00:00:00,true",
            "2,bar,2024-01-29T00:00:00,true",
            "3,xyz,2024-01-25T00:00:00,false",
            ""
        ]));
    }
}