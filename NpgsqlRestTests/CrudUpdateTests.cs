namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudUpdateTests()
    {
        script.Append("""
        create table crud_update_tests (
            id serial primary key,
            name text not null,
            some_date date not null,
            status boolean not null
        );

        insert into crud_update_tests
        (id, name, some_date, status)
        values
        (1, 'name1', '2024-01-01', true),
        (2, 'name2', '2024-01-20', false),
        (3, 'name3', '2024-01-25', true);
        """);
    }
}

[Collection("TestFixture")]
public class CrudUpdateTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_update_tests()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-tests/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);


        using var getResponse = await test.Client.GetAsync("/api/crud-update-tests/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id":2,"name":"name2","someDate":"2024-01-20","status":false},
                {"id":3,"name":"name3","someDate":"2024-01-25","status":true},
                {"id":1,"name":"updated1","someDate":"2024-01-01","status":false}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_update_tests_NoneExistingParam()
    {
        using var body = new StringContent("{\"bla\":1,\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-tests/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_crud_update_tests_NoKeys()
    {
        using var body = new StringContent("{\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-tests/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_crud_update_tests_NoFields()
    {
        using var body = new StringContent("{\"name\":\"updated1\",\"status\":false}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/crud-update-tests/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}