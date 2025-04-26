using System.Net.Http.Headers;
using System.Text.Json;
using Npgsql;

namespace NpgsqlRestTests;

public static partial class Database
{
    public static void UploadTests()
    {
        script.Append(@"
        create function simple_upload(
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

        comment on function simple_upload(json) is 'upload _meta as metadata';
");
    }
}

[Collection("TestFixture")]
public class UploadTests(TestFixture test)
{
    [Fact]
    public async Task Test_simple_upload_test1()
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

        using var result = await test.Client.PostAsync("/api/simple-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();

        var jsonDoc = JsonDocument.Parse(response);
        var rootElement = jsonDoc.RootElement[0]; // Get the first object in the array
        rootElement.GetProperty("type").GetString().Should().Be("large_object", "because the type should match the expected value");
        rootElement.GetProperty("fileName").GetString().Should().Be("test-data.csv", "because the fileName should match the expected value");
        rootElement.GetProperty("contentType").GetString().Should().Be("text/csv", "because the contentType should match the expected value");
        rootElement.GetProperty("size").GetInt32().Should().Be(57, "because the size should match the expected value");
        rootElement.GetProperty("oid").ValueKind.Should().Be(JsonValueKind.Number, "because oid should be a number");
        rootElement.GetProperty("oid").TryGetInt32(out _).Should().BeTrue("because oid should be a valid integer");

        var oid = rootElement.GetProperty("oid").GetInt32();
        using var connection = Database.CreateConnection();
        await connection.OpenAsync();
        using (var command = new NpgsqlCommand("select * from pg_largeobject_metadata where oid = " + oid, connection))
        {
            using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue(); // there is a record
        }
        using (var command = new NpgsqlCommand("select convert_from(lo_get(" + oid + "), 'utf8')", connection)) 
        {
            var content = (string?)await command.ExecuteScalarAsync();
            content.Should().Be(csvContent);
        }
        using (var command = new NpgsqlCommand("select * from pg_largeobject where convert_from(data, 'utf8') = $1", connection))
        {
            command.Parameters.Add(new NpgsqlParameter()
            {
                NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
                Value = csvContent
            });
            using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue(); // there is a record
        }
    }

    [Fact]
    public async Task Test_simple_upload_meta_param1()
    {
        var fileName = "test-data.csv";
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Value");
        sb.AppendLine("5,Item 5,500");
        sb.AppendLine("6,Item 6,600");
        sb.AppendLine("7,Item 7,700");
        var csvContent = sb.ToString();
        var contentBytes = Encoding.UTF8.GetBytes(csvContent);
        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(contentBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/simple-upload/?meta=this_content_is_ignored", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();

        var jsonDoc = JsonDocument.Parse(response);
        var rootElement = jsonDoc.RootElement[0]; // Get the first object in the array
        rootElement.GetProperty("type").GetString().Should().Be("large_object", "because the type should match the expected value");
        rootElement.GetProperty("fileName").GetString().Should().Be("test-data.csv", "because the fileName should match the expected value");
        rootElement.GetProperty("contentType").GetString().Should().Be("text/csv", "because the contentType should match the expected value");
        rootElement.GetProperty("size").GetInt32().Should().Be(57, "because the size should match the expected value");
        rootElement.GetProperty("oid").ValueKind.Should().Be(JsonValueKind.Number, "because oid should be a number");
        rootElement.GetProperty("oid").TryGetInt32(out _).Should().BeTrue("because oid should be a valid integer");

        var oid = rootElement.GetProperty("oid").GetInt32();
        using var connection = Database.CreateConnection();
        await connection.OpenAsync();
        using (var command = new NpgsqlCommand("select * from pg_largeobject_metadata where oid = " + oid, connection))
        {
            using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue(); // there is a record
        }
        using (var command = new NpgsqlCommand("select convert_from(lo_get(" + oid + "), 'utf8')", connection))
        {
            var content = (string?)await command.ExecuteScalarAsync();
            content.Should().Be(csvContent);
        }
        using (var command = new NpgsqlCommand("select * from pg_largeobject where convert_from(data, 'utf8') = $1", connection))
        {
            command.Parameters.Add(new NpgsqlParameter()
            {
                NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
                Value = csvContent
            });
            using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue(); // there is a record
        }
    }


    [Fact]
    public async Task Test_simple_upload_failed1()
    {
        var fileName = "test-data.csv";
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Value");
        sb.AppendLine("8,Item 8,500");
        sb.AppendLine("9,Item 9,600");
        sb.AppendLine("10,Item 10,1000");
        var csvContent = sb.ToString();
        var contentBytes = Encoding.UTF8.GetBytes(csvContent);
        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(contentBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/simple-upload/?unknown=unseen", formData);
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var response = await result.Content.ReadAsStringAsync();

        response.Should().BeEmpty();

        using var connection = Database.CreateConnection();
        await connection.OpenAsync();
        using var command = new NpgsqlCommand("select * from pg_largeobject where convert_from(data, 'utf8') = $1", connection);
        command.Parameters.Add(new NpgsqlParameter()
        {
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
            Value = csvContent
        });
        using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeFalse(); // there is a NO record, LOB has rolled-back
    }
}