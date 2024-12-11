namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CrudTableTagCommentTests()
    {
        script.Append("""
        create table crud_commented_table (
            id int primary key,
            name text
        );

        comment on table crud_commented_table is 'This is a commented table
        for select
        HTTP GET /select_commented_table
        for update
        authorize
        for returning
        disabled
        for InsertOnConflictDoUpdate, InsertOnConflictDoNothing
        disabled
        ';

        create table crud_select_only (
            id int primary key,
            name text
        );

        comment on table crud_select_only is '
        disabled
        enabled select, delete
        ';
        """);
    }
}

[Collection("TestFixture")]
public class CrudTableTagCommentTests(TestFixture test)
{
    [Fact]
    public async Task Test_crud_commented_table()
    {
        using var select1 = await test.Client.GetAsync("/api/crud-commented-table/?id=1");
        select1.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var select2 = await test.Client.GetAsync("/select_commented_table/?id=1");
        select2.StatusCode.Should().Be(HttpStatusCode.OK);

        using var updateBody = new StringContent("{\"id\":1,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var update = await test.Client.PostAsync("/api/crud-commented-table/", updateBody);
        update.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var updateReturningBody = new StringContent("{\"id\":1,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var updateReturning = await test.Client.PostAsync("/api/crud-commented-table/returning/", updateReturningBody);
        updateReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var delete = await test.Client.DeleteAsync("/api/crud-commented-table/?id=1");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var deleteReturning = await test.Client.DeleteAsync("/api/crud-commented-table/returning/?id=1");
        deleteReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertBody = new StringContent("{\"id\":1,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insert = await test.Client.PutAsync("/api/crud-commented-table/", insertBody);
        insert.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var insertReturningBody = new StringContent("{\"id\":2,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertReturning = await test.Client.PutAsync("/api/crud-commented-table/returning/", insertReturningBody);
        insertReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoNothingBody = new StringContent("{\"id\":3,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoNothing = await test.Client.PutAsync("/api/crud-commented-table/on-conflict-do-nothing/", insertOnConflictDoNothingBody);
        insertOnConflictDoNothing.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoNothingReturningBody = new StringContent("{\"id\":4,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoNothingReturning = await test.Client.PutAsync("/api/crud-commented-table/on-conflict-do-nothing/returning/", insertOnConflictDoNothingReturningBody);
        insertOnConflictDoNothingReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoUpdateBody = new StringContent("{\"id\":5,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoUpdate = await test.Client.PutAsync("/api/crud-commented-table/on-conflict-do-update/", insertOnConflictDoUpdateBody);
        insertOnConflictDoUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoUpdateReturningBody = new StringContent("{\"id\":6,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoUpdateReturning = await test.Client.PutAsync("/api/crud-commented-table/on-conflict-do-update/returning/", insertOnConflictDoUpdateReturningBody);
        insertOnConflictDoUpdateReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_crud_select_only()
    {
        using var select = await test.Client.GetAsync("/api/crud-select-only/?id=1");
        select.StatusCode.Should().Be(HttpStatusCode.OK);

        using var updateBody = new StringContent("{\"id\":1,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var update = await test.Client.PostAsync("/api/crud-select-only/", updateBody);
        update.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var updateReturningBody = new StringContent("{\"id\":1,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var updateReturning = await test.Client.PostAsync("/api/crud-select-only/returning/", updateReturningBody);
        updateReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var delete = await test.Client.DeleteAsync("/api/crud-select-only/?id=1");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var deleteReturning = await test.Client.DeleteAsync("/api/crud-select-only/returning/?id=1");
        deleteReturning.StatusCode.Should().Be(HttpStatusCode.OK);

        using var insertBody = new StringContent("{\"id\":1,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insert = await test.Client.PutAsync("/api/crud-select-only/", insertBody);
        insert.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertReturningBody = new StringContent("{\"id\":2,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertReturning = await test.Client.PutAsync("/api/crud-select-only/returning/", insertReturningBody);
        insertReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoNothingBody = new StringContent("{\"id\":3,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoNothing = await test.Client.PutAsync("/api/crud-select-only/on-conflict-do-nothing/", insertOnConflictDoNothingBody);
        insertOnConflictDoNothing.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoNothingReturningBody = new StringContent("{\"id\":4,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoNothingReturning = await test.Client.PutAsync("/api/crud-select-only/on-conflict-do-nothing/returning/", insertOnConflictDoNothingReturningBody);
        insertOnConflictDoNothingReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoUpdateBody = new StringContent("{\"id\":5,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoUpdate = await test.Client.PutAsync("/api/crud-select-only/on-conflict-do-update/", insertOnConflictDoUpdateBody);
        insertOnConflictDoUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var insertOnConflictDoUpdateReturningBody = new StringContent("{\"id\":6,\"name\":\"some name\"}", Encoding.UTF8, "application/json");
        using var insertOnConflictDoUpdateReturning = await test.Client.PutAsync("/api/crud-select-only/on-conflict-do-update/returning/", insertOnConflictDoUpdateReturningBody);
        insertOnConflictDoUpdateReturning.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}