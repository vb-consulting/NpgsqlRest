namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudUpdateReturningTests()
    {
        script.Append("""
        create table crud_update_returning_tests (
            id serial primary key,
            name text not null,
            some_date date not null,
            status boolean not null
        );

        insert into crud_update_returning_tests
        (id, name, some_date, status)
        values
        (1, 'name1', '2024-01-01', true),
        (2, 'name2', '2024-01-20', false),
        (3, 'name3', '2024-01-25', true);
        """);
    }
}

[Collection("TestFixture")]
public class CrudUpdateReturningTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_update_returning_tests()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-returning-tests/returning/", body);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        content.Should().Be("""
            [
                {"id":1,"name":"updated1","someDate":"2024-01-01","status":false}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_update_returning_tests_NoneExistingParam()
    {
        using var body = new StringContent("{\"bla\":1,\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-returning-tests/returning/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_crud_update_returning_tests_NoKeys()
    {
        using var body = new StringContent("{\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-returning-tests/returning/", body);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Test_crud_update_returning_tests_NoFields()
    {
        using var body = new StringContent("{\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-returning-tests/returning/", body);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}