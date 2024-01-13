#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnTableTests()
    {
        script.Append(@"
create function case_return_table1() 
returns table (
    int_field int, 
    text_field text, 
    bool_field bool
)
language plpgsql
as 
$$
begin
    return query 
    select t.*
    from (
        values 
            (1, 'ABC', null), 
            (2, null, false), 
            (null, 'IJN', true)
    ) t(int_field, text_field, bool_field);
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnTableTests(TestFixture test)
{
    [Fact]
    public async Task Test_CaseReturnSetOfText()
    {
        using var result = await test.Client.PostAsync("/api/case-return-table1/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");

        var array = JsonNode.Parse(response).AsArray();

        array.Count.Should().Be(3);

        array[0]["intField"].ToJsonString().Should().Be("1");
        array[0]["textField"].GetValue<string>().Should().Be("ABC");
        array[0]["boolField"].Should().BeNull();

        array[1]["intField"].ToJsonString().Should().Be("2");
        array[1]["textField"].Should().BeNull();
        array[1]["boolField"].ToJsonString().Should().Be("false");

        array[2]["intField"].Should().BeNull();
        array[2]["textField"].GetValue<string>().Should().Be("IJN");
        array[2]["boolField"].ToJsonString().Should().Be("true");
    }
}