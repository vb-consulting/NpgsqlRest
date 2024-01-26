#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ArrayTypeTests()
    {
        script.Append("""""
create function case_return_int_array() 
returns int[]
language plpgsql
as 
$$
begin
    return array[1,2,3];
end;
$$;

create function case_return_text_array() 
returns text[]
language plpgsql
as 
$$
begin
    return array['a', 'bc', 'x,y', 'foo"bar"', '"foo","bar"', 'foo\bar'];
end;
$$;

create function case_return_bool_array() 
returns boolean[]
language plpgsql
as 
$$
begin
    return array[true, false];
end;
$$;

create function case_return_setof_int_array() 
returns setof int[]
language plpgsql
as 
$$
begin
    return query select a from (
        values 
            (array[1,2,3]),
            (array[4,5,6]),
            (array[7,8,9])
    ) t(a);
end;
$$;

create function case_return_setof_bool_array() 
returns setof boolean[]
language plpgsql
as 
$$
begin
    return query select a from (
        values 
            (array[true,false]),
            (array[false,true])
    ) t(a);
end;
$$;

create function case_return_setof_text_array() 
returns setof text[]
language plpgsql
as 
$$
begin
    return query select a from (
        values 
            (array['a','bc']),
            (array['x','yz','foo','bar'])
    ) t(a);
end;
$$;

create function case_return_int_array_with_null() 
returns int[]
language plpgsql
as 
$$
begin
    return array[4,5,6,null];
end;
$$;


create function case_return_array_edge_cases() 
returns text[]
language plpgsql
as 
$$
begin
    return array[
        'foo,bar',
        'foo"bar',
        '"foo"bar"',
        'foo""bar',
        'foo""""bar',
        'foo"",""bar',
        'foo\bar',
        'foo/bar',
        E'foo\nbar'
    ];
end;
$$;
""""");
    }
}

[Collection("TestFixture")]
public class ArrayTypeTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_return_int_array()
    {
        using var result = await test.Client.PostAsync("/api/case-return-int-array/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[1,2,3]");
    }

    [Fact]
    public async Task Test_case_return_text_array()
    {
        using var result = await test.Client.PostAsync("/api/case-return-text-array/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("""["a","bc","x,y","foo\"bar\"","\"foo\",\"bar\"","foo\\bar"]""");

        var array = JsonNode.Parse(response).AsArray();
        array.Count.Should().Be(6);
        array[0].ToJsonString().Should().Be("\"a\"");
        array[1].ToJsonString().Should().Be("\"bc\"");
        array[2].ToJsonString().Should().Be("\"x,y\"");
        array[3].ToJsonString().Should().Be("\"foo\\u0022bar\\u0022\"");
        array[4].ToJsonString().Should().Be("\"\\u0022foo\\u0022,\\u0022bar\\u0022\"");
        array[5].ToJsonString().Should().Be("\"foo\\\\bar\"");
    }

    [Fact]
    public async Task Test_case_return_bool_array()
    {
        using var result = await test.Client.PostAsync("/api/case-return-bool-array/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[true,false]");
    }

    [Fact]
    public async Task Test_case_return_setof_int_array()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-int-array/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[[1,2,3],[4,5,6],[7,8,9]]");
    }

    [Fact]
    public async Task Test_CaseReturnSetofBoolArray()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-bool-array/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[[true,false],[false,true]]");
    }

    [Fact]
    public async Task Test_case_return_setof_bool_array()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-text-array/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("""[["a","bc"],["x","yz","foo","bar"]]""");
    }

    [Fact]
    public async Task Test_case_return_int_array_with_null()
    {
        using var result = await test.Client.PostAsync("/api/case-return-int-array-with-null/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        response.Should().Be("[4,5,6,null]");
    }

    [Fact]
    public async Task Test_case_return_array_edge_cases()
    {
        using var response = await test.Client.PostAsync("/api/case-return-array-edge-cases", null);
        var content = await response.Content.ReadAsStringAsync();

        response?.StatusCode.Should().Be(HttpStatusCode.OK);
        response?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");

        var expextedContent = """""
        [
            "foo,bar",
            "foo\"bar",
            "\"foo\"bar\"",
            "foo\"\"bar",
            "foo\"\"\"\"bar",
            "foo\"\",\"\"bar",
            "foo\\bar",
            "foo/bar",
            "foo\nbar"
        ]
        """""
        .Replace(" ", "")
        .Replace("\r", "")
        .Replace("\n", "");

        content.Should().Be(expextedContent);
    }
}
