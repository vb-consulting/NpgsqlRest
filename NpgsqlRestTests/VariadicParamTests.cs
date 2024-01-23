namespace NpgsqlRestTests;

public static partial class Database
{
    public static void VariadicParamTests()
    {
        script.Append(@"
create function variadic_param_plus_one(variadic v int[]) 
returns int[] 
language sql
as 
$$
select array_agg(n + 1) from unnest($1) AS n;
$$;
");
    }
}

[Collection("TestFixture")]
public class VariadicParamTests(TestFixture test)
{
    [Fact]
    public async Task Test_variadic_param_plus_one()
    {
        using var body = new StringContent("{\"v\": [1,2,3,4]}", Encoding.UTF8);
        using var response = await test.Client.PostAsync("/api/variadic-param-plus-one", body);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("[2,3,4,5]");
    }
}