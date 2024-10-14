namespace NpgsqlRestTests;

public static partial class Database
{
    public static void MixinTests()
    {
        script.Append(@"
        create schema mixins;

        create table mixins.id (
            id int generated always as identity primary key
        );

        create table mixin_users (
            like mixins.id including all,
            name text not null  
        );

        insert into mixin_users (id, name) 
        overriding system value values 
        (1, 'test1'), (2, 'test2');

        create table mixins.audit (
            created_by int not null references mixin_users(id),
            modified_by int not null references mixin_users(id),
            created_at timestamptz not null default now(),
            modified_at timestamptz not null default now()
        );

        create table test_mixins (
            like mixins.id including all,
            like mixins.audit including all
        );

        insert into test_mixins (id, created_by, modified_by, created_at, modified_at) 
        overriding system value 
        values (1, 1, 1, '2024-01-01', '2024-01-01'), 
               (2, 2, 2, '2024-01-02', '2024-01-02');

        create or replace function get_audit_mixins_test(
            _p mixins.id
        )
        returns table (
            result mixins.audit
        )
        language sql as 
        $$
        select row(t.*)::mixins.audit
        from (
            select created_by, modified_by, created_at, modified_at from test_mixins
        ) t;
        $$;
");
    }
}

[Collection("TestFixture")]
public class MixinTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_audit_mixins_test()
    {
        using var response = await test.Client.GetAsync($"/api/get-audit-mixins-test?pId=1");
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[{\"createdBy\":1,\"modifiedBy\":1,\"createdAt\":\"2024-01-01T00:00:00+00\",\"modifiedAt\":\"2024-01-01T00:00:00+00\"},{\"createdBy\":2,\"modifiedBy\":2,\"createdAt\":\"2024-01-02T00:00:00+00\",\"modifiedAt\":\"2024-01-02T00:00:00+00\"}]");
    }
}