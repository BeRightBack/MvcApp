using System.Text;
using Microsoft.AspNetCore.Http;
using MvcApp.Common.Uploads;
using Xunit;

namespace MvcApp.Tests.Unit;

/// <summary>
/// Guards audit 2.2: the upload endpoints used to trust a client-supplied Content-Type and then
/// take the stored extension from the uploaded filename, so "evil.html" sent as image/png survived
/// validation, was written under wwwroot as .html, and was served as text/html — stored XSS in the
/// site's own origin. Detection must come from the bytes, and the extension from the detected type.
/// </summary>
public class FileSignatureTests
{
    private static IFormFile File(byte[] content, string fileName, string contentType)
        => new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    [Fact]
    public async Task Html_declared_as_png_is_rejected()
    {
        var bytes = Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>");

        var detected = await FileSignature.DetectImageAsync(File(bytes, "evil.html", "image/png"));

        Assert.False(detected.IsValid);
    }

    [Fact]
    public async Task Svg_is_rejected_as_an_image()
    {
        var bytes = Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'><script/></svg>");

        var detected = await FileSignature.DetectImageAsync(File(bytes, "evil.svg", "image/svg+xml"));

        Assert.False(detected.IsValid);
    }

    [Fact]
    public async Task Png_bytes_yield_a_png_extension_regardless_of_the_file_name()
    {
        // A genuine PNG header. The caller claims it is a .html file; the stored name must be .png.
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

        var detected = await FileSignature.DetectImageAsync(File(png, "totally-a-photo.html", "text/html"));

        Assert.True(detected.IsValid);
        Assert.Equal(".png", detected.Extension);
        Assert.Equal("image/png", detected.ContentType);
    }

    [Fact]
    public async Task Jpeg_bytes_are_detected()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

        var detected = await FileSignature.DetectImageAsync(File(jpeg, "x.jpg", "image/jpeg"));

        Assert.True(detected.IsValid);
        Assert.Equal(".jpg", detected.Extension);
    }

    [Fact]
    public async Task Webp_bytes_are_detected()
    {
        byte[] webp = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];

        var detected = await FileSignature.DetectImageAsync(File(webp, "x.webp", "image/webp"));

        Assert.True(detected.IsValid);
        Assert.Equal(".webp", detected.Extension);
    }

    [Fact]
    public async Task Mp4_bytes_are_detected_as_video()
    {
        byte[] mp4 = [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D];

        var detected = await FileSignature.DetectVideoAsync(File(mp4, "clip.html", "text/html"));

        Assert.True(detected.IsValid);
        Assert.Equal(".mp4", detected.Extension);
    }
}
