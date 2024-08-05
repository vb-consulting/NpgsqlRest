#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void ReturnTableTests()
    {
        script.Append("""
create function case_return_table1() 
returns table (
    int_field int, 
    text_field text, 
    bool_field bool,
    int_array int[], 
    text_array text[], 
    bool_array bool[]
)
language plpgsql
as 
$$
begin
    return query 
    select t.*
    from (
        values 
        (1, 'ABC', null, array[1,null,3], array['ABC', 'XYZ'], array[true, false]), 
        (2, null, false, array[1,2,3], array['ABC', null], array[false, true]), 
        (null, 'IJN', true, array[3,2,1], array['XYZ', 'ABC'], array[true, null])
    ) t;
end;
$$;

create function case_return_table2() 
returns table (
    "real" real,
    "double" double precision,
    "jsonpath" jsonpath,
    "timestamp" timestamp,
    "timestamptz" timestamptz,
    "date" date,
    "time" time,
    "timetz" timetz,
    "interval" interval,
    "uuid" uuid,
    "varbit" varbit,
    "bit" bit(3),
    "inet" inet,
    "macaddr" macaddr,
    "bytea" bytea
)
language plpgsql
as 
$$
begin
    return query 
    select t.*
    from (
        values 
        (
            1.1::real, 2.2::double precision, 
            '$.path'::jsonpath, 
            '2023-01-29'::timestamp, '2023-01-30'::timestamptz, '2023-01-31'::date, '00:15:45'::time, '00:35:45'::timetz,
            '1 day 10 hours 30 minutes 1 second'::interval, '1137788C-F1BA-4379-8F5A-F530CACDE300'::uuid,
            '111100001'::varbit, '111100001'::bit(3),
            '192.168.5.18'::inet, '00-B0-D0-63-C2-26'::macaddr,
            '\xDEADBEEF'::bytea
        )
    ) t;
end;
$$;
""");
    }
}


[Collection("TestFixture")]
public class ReturnTableTests(TestFixture test)
{
    [Fact]
    public async Task Test_case_return_table1()
    {
        using var result = await test.Client.PostAsync("/api/case-return-table1/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var array = JsonNode.Parse(response).AsArray();
        array.Count.Should().Be(3);

        array[0]["intField"].ToJsonString().Should().Be("1");
        array[0]["textField"].GetValue<string>().Should().Be("ABC");
        array[0]["boolField"].Should().BeNull();
        array[0]["intArray"].ToJsonString().Should().Be("[1,null,3]");
        array[0]["textArray"].ToJsonString().Should().Be("[\"ABC\",\"XYZ\"]");
        array[0]["boolArray"].ToJsonString().Should().Be("[true,false]");

        array[1]["intField"].ToJsonString().Should().Be("2");
        array[1]["textField"].Should().BeNull();
        array[1]["boolField"].ToJsonString().Should().Be("false");
        array[1]["intArray"].ToJsonString().Should().Be("[1,2,3]");
        array[1]["textArray"].ToJsonString().Should().Be("[\"ABC\",null]");
        array[1]["boolArray"].ToJsonString().Should().Be("[false,true]");

        array[2]["intField"].Should().BeNull();
        array[2]["textField"].GetValue<string>().Should().Be("IJN");
        array[2]["boolField"].ToJsonString().Should().Be("true");
        array[2]["intArray"].ToJsonString().Should().Be("[3,2,1]");
        array[2]["textArray"].ToJsonString().Should().Be("[\"XYZ\",\"ABC\"]");
        array[2]["boolArray"].ToJsonString().Should().Be("[true,null]");
    }

    [Fact]
    public async Task Test_case_return_table2()
    {
        using var result = await test.Client.PostAsync("/api/case-return-table2/", null);
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var array = JsonNode.Parse(response).AsArray();

        array.Count.Should().Be(1);

        array[0]["real"].ToJsonString().Should().Be("1.1");
        array[0]["double"].ToJsonString().Should().Be("2.2");
        array[0]["jsonpath"].GetValue<string>().Should().Be("$.\"path\"");
        array[0]["timestamp"].GetValue<string>().Should().Be("2023-01-29T00:00:00");
        array[0]["timestamptz"].GetValue<string>().Should().Be("2023-01-30T00:00:00+00");
        array[0]["date"].GetValue<string>().Should().Be("2023-01-31");
        array[0]["time"].GetValue<string>().Should().Be("00:15:45");
        array[0]["timetz"].GetValue<string>().Should().Be("00:35:45+00");
        array[0]["interval"].GetValue<string>().Should().Be("1 day 10:30:01");
        array[0]["uuid"].GetValue<string>().Should().Be("1137788c-f1ba-4379-8f5a-f530cacde300");
        array[0]["varbit"].GetValue<string>().Should().Be("111100001");
        array[0]["bit"].GetValue<string>().Should().Be("111");
        array[0]["inet"].GetValue<string>().Should().Be("192.168.5.18");
        array[0]["macaddr"].GetValue<string>().Should().Be("00:b0:d0:63:c2:26");
        array[0]["bytea"].GetValue<string>().Should().Be("\\xdeadbeef");
    }
}
