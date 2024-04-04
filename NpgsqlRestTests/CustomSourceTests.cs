using Microsoft.AspNetCore.Http;

namespace NpgsqlRestTests;

public class BadRequestFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;
    public bool RefContext { get; } = true;

    public string? FormatCommand(ref Routine routine, ref List<NpgsqlRestParameter> parameters, ref HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return null;
    }
}

public class SelectPathFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;
    public bool RefContext { get; } = true;

    public string? FormatCommand(ref Routine routine, ref List<NpgsqlRestParameter> parameters, ref HttpContext context)
    {
        return string.Format(routine.Expression, context.Request.Path);
    }
}

public class Metadata
{
    public override string ToString() => "metadata";
}

public class MetadataFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;

    public string? FormatCommand(ref Routine routine, ref List<NpgsqlRestParameter> parameters)
    {
        return string.Format(routine.Expression, routine.Metadata);
    }
}

public class QueryStringFormatter : IRoutineSourceParameterFormatter
{
    public bool IsFormattable { get; } = true;
    public bool RefContext { get; } = true;

    public string? FormatCommand(ref Routine routine, ref List<NpgsqlRestParameter> parameters, ref HttpContext context)
    {
        return string.Format(routine.Expression, context.Request.QueryString);
    }
}

public class TestSource : IRoutineSource
{
    public string Query { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IEnumerable<(Routine, IRoutineSourceParameterFormatter)> Read(NpgsqlRestOptions options)
    {
        yield return (
            new Routine(name: "test_custom_source_bad_request", expression: ""),
            new BadRequestFormatter());

        yield return (
            new Routine(name: "test_custom_source_select_path", expression: "select '{0}'")
            {
                IsVoid = false,
                ColumnCount = 1,
                ColumnsTypeDescriptor = [new TypeDescriptor("text")],
            },
            new SelectPathFormatter());

        yield return (
            new Routine(name: "test_custom_source_metadata", expression: "select '{0}'")
            {
                IsVoid = false,
                ColumnCount = 1,
                ColumnsTypeDescriptor = [new TypeDescriptor("text")],
                Metadata = new Metadata(),
            },
            new MetadataFormatter());

        yield return (
            new Routine(name: "test_custom_source_query", expression: "select '{0}'")
            {
                IsVoid = false,
                ColumnCount = 1,
                ColumnsTypeDescriptor = [new TypeDescriptor("text")],
                Metadata = new Metadata(),
            },
            new QueryStringFormatter());
    }
}

[Collection("TestFixture")]
public class CustomSourceTests(TestFixture test)
{
    [Fact]
    public async Task Test_test_custom_source_bad_request()
    {
        using var response = await test.Client.GetAsync("/api/test-custom-source-bad-request/");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Test_test_custom_source_select_path()
    {
        using var response = await test.Client.GetAsync("/api/test-custom-source-select-path/");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Be("/api/test-custom-source-select-path/");
    }

    [Fact]
    public async Task Test_test_custom_source_metadata()
    {
        using var response = await test.Client.GetAsync("/api/test-custom-source-metadata/");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Be("metadata");
    }

    [Fact]
    public async Task Test_test_custom_source_query()
    {
        using var response = await test.Client.GetAsync("/api/test-custom-source-query/?foo=bar&xyz=999");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        content.Should().Be("?foo=bar&xyz=999");
    }
}