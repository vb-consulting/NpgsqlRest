namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudInsertTests()
    {
        script.Append("""
        create table crud_insert1 (
            id int,
            name text,
            some_date date not null,
            status boolean not null
        );

        create table crud_insert2 (
            id int,
            name text,
            some_date date not null,
            status boolean not null
        );

        create table crud_insert3 (
            id int not null generated always as identity,
            name text
        );

        create table crud_insert4 (
            name1 text null,
            name2 text not null
        );
        """);
    }
}

[Collection("TestFixture")]
public class CrudInsertTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_insert1()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"name1\",\"someDate\":\"2024-02-22\",\"status\":true}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert1/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-insert1/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id":1,"name":"name1","someDate":"2024-02-22","status":true}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_insert2_DateAndStatus()
    {
        using var body = new StringContent("{\"someDate\":\"2024-02-23\",\"status\":true}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert2/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-insert2/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id":null,"name":null,"someDate":"2024-02-23","status":true}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_insert3()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"name1\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert3/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-insert3/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id":1,"name":"name1"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_insert4()
    {
        using var body = new StringContent("{\"name1\":\"name1\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert4/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var body2 = new StringContent("{\"name1\":\"name1\",\"name2\":\"name2\"}", Encoding.UTF8, "application/json");
        using var response2 = await test.Client.PutAsync("/api/crud-insert4/", body2);
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-insert4/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"name1":"name1","name2":"name2"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }
}