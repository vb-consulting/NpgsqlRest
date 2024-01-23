namespace NpgsqlRestTests;

public static partial class Database
{
    public static void SetofRecordTests()
    {
        script.Append(@"
create function get_test_records() 
returns setof record
language sql
as 
$$
select * from (values (1, 'one'), (2, 'two'), (3, 'three')) v(id, name);
$$;
");
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
        response?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[\"1,one\",\"2,two\",\"3,three\"]");
    }
}
