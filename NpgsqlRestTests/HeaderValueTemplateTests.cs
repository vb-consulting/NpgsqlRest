namespace NpgsqlRestTests;

public static partial class Database
{
    public static void HeaderValueTemplateTests()
    {
        script.Append(
        """
        create function header_template_response1(_type text, _file text) 
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

        comment on function header_template_response1(text, text) is '
        raw
        separator ,
        newline \n
        Content-Type: {_type}
        Content-Disposition: attachment; filename={_file}
        ';

        create function header_template_response2(_type text, _file text) 
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
        
        comment on function header_template_response2(text, text) is '
        raw
        separator ,
        newline \n
        Content-Type: {type}
        Content-Disposition: attachment; filename={file}
        ';
        """);
    }
}

[Collection("TestFixture")]
public class HeaderValueTemplateTests(TestFixture test)
{
    [Fact]
    public async Task Test_header_template_response1()
    {
        string body = """
        {  
            "type": "text/csv",
            "file": "test.csv"
        }
        """;
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var result = await test.Client.PostAsync("/api/header-template-response1/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        result.Content.Headers.ContentType.MediaType.Should().Be("text/csv");
        result.Content.Headers.ContentDisposition.FileName.Should().Be("test.csv");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        response.Should().Be(string.Concat(
            "123,\"2024-01-01 00:00:00\",t,\"some text\"",
            "\n",
            "456,\"2024-12-31 00:00:00\",f,\"another text\""));
    }

    [Fact]
    public async Task Test_header_template_response2()
    {
        string body = """
        {  
            "type": "text/csv",
            "file": "test.csv"
        }
        """;
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var result = await test.Client.PostAsync("/api/header-template-response2/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        result.Content.Headers.ContentType.MediaType.Should().Be("text/csv");
        result.Content.Headers.ContentDisposition.FileName.Should().Be("test.csv");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        response.Should().Be(string.Concat(
            "123,\"2024-01-01 00:00:00\",t,\"some text\"",
            "\n",
            "456,\"2024-12-31 00:00:00\",f,\"another text\""));
    }
}