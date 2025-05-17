namespace NpgsqlRestTests;

public static partial class Database
{
    public static void RawResponseUsingParamsTests()
    {
        script.Append(
        """
        create function raw_response2() 
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
        comment on function raw_response2() is 'raw=true';

        create function raw_csv_response2() 
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
        comment on function raw_csv_response2() is '
        raw = true
        Content-Type: text/csv
        ';

        create function raw_csv_separators_response2() 
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
        comment on function raw_csv_separators_response2() is '
        raw = true
        raw_separator = ,
        new_line = \n
        Content-Type: text/csv
        ';

        create function raw_csv_separators_with_headers_response2() 
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
        comment on function raw_csv_separators_with_headers_response2() is '
        raw=true
        raw_separator=,
        raw_new_line=\n
        column_names=true
        Content-Type: text/csv
        ';
        """);
    }
}

[Collection("TestFixture")]
public class RawResponseUsingParamsTests(TestFixture test)
{
    [Fact]
    public async Task Test_raw_response2()
    {
        using var result = await test.Client.PostAsync("/api/raw-response2/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("1232024-01-01 00:00:00tsome text4562024-12-31 00:00:00fanother text");
    }

    [Fact]
    public async Task Test_raw_csv_response2()
    {
        using var result = await test.Client.PostAsync("/api/raw-csv-response2/", null);
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
    public async Task Test_raw_csv_separators_response2()
    {
        using var result = await test.Client.PostAsync("/api/raw-csv-separators-response2/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/csv");
        response.Should().Be(string.Concat(
            "123,\"2024-01-01 00:00:00\",t,\"some text\"",
            "\n",
            "456,\"2024-12-31 00:00:00\",f,\"another text\""));
    }

    [Fact]
    public async Task Test_raw_csv_separators_with_headers_response2()
    {
        using var result = await test.Client.PostAsync("/api/raw-csv-separators-with-headers-response2/", null);
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