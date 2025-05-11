using System.Globalization;

namespace NpgsqlRestTests.ParserTests;

public class SerializerTests
{
    [Fact]
    public void SerializeDatbaseObject_NullValue_ReturnsNullConstant()
    {
        // Arrange
        object? value = null;

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void SerializeDatbaseObject_DBNullValue_ReturnsNullConstant()
    {
        // Arrange
        object value = DBNull.Value;

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void SerializeDatbaseObject_StringValue_ReturnsJsonString()
    {
        // Arrange
        string value = "test string";

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("\"test string\"");
    }

    [Fact]
    public void SerializeDatbaseObject_EmptyString_ReturnsEmptyJsonString()
    {
        // Arrange
        string value = string.Empty;

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("\"\"");
    }

    [Fact]
    public void SerializeDatbaseObject_IntegerValue_ReturnsUnquotedNumber()
    {
        // Arrange
        int value = 42;

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void SerializeDatbaseObject_DecimalValue_ReturnsUnquotedNumberWithDot()
    {
        // Arrange
        decimal value = 123.45m;

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("123.45");
    }

    [Fact]
    public void SerializeDatbaseObject_DecimalValue_UsesDotRegardlessOfCurrentCulture()
    {
        // Arrange
        decimal value = 123.45m;
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            // Use a culture that typically uses comma as decimal separator
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            // Act
            string result = PgConverters.SerializeDatbaseObject(value);

            // Assert
            result.Should().Be("123.45"); // Should still use dot, not comma
        }
        finally
        {
            // Restore original culture
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void SerializeDatbaseObject_BooleanValue_ReturnsUnquotedLowercaseBoolean()
    {
        // Arrange
        bool value = true;

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("true");
    }

    [Fact]
    public void SerializeDatbaseObject_DateTimeValue_ReturnsISOFormattedDate()
    {
        // Arrange
        DateTime value = new(2023, 4, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("\"2023-04-15T10:30:00.0000000Z\"");
    }

    [Fact]
    public void SerializeDatbaseObject_GuidValue_ReturnsQuotedString()
    {
        // Arrange
        Guid value = Guid.NewGuid();

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be($"\"{value}\"");
    }

    [Fact]
    public void SerializeDatbaseObject_IntArray_ReturnsJsonArray()
    {
        // Arrange
        int[] value = [1, 2, 3];

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("[1,2,3]");
    }

    [Fact]
    public void SerializeDatbaseObject_StringArray_ReturnsJsonArray()
    {
        // Arrange
        string[] value = ["one", "two", "three"];

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("[\"one\",\"two\",\"three\"]");
    }

    [Fact]
    public void SerializeDatbaseObject_MixedTypeArray_ReturnsJsonArray()
    {
        // Arrange
        object?[] value = [1, "two", true, null];

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("[1,\"two\",true,null]");
    }

    [Fact]
    public void SerializeDatbaseObject_SpecialCharacters_HandlesEscaping()
    {
        // Arrange
        string value = "test\"quote";

        // Act
        string result = PgConverters.SerializeDatbaseObject(value);

        // Assert
        result.Should().Be("\"test\\\"quote\"");
    }
}