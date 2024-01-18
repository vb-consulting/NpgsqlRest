namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnIntTests()
    {
        script.Append(@"
create function case_return_int(_i int) 
returns int 
language plpgsql
as 
$$
begin
    raise info '_i = %', _i;
    return _i;
end;
$$;

create function case_get_int(_i int) 
returns int 
language plpgsql
as 
$$
begin
    raise info '_i = %', _i;
    return _i;
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class ReturnIntTests(TestFixture test)
{
    [Fact]
    public async Task Test_CaseReturnInt()
    {
        using var content = new StringContent("{\"i\":999}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-int/", content);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("999");
    }

    [Fact]
    public async Task Test_CaseReturnInt_Wrong_Parameter()
    {
        using var content = new StringContent("{\"x\":666}", Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-return-int/", content);

        result?.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_CaseGetInt()
    {
        using var result = await test.Client.GetAsync("/api/case-get-int/?i=999");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("text/plain");
        response.Should().Be("999");
    }
}