namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CommentParamTypeAttrTests()
    {
        script.Append(@"
create function comment_param_type_query1(_t text) returns text language sql as 'select _t';
comment on function comment_param_type_query1(text) is '
HTTP
request_param_type query_string
';

create function comment_param_type_query2(_t text) returns text language sql as 'select _t';
comment on function comment_param_type_query2(text) is '
HTTP
param_type query
';

create function comment_get_param_type_json1(_t text) returns text language sql as 'select _t';
comment on function comment_get_param_type_json1(text) is '
HTTP
request_param_type body_json
';

create function comment_get_param_type_json2(_t text) returns text language sql as 'select _t';
comment on function comment_get_param_type_json2(text) is '
HTTP
param_type BODY
';
");
    }
}

[Collection("TestFixture")]
public class CommentParamTypeAttrTests(TestFixture test)
{
    [Fact]
    public async Task Test_comment_param_type_query1_JsonBody()
    {
        using var content = new StringContent("{\"t\": \"comment_param_type_query1\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/comment-param-type-query1/", content);
        response?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_comment_param_type_query1_QueryString()
    {
        var query = new QueryBuilder { { "t", "comment-param-type-query1" } };
        using var response = await test.Client.PostAsync($"/api/comment-param-type-query1/{query}", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Be("comment-param-type-query1");
    }

    [Fact]
    public async Task Test_comment_param_type_query2_JsonBody()
    {
        using var content = new StringContent("{\"t\": \"comment_param_type_query2\"}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/comment-param-type-query2/", content);
        response?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_comment_param_type_query2_QueryString()
    {
        var query = new QueryBuilder { { "t", "comment-param-type-query2" } };
        using var response = await test.Client.PostAsync($"/api/comment-param-type-query2/{query}", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Be("comment-param-type-query2");
    }

    [Fact]
    public async Task Test_comment_get_param_type_json1_QueryString()
    {
        var query = new QueryBuilder { { "t", "comment-param-type-query1" } };
        using var response = await test.Client.GetAsync($"/api/comment-get-param-type-json1/{query}");
        response?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_comment_get_param_type_json1_JsonBody()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("/api/comment-get-param-type-json1/", UriKind.Relative),
            Content = new StringContent("{\"t\": \"comment-get-param-type-json1\"}", Encoding.UTF8, "application/json"),
        };
        using var response = await test.Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Be("comment-get-param-type-json1");
    }

    [Fact]
    public async Task Test_comment_get_param_type_json2_QueryString()
    {
        var query = new QueryBuilder { { "t", "comment-param-type-query2" } };
        using var response = await test.Client.GetAsync($"/api/comment-get-param-type-json2/{query}");
        response?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_comment_get_param_type_json2_JsonBody()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("/api/comment-get-param-type-json2/", UriKind.Relative),
            Content = new StringContent("{\"t\": \"comment-get-param-type-json2\"}", Encoding.UTF8, "application/json"),
        };
        using var response = await test.Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Be("comment-get-param-type-json2");
    }
}