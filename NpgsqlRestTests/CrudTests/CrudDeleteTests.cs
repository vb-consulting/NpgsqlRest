namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudDeleteTests()
    {
        script.Append("""
        create table crud_delete1 (
            id int,
            name text
        );
        insert into crud_delete1 values (1,'name1'),(2,'name2'),(3,'name3'),(4,'name4');

        create table crud_delete2 (
            id int,
            name text
        );
        insert into crud_delete2 values (1,'name1'),(2,'name2'),(3,'name3'),(4,'name4');
        """);
    }
}

[Collection("TestFixture")]
public class CrudDeleteTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_delete1()
    {
        using var response = await test.Client.DeleteAsync("/api/crud-delete1/?id=1");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-delete1/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id":2,"name":"name2"},
                {"id":3,"name":"name3"},
                {"id":4,"name":"name4"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());

        using var response2 = await test.Client.DeleteAsync("/api/crud-delete1/");
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse2 = await test.Client.GetAsync("/api/crud-delete1/");
        var content2 = await getResponse2.Content.ReadAsStringAsync();

        content2.Should().Be("[]");
    }

    [Fact]
    public async Task Test_crud_delete2_Returning()
    {
        using var response = await test.Client.DeleteAsync("/api/crud-delete2/returning/?id=1");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        content.Should().Be("""
            [
                {"id":1,"name":"name1"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());

        using var response2 = await test.Client.DeleteAsync("/api/crud-delete2/returning/");
        var content2 = await response2.Content.ReadAsStringAsync();
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        content2.Should().Be("""
            [
                {"id":2,"name":"name2"},
                {"id":3,"name":"name3"},
                {"id":4,"name":"name4"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }
}