namespace NpgsqlRestTests;

public static partial class Database
{
    public static void SetofRecordTests()
    {
        script.Append("""""
create function get_test_records() 
returns setof record
language sql
as 
$$
select * from (
    values 
    (1, true, 'one'), 
    (2, false, 'two'), 
    (3, null, 'three'),
    (null, true, 'foo,bar'),
    (null, null, 'foo"bar'),
    (null, null, null),
    (1, null, 't'),
    (1, true, null),
    (null, null, '"foo"bar"'),
    (null, null, 'foo""bar'),
    (null, null, 'foo""""bar'),
    (null, null, 'foo"",""bar'),
    (null, null, 'foo\bar'),
    (null, null, 'foo/bar'),
    (null, null, E'foo\nbar')
) v;
$$;
""""");
    }
}


[Collection("TestFixture")]
public class SetofRecordTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_test_records()
    {
        using var response = await test.Client.GetAsync("/api/get-test-records");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var expextedContent = """
        [
            ["1","t","one"],
            ["2","f","two"],
            ["3",null,"three"],
            [null,"t","foo,bar"],
            [null,null,"foo\"bar"],
            [null,null,null],
            ["1",null,"t"],
            ["1","t",null],
            [null,null,"\"foo\"bar\""],
            [null,null,"foo\"\"bar"],
            [null,null,"foo\"\"\"\"bar"],
            [null,null,"foo\"\",\"\"bar"],
            [null,null,"foo\\bar"],
            [null,null,"foo/bar"],
            [null,null,"foo\nbar"]
        ]
        """
        .Replace(" ", "")
        .Replace("\r", "")
        .Replace("\n", "");

        content.Should().Be(expextedContent);
    }
}
