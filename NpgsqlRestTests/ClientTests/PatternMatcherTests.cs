using NpgsqlRestClient;

namespace NpgsqlRestTests.ClientTests;

public class PatternMatcherTests
{
    [Theory]
    [InlineData("", "", false)]
    [InlineData("test", "", false)]
    [InlineData("", "test", false)]
    [InlineData(null, "test", false)]
    [InlineData("test", null, false)]
    [InlineData(null, null, false)]
    public void EdgeCases_EmptyAndNull_ReturnFalse(string name, string pattern, bool expected)
    {
        DefaultResponseParser.IsPatternMatch(name, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "test", true)]
    [InlineData("test", "TEST", true)]
    [InlineData("a", "b", false)]
    [InlineData("abc", "abcd", false)]
    public void ExactMatches_WithoutWildcards(string name, string pattern, bool expected)
    {
        DefaultResponseParser.IsPatternMatch(name, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("test.txt", "*.txt", true)]
    [InlineData("test.doc", "*.txt", false)]
    [InlineData("file", "*", true)]
    [InlineData("", "*", false)]
    [InlineData("abc", "a*", true)]
    [InlineData("abc", "*c", true)]
    [InlineData("abc", "a*c", true)]
    [InlineData("abcdef", "*d*f", true)]
    [InlineData("abc", "*d", false)]
    public void StarWildcard_MatchesCorrectly(string name, string pattern, bool expected)
    {
        DefaultResponseParser.IsPatternMatch(name, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "t?st", true)]
    [InlineData("test", "te?t", true)]
    [InlineData("test", "????", true)]
    [InlineData("test", "tes?", true)]
    [InlineData("test", "?est", true)]
    [InlineData("abc", "a?c?", false)]
    [InlineData("abc", "??", false)]
    [InlineData("abc", "a?d", false)]
    public void QuestionMarkWildcard_MatchesCorrectly(string name, string pattern, bool expected)
    {
        DefaultResponseParser.IsPatternMatch(name, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("testfile.txt", "t*t", true)]
    [InlineData("abcde", "a*c?e", true)]
    [InlineData("abcde", "*?e", true)]
    [InlineData("x", "*?*", true)]
    [InlineData("abcdef", "a*d?f", true)]
    [InlineData("a", "**", true)]
    [InlineData("abc", "a**c", true)]
    public void CombinedWildcards_MatchesCorrectly(string name, string pattern, bool expected)
    {
        DefaultResponseParser.IsPatternMatch(name, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("verylongfilename.txt", "*.txt", true)]
    [InlineData("a", "************************a", true)]
    [InlineData("abc", "???", true)]
    [InlineData("special@#$%.txt", "*@#$%.txt", true)]
    public void ExtremeCases_MatchesCorrectly(string name, string pattern, bool expected)
    {
        DefaultResponseParser.IsPatternMatch(name, pattern).Should().Be(expected);
    }

    [Fact]
    public void PerformanceTest_LargeInput_DoesNotStackOverflow()
    {
        string largeName = new string('a', 10000);
        string largePattern = "*" + new string('?', 9999);
        Assert.True(DefaultResponseParser.IsPatternMatch(largeName, largePattern));
    }

    [Theory]
    [InlineData("test", "TEST", true)]
    [InlineData("Test", "TEST", true)]
    [InlineData("test", "tEsT", true)]
    [InlineData("TEST", "t?st", true)]
    [InlineData("TEST", "t*st", true)]
    [InlineData("TEST", "*st", true)]
    public void CaseInsensitive_MatchesCorrectly(string name, string pattern, bool expected)
    {
        DefaultResponseParser.IsPatternMatch(name, pattern).Should().Be(expected);
    }
}
