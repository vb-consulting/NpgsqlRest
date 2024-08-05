#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void MultiParamsTests1()
    {
        script.Append(@"
create function case_multi_params1(
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
public class MultiParamsTests1(TestFixture test)
{
    [Fact]
    public async Task Test_case_multi_params1()
    {
        string body = """
        {  
            "smallint": 1,
            "integer": 2,
            "bigint": 3,
            "numeric": 4,
            "text": "text",
            "varchar": "varchar",
            "char": "abc",
            "json": {"a": "b"},
            "jsonb": {"c": "d"},
            "smallintArray": [1,2,3],
            "integerArray": [2,3,4],
            "bigintArray": [3,4,5],
            "numericArray": [4,5,6],
            "textArray": ["text1","text2"],
            "varcharArray": ["varchar1","varchar2"],
            "charArray": ["abc","xyz"],
            "jsonArray": [{"a": "b"},{"c": "d"}],
            "jsonbArray": [{"x": "y"},{"i": "j"}]
        }
""";
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var result = await test.Client.PostAsync("/api/case-multi-params1/", content);
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
}