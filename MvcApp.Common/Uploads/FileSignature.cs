using Microsoft.AspNetCore.Http;

namespace MvcApp.Common.Uploads;

/// <summary>
/// Identifies an uploaded file from its CONTENT, never from what the client claims.
///
/// Why this exists: the upload endpoints trusted <c>IFormFile.ContentType</c> (a client-supplied
/// header) and then took the stored extension from the user's filename. Sending "evil.html" with
/// <c>Content-Type: image/png</c> therefore passed validation and was written under wwwroot as
/// .html, where it was served as text/html and executed in the site's own origin — stored XSS
/// (audit 2.2). Sniffing the bytes and deriving the extension from the detected type removes both
/// halves of that: the claim is ignored, and the stored name can only be an image/video extension.
/// </summary>
public static class FileSignature
{
    public readonly record struct Detection(bool IsValid, string Extension, string ContentType)
    {
        public static Detection Invalid { get; } = new(false, string.Empty, string.Empty);
    }

    public static async Task<Detection> DetectImageAsync(IFormFile file)
    {
        var head = await ReadHeadAsync(file, 12);

        // JPEG  FF D8 FF
        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
        {
            return new Detection(true, ".jpg", "image/jpeg");
        }

        // PNG  89 50 4E 47 0D 0A 1A 0A
        if (head.Length >= 8 &&
            head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 &&
            head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A)
        {
            return new Detection(true, ".png", "image/png");
        }

        // GIF  47 49 46 38 ("GIF8")
        if (head.Length >= 6 && head[0] == 0x47 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x38)
        {
            return new Detection(true, ".gif", "image/gif");
        }

        // WebP RIFF....WEBP
        if (head.Length >= 12 &&
            head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46 &&
            head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50)
        {
            return new Detection(true, ".webp", "image/webp");
        }

        return Detection.Invalid;
    }

    public static async Task<Detection> DetectVideoAsync(IFormFile file)
    {
        var head = await ReadHeadAsync(file, 12);

        // ISO base media (MP4 / QuickTime): "....ftyp"
        if (head.Length >= 12 &&
            head[4] == 0x66 && head[5] == 0x74 && head[6] == 0x79 && head[7] == 0x70)
        {
            return new Detection(true, ".mp4", "video/mp4");
        }

        // WebM / Matroska EBML header: 1A 45 DF A3
        if (head.Length >= 4 && head[0] == 0x1A && head[1] == 0x45 && head[2] == 0xDF && head[3] == 0xA3)
        {
            return new Detection(true, ".webm", "video/webm");
        }

        return Detection.Invalid;
    }

    private static async Task<byte[]> ReadHeadAsync(IFormFile file, int count)
    {
        await using var stream = file.OpenReadStream();

        var buffer = new byte[count];
        var read = 0;
        while (read < count)
        {
            var chunk = await stream.ReadAsync(buffer.AsMemory(read, count - read));
            if (chunk == 0)
            {
                break;
            }

            read += chunk;
        }

        return read == count ? buffer : buffer[..read];
    }
}
