namespace NpgsqlRestTests;

public static partial class Database
{
    public static void CommentAuthAttrTests()
    {
        script.Append(@"
create function comment_authorize1() returns text language sql as 'select ''authorize1''';
comment on function comment_authorize1() is 'HTTP
Authorize';

create function comment_authorize2() returns text language sql as 'select ''authorize2''';
comment on function comment_authorize2() is 'HTTP
requires_authorization';

create function comment_authorize3() returns text language sql as 'select ''authorize3''';
comment on function comment_authorize3() is '
Authorize';

create function comment_authorize4() returns text language sql as 'select ''authorize4''';
comment on function comment_authorize4() is 'Authorize
HTTP';
");
    }
}

[Collection("TestFixture")]
public class CommentAuthAttrTests(TestFixture test)
{
    [Fact]
    public async Task Test_comment_authorize1()
    {
        using var response1 = await test.Client.PostAsync("/api/comment-authorize1/", null);
        response1?.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Test_comment_authorize2()
    {
        using var response1 = await test.Client.PostAsync("/api/comment-authorize2/", null);
        response1?.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Test_comment_authorize3()
    {
        using var response1 = await test.Client.PostAsync("/api/comment-authorize3/", null);
        response1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Test_comment_authorize4()
    {
        using var response1 = await test.Client.PostAsync("/api/comment-authorize4/", null);
        response1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}