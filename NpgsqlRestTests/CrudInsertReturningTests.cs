namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudInsertReturningTests()
    {
        script.Append("""
        create table crud_insert_returning1 (
            id int not null generated always as identity,
            name text
        );
        """);
    }
}

[Collection("TestFixture")]
public class CrudInsertReturningTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_insert_returning1_Name()
    {
        using var body = new StringContent("{\"name\":\"inserted1\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert-returning1/returning/", body);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        content.Should().Be("""
            [
                {"id":1,"name":"inserted1"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }
}