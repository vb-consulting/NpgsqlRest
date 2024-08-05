namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CommentResponseHeadersTests()
    {
        script.Append(@"
create function hello_world_html() returns text language sql as 'select ''<div>Hello World</div>''';
comment on function hello_world_html() is '
HTTP GET
Content-Type: text/html';

create function comment_response_headers() returns text language sql as 'select ''comment_response_headers''';
comment on function comment_response_headers() is '
HTTP
Content-Type: application/json
CustomHeader: test
MultiValueCustomHeader: test1
MultiValueCustomHeader: test2
cache-control: no-store
';
");
    }
}

[Collection("TestFixture")]
public class CommentResponseHeadersTests(TestFixture test)
{
    [Fact]
    public async Task Test_hello_world_html()
    {
        using var result = await test.Client.GetAsync("/api/hello-world-html/");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/html");
        response.Should().Be("<div>Hello World</div>");
    }

    [Fact]
    public async Task Test_comment_response_headers()
    {
        using var response = await test.Client.PostAsync("/api/comment-response-headers/", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("comment_response_headers");

        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        // Note: test client does not support custom headers check out headers in the http client, following headers should be present:
        // Cache-Control            no-store
        // CustomHeader             test
        // MultiValueCustomHeader   test1, test2
    }
}