using System.Net.Http.Headers;

namespace NpgsqlRestTests;

public static partial class Database
{
    public static void UploadFileChecksTests()
    {
        script.Append(@"
        create function fs_check_text_upload(
            _meta json
        )
        returns json 
        language plpgsql
        as 
        $$
        begin
            return _meta;
        end;
        $$;

        comment on function fs_check_text_upload(json) is '
        upload for file_system
        param _meta is upload metadata
        check_text = true 
        ';

        create function fs_check_image_upload(
            _meta json
        )
        returns json 
        language plpgsql
        as 
        $$
        begin
            return _meta;
        end;
        $$;

        comment on function fs_check_image_upload(json) is '
        upload for file_system
        param _meta is upload metadata
        check_image = true 
        ';

        create function fs_check_jpg_and_gif_upload(
            _meta json
        )
        returns json 
        language plpgsql
        as 
        $$
        begin
            return _meta;
        end;
        $$;

        comment on function fs_check_jpg_and_gif_upload(json) is '
        upload for file_system
        param _meta is upload metadata
        check_image = jpg, gif 
        ';
");
    }
}

[Collection("TestFixture")]
public class UploadFileChecksTests(TestFixture test)
{
    [Fact]
    public async Task Test_fs_check_text_upload_text_test1()
    {
        var fileName = "test.txt";
        var sb = new StringBuilder();
        sb.AppendLine("Text Line1");
        sb.AppendLine("Text Line2");
        sb.AppendLine("Text Line3");
        sb.AppendLine("Text Line4");

        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(contentBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-text-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();

        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_text_upload_binary_test()
    {
        var fileName = "test-binary.dat";
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0xFF, 0xFE, 0xFD, 0xFC, 0xFB };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-text-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":false,\"status\":\"ProbablyBinary\"}]");
    }

    [Fact]
    public async Task Test_fs_check_image_upload_binary_test()
    {
        var fileName = "test-binary.dat";
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0xFF, 0xFE, 0xFD, 0xFC, 0xFB };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-image-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":false,\"status\":\"InvalidImage\"}]");
    }

    [Fact]
    public async Task Test_fs_check_image_upload_jpg_test()
    {
        var fileName = "test-binary.jpg";
        var binaryData = new byte[] {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xD9
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-image-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_image_upload_png_test()
    {
        var fileName = "test-binary.png";
        var binaryData = new byte[] {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00,
            0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x01, 0x35, 0xF2, 0x14, 0x24, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-image-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_image_upload_gif_test()
    {
        var fileName = "test-binary.gif";
        var binaryData = new byte[] {
            0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02,
            0x02, 0x44, 0x01, 0x00, 0x3B
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-image-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_image_upload_bmp_test()
    {
        var fileName = "test-binary.bmp";
        var binaryData = new byte[] {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00,
            0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-image-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_image_upload_tiff_test()
    {
        var fileName = "test-binary.tiff";
        var binaryData = new byte[] {
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x01,
            0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x01,
            0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x01,
            0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x01,
            0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01,
            0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x11, 0x01,
            0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x15, 0x01,
            0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x16, 0x01,
            0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x17, 0x01,
            0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x1C, 0x01,
            0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-image-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_image_upload_webp_test()
    {
        var fileName = "test-binary.webp";
        var binaryData = new byte[] {
            0x52, 0x49, 0x46, 0x46, 0x1A, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50,
            0x56, 0x50, 0x38, 0x20, 0x0E, 0x00, 0x00, 0x00, 0x30, 0x01, 0x00, 0x9D,
            0x01, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02, 0x00, 0x34, 0x25, 0xA4, 0x00,
            0x03, 0x70, 0x00, 0xFE, 0xFB, 0xFD, 0x50, 0x00
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-image-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_jpg_and_gif_upload_test_binary()
    {
        var fileName = "test-binary.dat";
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0xFF, 0xFE, 0xFD, 0xFC, 0xFB };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-jpg-and-gif-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":false,\"status\":\"InvalidImage\"}]");
    }

    [Fact]
    public async Task Test_fs_check_jpg_and_gif_upload_test_jpeg()
    {
        var fileName = "test-binary.jpg";
        var binaryData = new byte[] {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xD9
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-jpg-and-gif-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_jpg_and_gif_upload_test_gif()
    {
        var fileName = "test-binary.gif";
        var binaryData = new byte[] {
            0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02,
            0x02, 0x44, 0x01, 0x00, 0x3B
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-jpg-and-gif-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":true,\"status\":\"Ok\"}]");
    }

    [Fact]
    public async Task Test_fs_check_jpg_and_gif_upload_test_bmp()
    {
        var fileName = "test-binary.bmp";
        var binaryData = new byte[] {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00,
            0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00
        };

        using var formData = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(binaryData);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(byteContent, "file", fileName);

        using var result = await test.Client.PostAsync("/api/fs-check-jpg-and-gif-upload/", formData);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadAsStringAsync();
        response.Should().EndWith(",\"success\":false,\"status\":\"InvalidImage\"}]");
    }
}