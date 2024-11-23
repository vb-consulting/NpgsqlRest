namespace NpgsqlRestTests;

public static partial class Database
{
    public static void QuotedJsonTests()
    {
        script.Append(
        """
        create function case_quoted_texts() 
        returns setof text 
        language sql
        as 
        $$
        select * from (values ('aaa'), ('a''a'), ('a"a'), ('a""a')) sub (a)
        $$;

        create function case_quoted_text_table() 
        returns table(t text)
        language sql
        as 
        $$
        select * from (values
            ('aaa'),
            ('a''a'),
            ('a"a'),
            ('a""a')
        ) sub (a)
        $$;
        """);
    }
}

[Collection("TestFixture")]
public class QuotedJsonTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_quoted_texts()
    {
        using var result = await test.Client.PostAsync("/api/case-quoted-texts/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[\"aaa\",\"a'a\",\"a\\\"a\",\"a\\\"\\\"a\"]");
    }

    [Fact]
    public async Task Test_case_quoted_text_table()
    {
        using var result = await test.Client.PostAsync("/api/case-quoted-text-table/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[{\"t\":\"aaa\"},{\"t\":\"a'a\"},{\"t\":\"a\\\"a\"},{\"t\":\"a\\\"\\\"a\"}]");
    }
}