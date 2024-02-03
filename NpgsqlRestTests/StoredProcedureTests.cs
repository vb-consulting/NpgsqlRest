namespace NpgsqlRestTests;

public static partial class Database
{
    public static void StoredProcedureTests()
    {
        script.Append(@"
create table proc_test_tbl(i int, t text);
insert into proc_test_tbl values (1, 'X');

create procedure proc_test(_t text)
language plpgsql
as 
$$
begin
    update proc_test_tbl set t = _t where i = 1;
end;
$$;

create function get_proc_test_tbl() returns text language sql as 'select t from proc_test_tbl where i = 1';
");
    }
}

[Collection("TestFixture")]
public class StoredProcedureTests(TestFixture test)
{
    [Fact]
    public async Task Test_proc_test()
    {
        using var body = new StringContent("{\"t\": \"YYY\"}", Encoding.UTF8);
        using var response = await test.Client.PostAsync("/api/proc-test/", body);
        response?.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getResponse = await test.Client.GetAsync("/api/get-proc-test-tbl/");
        var getContent = await getResponse.Content.ReadAsStringAsync();

        getResponse?.StatusCode.Should().Be(HttpStatusCode.OK);
        getContent.Should().Be("YYY");
    }
}