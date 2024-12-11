namespace NpgsqlRestTests;

public static partial class Database
{
    public static void NoneExistingParamTests()
    {
        script.Append("""
        create function get_none_existing_params(_p1 int, _p2 int) 
        returns text 
        language plpgsql
        as 
        $$
        begin
            return 'OK';
        end;
        $$;

        create function post_none_existing_params(_p1 int, _p2 int) 
        returns text 
        language plpgsql
        as 
        $$
        begin
            return 'OK';
        end;
        $$;
        """);
    }
}

[Collection("TestFixture")]
public class NoneExistingParamTests(TestFixture test)
{
    [Fact]
    public async Task Test_none_existing_params__OK()
    {
        using var response = await test.Client.GetAsync("/api/get-none-existing-params/?p1=1&p2=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_post_none_existing_params__OK()
    {
        using var body = new StringContent("{\"p1\":1,\"p2\":2}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/post-none-existing-params/", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_none_existing_params__NotFound1()
    {
        using var response = await test.Client.GetAsync("/api/get-none-existing-params/?x1=1&x2=2");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_post_none_existing_params__NotFound1()
    {
        using var body = new StringContent("{\"x1\":1,\"x2\":2}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/post-none-existing-params/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_none_existing_params__NotFound2()
    {
        using var response = await test.Client.GetAsync("/api/get-none-existing-params/?x1=1&p2=2");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_post_none_existing_params__NotFound2()
    {
        using var body = new StringContent("{\"x1\":1,\"p2\":2}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/post-none-existing-params/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_none_existing_params__NotFound3()
    {
        using var response = await test.Client.GetAsync("/api/get-none-existing-params/?p1=1&x2=2");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_post_none_existing_params__NotFound3()
    {
        using var body = new StringContent("{\"p1\":1,\"x2\":2}", Encoding.UTF8, "application/json");
        using var response = await test.Client.PostAsync("/api/post-none-existing-params/", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}