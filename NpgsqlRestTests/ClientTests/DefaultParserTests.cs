using NpgsqlRestClient;

namespace NpgsqlRestTests.ClientTests;

public class DefaultParserTests
{
    [Fact]
    public void Parse_simple()
    {
        DefaultResponseParser.FormatString("", []).ToString().Should().Be("");

        var str5 = GenerateRandomString(5);
        DefaultResponseParser.FormatString(str5.AsSpan(), []).ToString().Should().Be(str5);

        var str10 = GenerateRandomString(10);
        DefaultResponseParser.FormatString(str10.AsSpan(), []).ToString().Should().Be(str10);

        var str50 = GenerateRandomString(50);
        DefaultResponseParser.FormatString(str50.AsSpan(), []).ToString().Should().Be(str50);

        var str100 = GenerateRandomString(100);
        DefaultResponseParser.FormatString(str100.AsSpan(), []).ToString().Should().Be(str100);
    }

    [Fact]
    public void Parse_one()
    {
        var str1 = "Hello, {name}!";
        DefaultResponseParser.FormatString(str1.AsSpan(), new Dictionary<string, string> { { "name", "world" } }).ToString().Should().Be("Hello, world!");
    }

    [Fact]
    public void Parse_multiple_placeholders()
    {
        var str = "Hello, {name}! Today is {day}.";
        var replacements = new Dictionary<string, string>
        {
            { "name", "Alice" },
            { "day", "Monday" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().Should().Be("Hello, Alice! Today is Monday.");
    }

    [Fact]
    public void Parse_placeholder_not_found()
    {
        var str = "Hello, {name}!";
        DefaultResponseParser.FormatString(str.AsSpan(), []).ToString().Should().Be("Hello, {name}!");
    }

    [Fact]
    public void Parse_placeholder_with_special_characters()
    {
        var str = "Hello, {user_name}!";
        var replacements = new Dictionary<string, string>
        {
            { "user_name", "user@domain.com" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().Should().Be("Hello, user@domain.com!");
    }

    [Fact]
    public void Parse_placeholder_with_numbers()
    {
        var str = "Your order number is {order_number}.";
        var replacements = new Dictionary<string, string>
        {
            { "order_number", "12345" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().Should().Be("Your order number is 12345.");
    }

    [Fact]
    public void Parse_empty_placeholder()
    {
        var str = "Hello, {}!";
        DefaultResponseParser.FormatString(str.AsSpan(), []).ToString().Should().Be("Hello, {}!");
    }

    [Fact]
    public void Parse_null_replacements()
    {
        var str = "Hello, {name}!";
        DefaultResponseParser.FormatString(str.AsSpan(), null!).ToString().Should().Be("Hello, {name}!");
    }

    [Fact]
    public void Parse_repeated_placeholders()
    {
        var str = "{greeting}, {greeting}!";
        var replacements = new Dictionary<string, string>
        {
            { "greeting", "Hi" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().Should().Be("Hi, Hi!");
    }

    [Fact]
    public void Parse_placeholder_with_empty_value()
    {
        var str = "Hello, {name}!";
        var replacements = new Dictionary<string, string>
        {
            { "name", "" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().ToString().Should().Be("Hello, !");
    }

    [Fact]
    public void Parse_ten()
    {
        var str = $"{GenerateRandomString(10)}{{name1}}{GenerateRandomString(10)}{{name2}}{GenerateRandomString(10)}{{name3}}{GenerateRandomString(10)}{{name4}}{GenerateRandomString(10)}{{name5}}{GenerateRandomString(10)}{{name6}}{GenerateRandomString(10)}{{name7}}{GenerateRandomString(10)}{{name8}}{GenerateRandomString(10)}{{name9}}{GenerateRandomString(10)}{{name10}}{GenerateRandomString(10)}";
        var replacements = new Dictionary<string, string>
    {
        { "name1", "value1" },
        { "name2", "value2" },
        { "name3", "value3" },
        { "name4", "value4" },
        { "name5", "value5" },
        { "name6", "value6" },
        { "name7", "value7" },
        { "name8", "value8" },
        { "name9", "value9" },
        { "name10", "value10" }
    };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements)
            .ToString().Should().Be(str
                .Replace("{name1}", "value1")
                .Replace("{name2}", "value2")
                .Replace("{name3}", "value3")
                .Replace("{name4}", "value4")
                .Replace("{name5}", "value5")
                .Replace("{name6}", "value6")
                .Replace("{name7}", "value7")
                .Replace("{name8}", "value8")
                .Replace("{name9}", "value9")
                .Replace("{name10}", "value10"));
    }

    [Fact]
    public void Parse_edge_case1()
    {
        var str = "{name}";
        var replacements = new Dictionary<string, string>
        {
            { "name", "" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().Should().Be("");
    }

    [Fact]
    public void Parse_edge_case2()
    {
        var str = "{name";
        var replacements = new Dictionary<string, string>
        {
            { "name", "" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().Should().Be("{name");
    }

    [Fact]
    public void Parse_edge_case3()
    {
        var str = "name}";
        var replacements = new Dictionary<string, string>
        {
            { "name", "" }
        };
        DefaultResponseParser.FormatString(str.AsSpan(), replacements).ToString().Should().Be("name}");
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 _-*/*?=()/&%$#\"!";
        var random = new Random(DateTime.Now.Millisecond);

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
