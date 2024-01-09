namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnSetOfTextTests()
    {
        script.Append(@"
create function case_return_setof_text() 
returns setof text 
language plpgsql
as 
$$
begin
    return query select i from (values ('ABC'), ('XYZ'), ('IJN')) t(i);
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnSetOfTextTests(TestFixture test)
{
    [Fact]
    public async Task Test_CaseReturnSetOfText()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-text/", null);

        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("[\"ABC\",\"XYZ\",\"IJN\"]");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}