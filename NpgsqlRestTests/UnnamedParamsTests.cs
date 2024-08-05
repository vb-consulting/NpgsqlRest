namespace NpgsqlRestTests;

public static partial class Database
{
    public static void UnnamedParamsTests()
    {
        script.Append(@"
create function case_get_unnamed_int(int) 
returns int 
language plpgsql
as 
$$
begin
    raise info '_i = %', $1;
    return $1;
end;
$$;

create function case_return_unnamed_int(int) 
returns int 
language plpgsql
as 
$$
begin
    raise info '_i = %', $1;
    return $1;
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class UnnamedParamsTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_get_unnamed_int()
    {
        using var result = await test.Client.GetAsync("/api/case-get-unnamed-int/?$1=999");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        response.Should().Be("999");
    }

    [Fact]
    public async Task Test_case_return_unnamed_int()
    {
        using var content = new StringContent("{\"$1\":999}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-unnamed-int/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
        response.Should().Be("999");
    }
}