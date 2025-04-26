namespace NpgsqlRestTests;

public static partial class Database
{
    public static void AuthPasswordLoginTests()
    {
        script.Append("""
        create function password_protected_login1(
            _pass text,
            _hashed text
        ) 
        returns table (
            name_identifier int,
            name text,
            message text,
            hash text
        )
        language sql as $$
        select
            999 as name_identifier,
            'passwordprotected' as name, 
            'passwordprotected is succesuful' as message,
            _hashed
        $$;
        comment on function password_protected_login1(text,text) is 'login';

        create function password_protected_login2(
            _pass text
        ) 
        returns table (
            name_identifier int,
            name text,
            message text,
            hash text
        )
        language sql as $$
        select
            999 as name_identifier,
            'passwordprotected' as name, 
            'passwordprotected is succesuful' as message,
            'cM+KumWip708pxDiNQJHt/8rU4iECVbm4XeBRcjWP9R8k6UTVDfTAwN6wWT65rYn'
        $$;
        comment on function password_protected_login2(text) is 'login';
        
        """);
    }
}

[Collection("TestFixture")]
public class AuthPasswordLoginTests(TestFixture test)
{
    [Fact]
    public async Task Test_password_protected_login1()
    {
        using var client = test.Application.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);
        var hasher = new PasswordHasher();

        var password = "MySecurePass123!";
        var hashed = hasher.HashPassword(password);

        using var content = new StringContent($"{{\"pass\": \"{password}\", \"hashed\": \"{hashed}\"}}", Encoding.UTF8, "application/json");
        using var login = await client.PostAsync(requestUri: "/api/password-protected-login1/", content);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginContent = await login.Content.ReadAsStringAsync();
        loginContent.Should().Be("passwordprotected is succesuful");
    }

    [Fact]
    public async Task Test_password_protected_login2()
    {
        using var client = test.Application.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);

        var password = "MySecurePass123!";

        using var content = new StringContent($"{{\"pass\": \"{password}\"}}", Encoding.UTF8, "application/json");
        using var login = await client.PostAsync(requestUri: "/api/password-protected-login2/", content);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginContent = await login.Content.ReadAsStringAsync();
        loginContent.Should().Be("passwordprotected is succesuful");
    }

    [Fact]
    public async Task Test_password_protected_login1_wrong_password()
    {
        using var client = test.Application.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);
        var hasher = new PasswordHasher();

        var password = "MySecurePass123!";
        var hashed = hasher.HashPassword("MySecurePass124!");

        using var content = new StringContent($"{{\"pass\": \"{password}\", \"hashed\": \"{hashed}\"}}", Encoding.UTF8, "application/json");
        using var login = await client.PostAsync(requestUri: "/api/password-protected-login1/", content);
        login.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}