using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace MvcApp.Module.Ads.Services;

public interface IAdMediaService
{
    /// <summary>
    /// Validates and saves an uploaded banner image under wwwroot/images/ads/{zoneKey}/,
    /// enforcing the zone's declared format (width × height) when set.
    /// </summary>
    Task<AdMediaResult> SaveBannerImageAsync(IFormFile file, string zoneKey, int? expectedWidth, int? expectedHeight);

    /// <summary>Deletes a previously uploaded banner image (only paths under /images/ads/).</summary>
    void DeleteBannerImage(string? contentPath);
}

public record AdMediaResult(bool Success, string? Error, string? Url);

public class AdMediaService : IAdMediaService
{
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg"];
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
    private const string AdsRoot = "images/ads";

    private readonly IWebHostEnvironment _env;

    public AdMediaService(IWebHostEnvironment env) => _env = env;

    public async Task<AdMediaResult> SaveBannerImageAsync(IFormFile file, string zoneKey, int? expectedWidth, int? expectedHeight)
    {
        if (file.Length == 0)
            return new(false, "The uploaded file is empty.", null);
        if (file.Length > MaxBytes)
            return new(false, $"The file exceeds the {MaxBytes / 1024 / 1024} MB limit.", null);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            return new(false, $"Unsupported file type '{ext}'. Allowed: {string.Join(", ", AllowedExtensions)}.", null);

        // Enforce the zone's declared format when the dimensions are readable (PNG/JPEG/GIF).
        if (expectedWidth.HasValue && expectedHeight.HasValue)
        {
            var dims = await TryReadDimensionsAsync(file);
            if (dims.HasValue && (dims.Value.Width != expectedWidth.Value || dims.Value.Height != expectedHeight.Value))
                return new(false, $"This zone expects {expectedWidth}×{expectedHeight} px images, but the uploaded file is {dims.Value.Width}×{dims.Value.Height} px.", null);
        }

        var safeZone = string.Concat(zoneKey.Where(c => char.IsLetterOrDigit(c) || c == '-'));
        if (string.IsNullOrEmpty(safeZone))
            return new(false, "Invalid zone key.", null);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absDir = Path.Combine(_env.WebRootPath, "images", "ads", safeZone);
        Directory.CreateDirectory(absDir);
        var absPath = Path.Combine(absDir, fileName);

        await using (var stream = File.Create(absPath))
            await file.CopyToAsync(stream);

        return new(true, null, $"/{AdsRoot}/{safeZone}/{fileName}");
    }

    public void DeleteBannerImage(string? contentPath)
    {
        if (string.IsNullOrWhiteSpace(contentPath)) return;
        var rel = contentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (!rel.StartsWith(AdsRoot.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;

        var abs = Path.GetFullPath(Path.Combine(_env.WebRootPath, rel));
        var allowedRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, AdsRoot.Replace('/', Path.DirectorySeparatorChar)));
        if (abs.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(abs))
            File.Delete(abs);
    }

    /// <summary>Reads image dimensions from PNG/JPEG/GIF headers. Returns null for other formats.</summary>
    private static async Task<(int Width, int Height)?> TryReadDimensionsAsync(IFormFile file)
    {
        var buffer = new byte[64 * 1024];
        int read;
        await using (var stream = file.OpenReadStream())
            read = await stream.ReadAsync(buffer, 0, buffer.Length);
        if (read < 26) return null;

        // PNG: 8-byte signature, IHDR, width/height big-endian at offset 16
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
        {
            var w = (buffer[16] << 24) | (buffer[17] << 16) | (buffer[18] << 8) | buffer[19];
            var h = (buffer[20] << 24) | (buffer[21] << 16) | (buffer[22] << 8) | buffer[23];
            return (w, h);
        }

        // GIF: "GIF8", width/height little-endian at offset 6
        if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38)
        {
            var w = buffer[6] | (buffer[7] << 8);
            var h = buffer[8] | (buffer[9] << 8);
            return (w, h);
        }

        // JPEG: scan for a Start-Of-Frame marker
        if (buffer[0] == 0xFF && buffer[1] == 0xD8)
        {
            var i = 2;
            while (i + 9 < read)
            {
                if (buffer[i] != 0xFF) { i++; continue; }
                var marker = buffer[i + 1];
                if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
                {
                    var h = (buffer[i + 5] << 8) | buffer[i + 6];
                    var w = (buffer[i + 7] << 8) | buffer[i + 8];
                    return (w, h);
                }
                if (marker == 0xD8 || marker == 0x01) { i += 2; continue; }
                var segLen = (buffer[i + 2] << 8) | buffer[i + 3];
                if (segLen < 2) break;
                i += 2 + segLen;
            }
        }

        return null;
    }
}
