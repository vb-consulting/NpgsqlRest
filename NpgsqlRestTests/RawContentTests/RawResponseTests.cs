namespace NpgsqlRestTests;

public static partial class Database
{
    public static void RawResponseTests()
    {
        script.Append(
        """
        create function raw_response1() 
        returns table(n numeric, d timestamp, b boolean, t text)
        language sql
        as 
        $$
        select * from (
        values 
            (123, '2024-01-01'::timestamp, true, 'some text'),
            (456, '2024-12-31'::timestamp, false, 'another text')
        )
        sub (n, d, b, t)
        $$;
        comment on function raw_response1() is 'raw';

        create function raw_csv_response1() 
        returns setof text
        language sql
        as 
        $$
        select trim(both '()' FROM sub::text) || E'\n' from (
        values 
            (123, '2024-01-01'::timestamp, true, 'some text'),
            (456, '2024-12-31'::timestamp, false, 'another text')
        )
        sub (n, d, b, t)
        $$;
        comment on function raw_csv_response1() is '
        raw
        Content-Type: text/csv
        ';

        create function raw_csv_separators_response1() 
        returns table(n numeric, d timestamp, b boolean, t text)
        language sql
        as 
        $$
        select sub.* 
        from (
        values 
            (123, '2024-01-01'::timestamp, true, 'some text'),
            (456, '2024-12-31'::timestamp, false, 'another text')
        )
        sub (n, d, b, t)
        $$;
        comment on function raw_csv_separators_response1() is '
        raw
        separator ,
        new_line \n
        Content-Type: text/csv
        ';

        create function raw_csv_separators_with_headers_response1() 
        returns table(n numeric, d timestamp, b boolean, t text)
        language sql
        as 
        $$
        select sub.* 
        from (
        values 
            (123, '2024-01-01'::timestamp, true, 'some text'),
            (456, '2024-12-31'::timestamp, false, 'another text')
        )
        sub (n, d, b, t)
        $$;
        comment on function raw_csv_separators_with_headers_response1() is '
        raw
        separator ,
        new_line \n
        column_names
        Content-Type: text/csv
        ';
        """);
    }
}

[Collection("TestFixture")]
public class RawResponseTests(TestFixture test)
{
    [Fact]
    public async Task Test_raw_response1()
    {
        using var result = await test.Client.PostAsync("/api/raw-response1/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("1232024-01-01 00:00:00tsome text4562024-12-31 00:00:00fanother text");
    }

    [Fact]
    public async Task Test_raw_csv_response1()
    {
        using var result = await test.Client.PostAsync("/api/raw-csv-response1/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/csv");
        response.Should().Be(string.Concat(
            "123,\"2024-01-01 00:00:00\",t,\"some text\"", 
            "\n",
            "456,\"2024-12-31 00:00:00\",f,\"another text\"",
            "\n"));
    }

    [Fact]
    public async Task Test_raw_csv_separators_response1()
    {
        using var result = await test.Client.PostAsync("/api/raw-csv-separators-response1/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/csv");
        response.Should().Be(string.Concat(
            "123,\"2024-01-01 00:00:00\",t,\"some text\"",
            "\n",
            "456,\"2024-12-31 00:00:00\",f,\"another text\""));
    }

    [Fact]
    public async Task Test_raw_csv_separators_with_headers_response1()
    {
        using var result = await test.Client.PostAsync("/api/raw-csv-separators-with-headers-response1/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/csv");
        response.Should().Be(string.Concat(
            "\"n\",\"d\",\"b\",\"t\"",
            "\n",
            "123,\"2024-01-01 00:00:00\",t,\"some text\"",
            "\n",
            "456,\"2024-12-31 00:00:00\",f,\"another text\""));
    }
}