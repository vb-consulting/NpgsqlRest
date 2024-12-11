namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudInsertOnConflictDoNothingReturningTests()
    {
        script.Append("""
        create table crud_insert_on_conflict_do_nothing_returning_test1 (
            id int not null primary key,
            name text
        );

        insert into crud_insert_on_conflict_do_nothing_returning_test1 (id, name) values (1, 'name1');

        create table crud_insert_on_conflict_do_nothing_returning_test2 (
            id int not null generated always as identity primary key,
            name text
        );
        
        insert into crud_insert_on_conflict_do_nothing_returning_test2 (id, name) overriding system value values (1, 'name1');

        create table crud_insert_on_conflict_do_nothing_returning_test3 (
            id1 int not null,
            id2 int not null,
            name text,
            primary key (id1, id2)
        );

        insert into crud_insert_on_conflict_do_nothing_returning_test3 (id1, id2, name) values (1, 1, 'name1');

        """);
    }
}

[Collection("TestFixture")]
public class CrudInsertOnConflictDoNothingReturningTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_insert_on_conflict_do_nothing_returning_test1()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"new name\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-nothing-returning-test1/on-conflict-do-nothing/returning/", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("[]");

        using var body2 = new StringContent("{\"id\":2,\"name\":\"new_name2\"}", Encoding.UTF8, "application/json");
        using var response2 = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-nothing-returning-test1/on-conflict-do-nothing/returning/", body2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadAsStringAsync();

        content2.Should().Be("""
            [
                {"id":2,"name":"new_name2"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_insert_on_conflict_do_nothing_returning_test2()
    {
        using var body = new StringContent("{\"id\":1,\"name\":\"new name\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-nothing-returning-test2/on-conflict-do-nothing/returning/", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("[]");

        using var body2 = new StringContent("{\"id\":2,\"name\":\"new_name2\"}", Encoding.UTF8, "application/json");
        using var response2 = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-nothing-returning-test2/on-conflict-do-nothing/returning/", body2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadAsStringAsync();

        content2.Should().Be("""
            [
                {"id":2,"name":"new_name2"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }

    [Fact]
    public async Task Test_crud_insert_on_conflict_do_nothing_returning_test3()
    {
        using var body = new StringContent("{\"id1\":1,\"id2\":1,\"name\":\"new name\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-nothing-returning-test3/on-conflict-do-nothing/returning/", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("[]");

        using var body2 = new StringContent("{\"id1\":1,\"id2\":2,\"name\":\"new_name2\"}", Encoding.UTF8, "application/json");
        using var response2 = await test.Client.PutAsync("/api/crud-insert-on-conflict-do-nothing-returning-test3/on-conflict-do-nothing/returning/", body2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadAsStringAsync();

        content2.Should().Be("""
            [
                {"id1":1,"id2":2,"name":"new_name2"}
            ]
            """
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());
    }
}