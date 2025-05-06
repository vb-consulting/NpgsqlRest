using System.Net.Http.Headers;
using System.Text.Json;
using Npgsql;

namespace NpgsqlRestTests;

public static partial class Database
{
    public static void FileSystemUploadTests()
    {
        script.Append(@"
        create function fs_simple_upload(
            _meta json = null
        )
        returns json 
        language plpgsql
        as 
        $$
        begin
            return _meta;
        end;
        $$;

        comment on function fs_simple_upload(json) is '
        upload for file_system
        param _meta is upload metadata
        ';

        create function fs_custom_parameter_upload(
            _path text,
            _file text,
            _unique_name boolean,
            _create_path boolean,
            _meta json = null
        )
        returns json 
        language plpgsql
        as 
        $$
        begin
            return _meta;
        end;
        $$;

        comment on function fs_custom_parameter_upload(text, text, boolean, boolean, json) is '
        upload for file_system
        param _meta is upload metadata
        path = {_path}
        file = {_file}
        unique_name = {_unique_name}
        create_path = {_create_path}
        ';
");
    }
}

[Collection("TestFixture")]
public class FileSystemUploadTests(TestFixture test)
{
    [Fact]
    public async Task Test_fs_simple_upload_test1()
    {
        var fileName = "test-data.csv";
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Value");
        sb.AppendLine("1,Item 1,100");
        sb.AppendLine("2,Item 2,200");
        sb.AppendLine("3,Item 3,300");
        var csvContent = sb.ToString();
        var contentBytes = Encoding.UTF8.GetBytes(csvContent);
        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(contentBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-simple-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();

        var jsonDoc = JsonDocument.Parse(response);
        var rootElement = jsonDoc.RootElement[0]; // Get the first object in the array
        rootElement.GetProperty("type").GetString().Should().Be("file_system", "because the type should match the expected value");
        rootElement.GetProperty("fileName").GetString().Should().Be("test-data.csv", "because the fileName should match the expected value");
        rootElement.GetProperty("contentType").GetString().Should().Be("text/csv", "because the contentType should match the expected value");
        rootElement.GetProperty("size").GetInt32().Should().BeOneOf(53, 57);

        var filePath = rootElement.GetProperty("filePath").GetString();

        var fileExtension = Path.GetExtension(filePath);
        Path.GetExtension(filePath).Should().Be(".csv", "because the file extension should be .csv");
        Path.GetFileName(filePath).Should().NotBe("test-data.csv", "because the file name should be unique");

        Path.GetDirectoryName(filePath).Should().Be("."); // the default path 
        File.Exists(filePath).Should().BeTrue("because the file should exist at the specified path");
        File.ReadAllText(filePath!).Should().Be(csvContent, "because the file content should match the original content");
    }

    [Fact]
    public async Task Test_fs_custom_parameter_upload_test1()
    {
        var fileName = "test-data.csv";
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Value");
        sb.AppendLine("1,Item 1,100");
        sb.AppendLine("2,Item 2,200");
        sb.AppendLine("3,Item 3,300");
        var csvContent = sb.ToString();
        var contentBytes = Encoding.UTF8.GetBytes(csvContent);
        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(contentBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(byteContent, "file", fileName);

        var query = new QueryBuilder
        {
            { "path", "./test" },
            { "file", fileName },
            { "uniqueName", "false" },
            { "createPath", "true" },
        };

        using var result = await test.Client.PostAsync($"/api/fs-custom-parameter-upload/{query}", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();

        var jsonDoc = JsonDocument.Parse(response);
        var rootElement = jsonDoc.RootElement[0]; // Get the first object in the array
        rootElement.GetProperty("type").GetString().Should().Be("file_system", "because the type should match the expected value");
        rootElement.GetProperty("fileName").GetString().Should().Be("test-data.csv", "because the fileName should match the expected value");
        rootElement.GetProperty("contentType").GetString().Should().Be("text/csv", "because the contentType should match the expected value");
        rootElement.GetProperty("size").GetInt32().Should().BeOneOf(53, 57);
        rootElement.GetProperty("filePath").GetString().Should()
            .BeOneOf("./test/test-data.csv", "./test\\test-data.csv");

        var filePath = rootElement.GetProperty("filePath").GetString();

        var fileExtension = Path.GetExtension(filePath);
        Path.GetExtension(filePath).Should().Be(".csv", "because the file extension should be .csv");
        Path.GetFileName(filePath).Should().Be("test-data.csv");

        File.Exists(filePath).Should().BeTrue("because the file should exist at the specified path");
        File.ReadAllText(filePath!).Should().Be(csvContent, "because the file content should match the original content");
    }
}