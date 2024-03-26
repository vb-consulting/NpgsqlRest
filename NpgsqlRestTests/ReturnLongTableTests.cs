#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnLongTableTests()
    {
        script.Append("""
        create function case_get_long_table1(_records int) 
        returns table (
            int_field int, 
            text_field text
        )
        language sql
        as 
        $_$
        select
            i, i::text
        from
            generate_series(1, _records) as i;
        $_$;
        """);
    }
}


[Collection("TestFixture")]
public class ReturnLongTableTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_get_long_table1_return10()
    {
        using var result = await test.Client.GetAsync("/api/case-get-long-table1/?records=10");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");

        var array = JsonNode.Parse(response).AsArray();
        array.Count.Should().Be(10);
    }

    [Fact]
    public async Task Test_case_get_long_table1_return25()
    {
        using var result = await test.Client.GetAsync("/api/case-get-long-table1/?records=25");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");

        var array = JsonNode.Parse(response).AsArray();
        array.Count.Should().Be(25);
    }

    [Fact]
    public async Task Test_case_get_long_table1_return75()
    {
        using var result = await test.Client.GetAsync("/api/case-get-long-table1/?records=75");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");

        var array = JsonNode.Parse(response).AsArray();
        array.Count.Should().Be(75);
    }

    [Fact]
    public async Task Test_case_get_long_table1_return0()
    {
        using var result = await test.Client.GetAsync("/api/case-get-long-table1/?records=0");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");

        var array = JsonNode.Parse(response).AsArray();
        array.Count.Should().Be(0);
    }
}
