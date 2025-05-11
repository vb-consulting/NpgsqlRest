using Microsoft.AspNetCore.Http;
using NpgsqlRest.UploadHandlers;


namespace NpgsqlRestTests.UploadTests;

public class FileStatusCheckerTests
{
    [Fact]
    public async Task CheckFileStatus_EmptyFile_ReturnsEmpty()
    {
        // Arrange
        var formFile = CreateFormFile([], "empty.txt");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.Empty);
    }

    [Fact]
    public async Task CheckFileStatus_WithNullBytes_ReturnsProbablyBinary()
    {
        // Arrange
        byte[] content = [72, 101, 108, 108, 111, 0, 87, 111, 114, 108, 100]; // "Hello\0World"
        var formFile = CreateFormFile(content, "binary.bin");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.ProbablyBinary);
    }

    [Fact]
    public async Task CheckFileStatus_WithNonPrintableChars_ReturnsProbablyBinary()
    {
        // Arrange
        var content = new List<byte>();
        content.AddRange(Encoding.ASCII.GetBytes("Hello World"));
        content.AddRange(new byte[] { 1, 2, 3, 4, 5, 6 }); // Add non-printable characters

        var formFile = CreateFormFile(content.ToArray(), "binary-nonprint.bin");

        // Act
        var result = await formFile.CheckFileStatus(nonPrintableThreshold: 5);

        // Assert
        result.Should().Be(UploadFileStatus.ProbablyBinary);
    }

    [Fact]
    public async Task CheckFileStatus_WithNewLines_ReturnsOk()
    {
        // Arrange
        string content = "Line 1\nLine 2\nLine 3";
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "text.txt");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.Ok);
    }

    [Fact]
    public async Task CheckFileStatus_WithoutNewLines_ReturnsNoNewLines()
    {
        // Arrange
        string content = "This is a single line text file without any newlines";
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "singleline.txt");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.NoNewLines);
    }

    [Fact]
    public async Task CheckFileStatus_WithoutNewLinesButCheckNewLinesFalse_ReturnsOk()
    {
        // Arrange
        string content = "This is a single line text file without any newlines";
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "singleline.txt");

        // Act
        var result = await formFile.CheckFileStatus(checkNewLines: false);

        // Assert
        result.Should().Be(UploadFileStatus.Ok);
    }

    [Fact]
    public async Task CheckFileStatus_WithCRLFLineEndings_ReturnsOk()
    {
        // Arrange
        string content = "Line 1\r\nLine 2\r\nLine 3";
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "crlf.txt");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.Ok);
    }

    [Fact]
    public async Task CheckFileStatus_WithOnlyCarriageReturns_ReturnsNoNewLines()
    {
        // Arrange
        string content = "Line 1\rLine 2\rLine 3";
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "cr-only.txt");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.NoNewLines);
    }

    [Fact]
    public async Task CheckFileStatus_WithSmallNonPrintableCount_ReturnsOk()
    {
        // Arrange
        var content = new List<byte>();
        // Add regular text content
        content.AddRange(Encoding.ASCII.GetBytes("Hello\nWorld"));
        // Add a few non-printable characters (but below threshold)
        content.AddRange(new byte[] { 1, 2 });

        var formFile = CreateFormFile(content.ToArray(), "few-nonprint.txt");

        // Act
        var result = await formFile.CheckFileStatus(nonPrintableThreshold: 5);

        // Assert
        result.Should().Be(UploadFileStatus.Ok);
    }

    [Fact]
    public async Task CheckFileStatus_WithCustomBuffer_ReadsCorrectAmount()
    {
        // Arrange
        var builder = new StringBuilder();
        // Create a large text file (over default buffer size)
        for (int i = 0; i < 100; i++)
        {
            builder.Append($"Line {i}\n");
        }

        string content = builder.ToString();
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "large.txt");

        // Act - use a smaller buffer size
        var result = await formFile.CheckFileStatus(testBufferSize: 1024);

        // Assert
        result.Should().Be(UploadFileStatus.Ok);
    }

    [Fact]
    public async Task CheckFileStatus_WithUTF8Characters_ReturnsOk()
    {
        // Arrange
        string content = "Unicode test: こんにちは\n你好\nПривет";
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "utf8.txt");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.Ok);
    }

    [Fact]
    public async Task CheckFileStatus_WithCSVContent_ReturnsOk()
    {
        // Arrange
        string content = "id,name,email\n1,John Doe,john@example.com\n2,Jane Smith,jane@example.com";
        var formFile = CreateFormFile(Encoding.UTF8.GetBytes(content), "data.csv");

        // Act
        var result = await formFile.CheckFileStatus();

        // Assert
        result.Should().Be(UploadFileStatus.Ok);
    }

    [Fact]
    public async Task CheckFileStatus_WithLargeNonPrintableButBelowThreshold_ReturnsOk()
    {
        // Arrange
        var textContent = Encoding.ASCII.GetBytes("Sample text\nwith newlines\n");
        var content = new byte[textContent.Length + 10];
        Array.Copy(textContent, content, textContent.Length);
        // Add some non-printable characters (below increased threshold)
        for (int i = textContent.Length; i < textContent.Length + 10; i++)
        {
            content[i] = 31; // Non-printable ASCII
        }

        var formFile = CreateFormFile(content, "threshold-test.txt");

        // Act
        var result = await formFile.CheckFileStatus(nonPrintableThreshold: 20);

        // Assert
        result.Should().Be(UploadFileStatus.Ok);

        // Arrange
        //var content = "Sample text\nwith newlines\n";
        //for (int i = textContent.Length; i < textContent.Length + 10; i++)
        //{
        //    content[i] = 31; // Non-printable ASCII
        //}

        //var formFile = CreateFormFile(content, "threshold-test.txt");

        //// Act
        //var result = await formFile.CheckFileStatus(nonPrintableThreshold: 20);

        //// Assert
        //result.Should().Be(UploadFileStatus.Ok);
    }

    // Helper method to create an IFormFile without using Moq
    private IFormFile CreateFormFile(byte[] content, string fileName)
    {
        var stream = new MemoryStream(content);

        return new FormFile(
            baseStream: stream,
            baseStreamOffset: 0,
            length: content.Length,
            name: "file", // form field name
            fileName: fileName
        );
    }
}