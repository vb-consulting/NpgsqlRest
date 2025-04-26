namespace NpgsqlRestTests.ParserTests;

public class TimeSpanParserTests
{
    [Theory]
    [InlineData("10s", 0, 0, 10)]           // 10 seconds
    [InlineData("5m", 0, 5, 0)]             // 5 minutes
    [InlineData("5min", 0, 5, 0)]           // 5 minutes with full unit
    [InlineData("2h", 2, 0, 0)]             // 2 hours
    [InlineData("1d", 24, 0, 0)]            // 1 day
    [InlineData("1 d", 24, 0, 0)]            // 1 day
    [InlineData("1 D", 24, 0, 0)]            // 1 day
    [InlineData("2 hours", 2, 0, 0)]        // Space and full unit
    [InlineData("10 SECONDS", 0, 0, 10)]    // Upper case
    public void ParsePostgresInterval_ValidSimpleInputs_ReturnsCorrectTimeSpan(string input, int expectedHours, int expectedMinutes, int expectedSeconds)
    {
        // Act
        TimeSpan? result = TimeSpanParser.ParsePostgresInterval(input);

        // Assert
        TimeSpan expected = TimeSpan.FromHours(expectedHours) +
                          TimeSpan.FromMinutes(expectedMinutes) +
                          TimeSpan.FromSeconds(expectedSeconds);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.5h", 1, 30, 0)]         // 1.5 hours = 1 hour 30 minutes
    [InlineData("0.25d", 6, 0, 0)]         // 0.25 days = 6 hours
    [InlineData("2.5 m", 0, 2, 30)]        // 2.5 minutes = 2 minutes 30 seconds
    [InlineData("1.25s", 0, 0, 1.25)]      // 1.25 seconds
    public void ParsePostgresInterval_ValidDecimalInputs_ReturnsCorrectTimeSpan(string input, int expectedHours, int expectedMinutes, double expectedSeconds)
    {
        // Act
        TimeSpan? result = TimeSpanParser.ParsePostgresInterval(input);

        // Assert
        TimeSpan expected = TimeSpan.FromHours(expectedHours) +
                          TimeSpan.FromMinutes(expectedMinutes) +
                          TimeSpan.FromSeconds(expectedSeconds);
        result.Should().BeCloseTo(expected, TimeSpan.FromMilliseconds(1)); // 1ms tolerance
    }

    [Theory]
    [InlineData("0s", 0, 0, 0)]            // Zero seconds
    [InlineData("0.0h", 0, 0, 0)]          // Zero hours with decimal
    [InlineData("0001m", 0, 1, 0)]         // Leading zeros
    public void ParsePostgresInterval_EdgeCaseNumbers_ReturnsCorrectTimeSpan(string input, int expectedHours, int expectedMinutes, int expectedSeconds)
    {
        // Act
        TimeSpan? result = TimeSpanParser.ParsePostgresInterval(input);

        // Assert
        TimeSpan expected = TimeSpan.FromHours(expectedHours) +
                          TimeSpan.FromMinutes(expectedMinutes) +
                          TimeSpan.FromSeconds(expectedSeconds);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParsePostgresInterval_NullOrWhitespace_ThrowsArgumentNullException(string? input)
    {
        // Act
        TimeSpan? result = TimeSpanParser.ParsePostgresInterval(input!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("abc")]                    // No number
    [InlineData("5")]                      // No unit
    [InlineData("h5")]                     // Unit before number
    [InlineData("5.5.5h")]                 // Invalid number format
    [InlineData("5 m m")]                  // Multiple units
    public void ParsePostgresInterval_InvalidFormat_ThrowsFormatException(string input)
    {
        // Act
        TimeSpan? result = TimeSpanParser.ParsePostgresInterval(input);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("5x")]                     // Unknown unit
    [InlineData("2months")]                // Unsupported unit
    [InlineData("1year")]                  // Unsupported calendar unit
    public void ParsePostgresInterval_UnknownUnit_ThrowsFormatException(string input)
    {
        // Act
        TimeSpan? result = TimeSpanParser.ParsePostgresInterval(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParsePostgresInterval_CaseInsensitivity_WorksWithMixedCase()
    {
        // Arrange
        string[] inputs = ["10S", "5Min", "2HoUrS", "1DAY"];
        TimeSpan[] expected =
        [
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromHours(2),
            TimeSpan.FromDays(1)
        ];

        // Act & Assert
        for (int i = 0; i < inputs.Length; i++)
        {
            TimeSpan? result = TimeSpanParser.ParsePostgresInterval(inputs[i]);
            result.Should().Be(expected[i], because: $"input '{inputs[i]}' should parse correctly");
        }
    }
}