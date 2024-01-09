namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ArrayTypeTests()
    {
        script.Append("""
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
    return array['a','bc','x,y','foo"bar"','"foo","bar"'];
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

create or replace function case_return_setof_int_array() 
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

create or replace function case_return_setof_bool_array() 
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

create or replace function case_return_setof_text_array() 
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
""");
    }
}

[Collection("TestFixture")]
public class ArrayTypeTests(TestFixture test)
{
    [Fact]
    public async Task Test_CaseReturnIntArray()
    {
        using var result = await test.Client.PostAsync("/api/case-return-int-array/", null);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("[1,2,3]");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_CaseReturnTextArray()
    {
        using var result = await test.Client.PostAsync("/api/case-return-text-array/", null);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("""["a","bc","x,y","foo\"bar\"","\"foo\",\"bar\""]""");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_CaseReturnBoolArray()
    {
        using var result = await test.Client.PostAsync("/api/case-return-bool-array/", null);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("[true,false]");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_CaseReturnSetofIntArray()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-int-array/", null);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("[[1,2,3],[4,5,6],[7,8,9]]");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_CaseReturnSetofBoolArray()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-bool-array/", null);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("[[true,false],[false,true]]");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_CaseReturnSetofTextArray()
    {
        using var result = await test.Client.PostAsync("/api/case-return-setof-text-array/", null);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().Be("""[["a","bc"],["x","yz","foo","bar"]]""");
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
        result?.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
