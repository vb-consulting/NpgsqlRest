namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudInsertOnConflictDoUpdateTests()
    {
        script.Append("""
        create table crud_insert_on_conflict_do_update_test1 (
            id int not null primary key,
            name text
        );
        
        insert into crud_insert_on_conflict_do_update_test1 (id, name) values (1, 'name1');

        create table crud_insert_on_conflict_do_update_test2 (
            id int not null generated always as identity primary key,
            name text
        );
        
        insert into crud_insert_on_conflict_do_update_test2 (id, name) overriding system value values (1, 'name1');

        create table crud_insert_on_conflict_do_update_test3 (
            id1 int not null,
            id2 int not null,
            name text,
            primary key (id1, id2)
        );

        insert into crud_insert_on_conflict_do_update_test3 (id1, id2, name) values (1, 1, 'name1');

        """);
    }
}

[Collection("TestFixture")]
public class CrudInsertOnConflictDoUpdateTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_insert_on_conflict_do_update_test1()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"new_name\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-update-test1/on-conflict-do-update/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-insert-on-conflict-do-update-test1/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id":1,"name":"new_name"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());

        using var body2 = new StringContent("{\"id\":2,\"name\":\"new_name2\"}", Encoding.UTF8, "application/json");
        using var response2 = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-update-test1/on-conflict-do-update/", body2);
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse2 = await test.Client.GetAsync("/api/crud-insert-on-conflict-do-update-test1/");
        var content2 = await getResponse2.Content.ReadAsStringAsync();

        content2.Should().Be("""
            [
                {"id":1,"name":"new_name"},
                {"id":2,"name":"new_name2"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_insert_on_conflict_do_update_test2()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"new_name\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-update-test2/on-conflict-do-update/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-insert-on-conflict-do-update-test2/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id":1,"name":"new_name"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());

        using var body2 = new StringContent("{\"id\":2,\"name\":\"new_name2\"}", Encoding.UTF8, "application/json");
        using var response2 = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-update-test2/on-conflict-do-update/", body2);
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse2 = await test.Client.GetAsync("/api/crud-insert-on-conflict-do-update-test2/");
        var content2 = await getResponse2.Content.ReadAsStringAsync();

        content2.Should().Be("""
            [
                {"id":1,"name":"new_name"},
                {"id":2,"name":"new_name2"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_insert_on_conflict_do_update_test3()
    {
        using var body = new StringContent("{\"id1\":1,\"id2\":1,\"name\":\"new_name\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-update-test3/on-conflict-do-update/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/crud-insert-on-conflict-do-update-test3/");
        var content = await getResponse.Content.ReadAsStringAsync();

        content.Should().Be("""
            [
                {"id1":1,"id2":1,"name":"new_name"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());

        using var body2 = new StringContent("{\"id1\":1,\"id2\":2,\"name\":\"new_name2\"}", Encoding.UTF8, "application/json");
        using var response2 = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-update-test3/on-conflict-do-update/", body2);
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse2 = await test.Client.GetAsync("/api/crud-insert-on-conflict-do-update-test3/");
        var content2 = await getResponse2.Content.ReadAsStringAsync();

        content2.Should().Be("""
            [
                {"id1":1,"id2":1,"name":"new_name"},
                {"id1":1,"id2":2,"name":"new_name2"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }
}