using System.Net;
using System.Security.Claims;

namespace NpgsqlRestTests;

public static partial class Database
{
    public static void UserContextTests()
    {
        script.Append("""
        create function user_context_login() 
        returns table (
            name_identifier int,
            name text,
            role text[]
        )
        language sql as $$
        select
            123 as name_identifier,
            'myname' as name,
            array['admin', 'user'] as role
        $$;
        comment on function user_context_login() is 'login';
        
        create function get_user_context() 
        returns table (
            name_identifier int,
            name text,
            role text[]
        )
        language sql as $$
        select
            current_setting('request.user_id', true)::int,
            current_setting('request.user_name', true)::text,
            (current_setting('request.user_roles', true))::text[]
        $$;
        comment on function get_user_context() is '
        authorize
        user_context
        ';

        create function get_user_context_and_headers() 
        returns table (
            name_identifier int,
            name text,
            role text[],
            headers jsonb
        )
        language sql as $$
        select
            current_setting('request.user_id', true)::int,
            current_setting('request.user_name', true)::text,
            (current_setting('request.user_roles', true))::text[],
            current_setting('request.headers', true)::jsonb
        $$;
        comment on function get_user_context_and_headers() is '
        authorize
        user_context
        request_headers context
        ';

        create function get_user_context_and_ip_and_full_claims() 
        returns table (
            ip_address text,
            claims text
        )
        language sql as $$
        select
            current_setting('request.ip_address', true)::text,
            current_setting('request.user_claims', true)::text
        $$;
        comment on function get_user_context_and_ip_and_full_claims() is '
        authorize
        user_context
        ';
        """);
    }
}

[Collection("TestFixture")]
public class UserContextTests(TestFixture test)
{

    [Fact]
    public async Task Test_get_user_context_and_ip_and_full_claims()
    {
        using var client = test.Application.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);

        using var login = await client.PostAsync("/api/user-context-login/", null);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.GetAsync("/api/get-user-context-and-ip-and-full-claims/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // this will be the content when UseActiveDirectoryFederationServicesClaimTypes is true
        //content.Should().Be("[{\"ipAddress\":\"\",\"claims\":\"{\\\"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier\\\":\\\"123\\\",\\\"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name\\\":\\\"myname\\\",\\\"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\\\":[\\\"admin\\\",\\\"user\\\"]}\"}]");
        
        content.Should().Be("[{\"ipAddress\":\"\",\"claims\":\"{\\\"nameidentifier\\\":\\\"123\\\",\\\"name\\\":\\\"myname\\\",\\\"role\\\":[\\\"admin\\\",\\\"user\\\"]}\"}]");
    }

    [Fact]
    public async Task Test_get_user_context1()
    {
        using var client = test.Application.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);

        using var login = await client.PostAsync("/api/user-context-login/", null);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.GetAsync("/api/get-user-context/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[{\"nameIdentifier\":123,\"name\":\"myname\",\"role\":[\"admin\",\"user\"]}]");
    }

    [Fact]
    public async Task Test_get_user_context_and_headers()
    {
        using var client = test.Application.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);

        using var login = await client.PostAsync("/api/user-context-login/", null);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.GetAsync("/api/get-user-context-and-headers/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().StartWith("[{\"nameIdentifier\":123,\"name\":\"myname\",\"role\":[\"admin\",\"user\"],\"headers\":{\"Host\": \"localhost\"");
    }
}