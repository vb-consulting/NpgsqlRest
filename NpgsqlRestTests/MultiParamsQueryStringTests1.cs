#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void MultiParamsQueryStringTests1()
    {
        script.Append(@"
create function case_get_multi_params1(
    _smallint smallint,
    _integer integer,
    _bigint bigint,
    _numeric numeric,
    _text text,
    _varchar varchar,
    _char char(3),
    _json json,
    _jsonb jsonb,
    _smallint_array smallint[],
    _integer_array integer[],
    _bigint_array bigint[],
    _numeric_array numeric[],
    _text_array text[],
    _varchar_array varchar[],
    _char_array char(3)[],
    _json_array json[],
    _jsonb_array jsonb[]
) 
returns json
language plpgsql
as 
$$
begin
    return json_build_object(
        'smallint', _smallint,
        'integer', _integer,
        'bigint', _bigint,
        'numeric', _numeric,
        'text', _text,
        'varchar', _varchar,
        'char', _char,
        'json', _json,
        'jsonb', _jsonb,
        'smallintArray', _smallint_array,
        'integerArray', _integer_array,
        'bigintArray', _bigint_array,
        'numericArray', _numeric_array,
        'textArray', _text_array,
        'varcharArray', _varchar_array,
        'charArray', _char_array,
        'jsonArray', _json_array,
        'jsonbArray', _jsonb_array
    );
end;
$$;
");
    }
}

[Collection("TestFixture")]
public class MultiParamsQueryStringTests1(TestFixture test)
{
    [Fact]
    public async Task Test_case_get_multi_params1()
    {
        var query = new QueryBuilder
        {
            { "smallint", "1" },
            { "integer", "2" },
            { "bigint", "3" },
            { "numeric", "4" },
            { "text", "text" },
            { "varchar", "varchar" },
            { "char", "abc" },
            { "json", "{\"a\": \"b\"}" },
            { "jsonb", "{\"c\": \"d\"}" },
            { "smallintArray", "1" }, 
            { "smallintArray", "2" }, 
            { "smallintArray", "3" },
            { "integerArray", "2" }, 
            { "integerArray", "3" }, 
            { "integerArray", "4" },
            { "bigintArray", "3" }, 
            { "bigintArray", "4" }, 
            { "bigintArray", "5" },
            { "numericArray", "4" }, 
            { "numericArray", "5" }, 
            { "numericArray", "6" },
            { "textArray", "text1" }, 
            { "textArray", "text2" },
            { "varcharArray", "varchar1" }, 
            { "varcharArray", "varchar2" },
            { "charArray", "abc" }, 
            { "charArray", "xyz" },
            { "jsonArray", "{\"a\": \"b\"}" }, 
            { "jsonArray", "{\"c\": \"d\"}" },
            { "jsonbArray", "{\"x\": \"y\"}" }, 
            { "jsonbArray", "{\"i\": \"j\"}" }
        };

        using var result = await test.Client.GetAsync($"/api/case-get-multi-params1/{query}");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var node = JsonNode.Parse(response);
        node["smallint"].ToJsonString().Should().Be("1");
        node["integer"].ToJsonString().Should().Be("2");
        node["bigint"].ToJsonString().Should().Be("3");
        node["numeric"].ToJsonString().Should().Be("4");
        node["text"].ToJsonString().Should().Be("\"text\"");
        node["varchar"].ToJsonString().Should().Be("\"varchar\"");
        node["char"].ToJsonString().Should().Be("\"abc\"");
        node["json"].ToJsonString().Should().Be("{\"a\":\"b\"}");
        node["jsonb"].ToJsonString().Should().Be("{\"c\":\"d\"}");
        node["smallintArray"].ToJsonString().Should().Be("[1,2,3]");
        node["integerArray"].ToJsonString().Should().Be("[2,3,4]");
        node["bigintArray"].ToJsonString().Should().Be("[3,4,5]");
        node["numericArray"].ToJsonString().Should().Be("[4,5,6]");
        node["textArray"].ToJsonString().Should().Be("[\"text1\",\"text2\"]");
        node["varcharArray"].ToJsonString().Should().Be("[\"varchar1\",\"varchar2\"]");
        node["charArray"].ToJsonString().Should().Be("[\"abc\",\"xyz\"]");
        node["jsonArray"].ToJsonString().Should().Be("[{\"a\":\"b\"},{\"c\":\"d\"}]");
        node["jsonbArray"].ToJsonString().Should().Be("[{\"x\":\"y\"},{\"i\":\"j\"}]");
    }

    [Fact]
    public async Task Test_case_get_multi_params1_reverse_order()
    {
        var query = new QueryBuilder
        {
            { "jsonbArray", "{\"i\": \"j\"}" },
            { "jsonbArray", "{\"x\": \"y\"}" },
            { "jsonArray", "{\"c\": \"d\"}" },
            { "jsonArray", "{\"a\": \"b\"}" },
            { "charArray", "xyz" },
            { "charArray", "abc" },
            { "varcharArray", "varchar2" },
            { "varcharArray", "varchar1" },
            { "textArray", "text2" },
            { "textArray", "text1" },
            { "numericArray", "6" },
            { "numericArray", "5" },
            { "numericArray", "4" },
            { "bigintArray", "5" },
            { "bigintArray", "4" },
            { "bigintArray", "3" },
            { "integerArray", "4" },
            { "integerArray", "3" },
            { "integerArray", "2" },
            { "smallintArray", "3" },
            { "smallintArray", "2" },
            { "smallintArray", "1" },
            { "jsonb", "{\"c\": \"d\"}" },
            { "json", "{\"a\": \"b\"}" },
            { "char", "abc" },
            { "varchar", "varchar" },
            { "text", "text" },
            { "numeric", "4" },
            { "bigint", "3" },
            { "integer", "2" },
            { "smallint", "1" },
        };

        using var result = await test.Client.GetAsync($"/api/case-get-multi-params1/{query}");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var node = JsonNode.Parse(response);
        node["smallint"].ToJsonString().Should().Be("1");
        node["integer"].ToJsonString().Should().Be("2");
        node["bigint"].ToJsonString().Should().Be("3");
        node["numeric"].ToJsonString().Should().Be("4");
        node["text"].ToJsonString().Should().Be("\"text\"");
        node["varchar"].ToJsonString().Should().Be("\"varchar\"");
        node["char"].ToJsonString().Should().Be("\"abc\"");
        node["json"].ToJsonString().Should().Be("{\"a\":\"b\"}");
        node["jsonb"].ToJsonString().Should().Be("{\"c\":\"d\"}");
        node["smallintArray"].ToJsonString().Should().Be("[3,2,1]");
        node["integerArray"].ToJsonString().Should().Be("[4,3,2]");
        node["bigintArray"].ToJsonString().Should().Be("[5,4,3]");
        node["numericArray"].ToJsonString().Should().Be("[6,5,4]");
        node["textArray"].ToJsonString().Should().Be("[\"text2\",\"text1\"]");
        node["varcharArray"].ToJsonString().Should().Be("[\"varchar2\",\"varchar1\"]");
        node["charArray"].ToJsonString().Should().Be("[\"xyz\",\"abc\"]");
        node["jsonArray"].ToJsonString().Should().Be("[{\"c\":\"d\"},{\"a\":\"b\"}]");
        node["jsonbArray"].ToJsonString().Should().Be("[{\"i\":\"j\"},{\"x\":\"y\"}]");
    }
}