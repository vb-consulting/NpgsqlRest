namespace NpgsqlRestTests;

public static partial class Database
{
    public static void HashedParameterTests()
    {
        script.Append("""
        create function get_hashed_in_new_parameter1(
            _pass text, 
            _hashed text = null
        ) 
        returns text
        language plpgsql
        as 
        $$
        begin
            return _hashed;
        end;
        $$;

        comment on function get_hashed_in_new_parameter1(text,text) is '
        param _hashed is hash of _pass';

        create function get_hashed_parameter1(
            _pass text
        ) 
        returns text
        language plpgsql
        as 
        $$
        begin
            return _pass;
        end;
        $$;

        comment on function get_hashed_parameter1(text) is '
        param _pass is hash of _pass';

        create function post_hashed_in_new_parameter1(
            _pass text, 
            _hashed text = null
        ) 
        returns text
        language plpgsql
        as 
        $$
        begin
            return _hashed;
        end;
        $$;

        comment on function post_hashed_in_new_parameter1(text,text) is '
        param _hashed is hash of _pass';

        create function post_hashed_parameter1(
            _pass text
        ) 
        returns text
        language plpgsql
        as 
        $$
        begin
            return _pass;
        end;
        $$;

        comment on function post_hashed_parameter1(text) is '
        param _pass is hash of _pass';
""");
    }
}

[Collection("TestFixture")]
public class HashedParameterTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_hashed_in_new_parameter1_1()
    {
        string password = "MySecurePassword123!";
        var hasher = new PasswordHasher();

        using var result = await test.Client.GetAsync(
            $"/api/get-hashed-in-new-parameter1/?pass={password}");
        var response = await result.Content.ReadAsStringAsync();
        result?.StatusCode.Should().Be(HttpStatusCode.OK);

        hasher.VerifyHashedPassword(response, password).Should().Be(true);
    }

    [Fact]
    public async Task Test_get_hashed_in_new_parameter1_2()
    {
        string password = "MySecurePassword456!";
        var hasher = new PasswordHasher();

        using var result = await test.Client.GetAsync(
            $"/api/get-hashed-in-new-parameter1/?pass={password}&hashed=something");
        var response = await result.Content.ReadAsStringAsync();
        result?.StatusCode.Should().Be(HttpStatusCode.OK);

        hasher.VerifyHashedPassword(response, password).Should().Be(true);
    }

    [Fact]
    public async Task Test_get_hashed_in_new_parameter1_3()
    {
        string password = "";
        using var result = await test.Client.GetAsync(
            $"/api/get-hashed-in-new-parameter1/?pass={password}");
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("");
    }

    [Fact]
    public async Task Test_get_hashed_parameter1()
    {
        var hasher = new PasswordHasher();
        string password = "MySecurePassword789!";
        using var result = await test.Client.GetAsync(
            $"/api/get-hashed-parameter1/?pass={password}");
        var response = await result.Content.ReadAsStringAsync();
        hasher.VerifyHashedPassword(response, password).Should().Be(true);
    }

    [Fact]
    public async Task Test_post_hashed_in_new_parameter1_1()
    {
        string password = "MySecurePassword123!";
        using var content = new StringContent($"{{\"pass\": \"{password}\"}}", Encoding.UTF8, "application/json");
        var hasher = new PasswordHasher();

        using var result = await test.Client.PostAsync("/api/post-hashed-in-new-parameter1/", content);
        var response = await result.Content.ReadAsStringAsync();
        result?.StatusCode.Should().Be(HttpStatusCode.OK);

        hasher.VerifyHashedPassword(response, password).Should().Be(true);
    }

    [Fact]
    public async Task Test_post_hashed_in_new_parameter1_2()
    {
        string password = "MySecurePassword456!";
        using var content = new StringContent($"{{\"pass\": \"{password}\", \"hashed\": \"something\"}}", Encoding.UTF8, "application/json");
        var hasher = new PasswordHasher();

        using var result = await test.Client.PostAsync("/api/post-hashed-in-new-parameter1/", content);
        var response = await result.Content.ReadAsStringAsync();
        result?.StatusCode.Should().Be(HttpStatusCode.OK);

        hasher.VerifyHashedPassword(response, password).Should().Be(true);
    }

    [Fact]
    public async Task Test_post_hashed_in_new_parameter1_3()
    {
        string password = "";
        using var content = new StringContent($"{{\"pass\": \"{password}\"}}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/post-hashed-in-new-parameter1/", content);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("");
    }

    [Fact]
    public async Task Test_post_hashed_parameter1()
    {
        var hasher = new PasswordHasher();
        string password = "MySecurePassword789!";
        using var content = new StringContent($"{{\"pass\": \"{password}\"}}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/post-hashed-parameter1/", content);
        var response = await result.Content.ReadAsStringAsync();
        hasher.VerifyHashedPassword(response, password).Should().Be(true);
    }
}
