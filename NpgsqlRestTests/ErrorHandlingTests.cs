namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ErrorHandlingTests()
    {
        script.Append(@"
        create function raise_exception()
        returns text 
        language plpgsql
        as 
        $$
        begin
            raise exception 'Test exception';
            return 'test';
        end;
        $$;

        create function assert_failure_exception()
        returns text 
        language plpgsql
        as 
        $$
        begin
            assert false, 'Test assert failure';
            return 'test';
        end;
        $$;

        create function division_by_zero_exception(
            _meta json = null
        )
        returns text 
        language plpgsql
        as 
        $$
        declare
            _result int = 1 / 0;
        begin
            return 'test';
        end;
        $$;
");
    }
}

[Collection("TestFixture")]
public class ErrorHandlingTests(TestFixture test)
{
    [Fact]
    public async Task Test_raise_exception_test()
    {
        using var result = await test.Client.PostAsync("/api/raise-exception/", null);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var response = await result.Content.ReadAsStringAsync();

        response.Should().Be("Test exception");
    }

    [Fact]
    public async Task Test_assert_failure_exception_test()
    {
        using var result = await test.Client.PostAsync("/api/assert-failure-exception/", null);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var response = await result.Content.ReadAsStringAsync();

        response.Should().Be("Test assert failure");
    }

    [Fact]
    public async Task Test_division_by_zero_exception_test()
    {
        using var result = await test.Client.PostAsync("/api/division-by-zero-exception/", null);
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var response = await result.Content.ReadAsStringAsync();

        response.Should().Be("22012: division by zero");
    }
}