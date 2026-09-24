using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using MvcApp.Module.Ads.Services;

namespace MvcApp.Module.Ads.Controllers;

[Route("ads")]
public class AdsController : Controller
{
    private readonly IAdTrackingService _trackingService;
    private readonly IAdBannerService _bannerService;
    private readonly ILogger<AdsController> _logger;

    public AdsController(IAdTrackingService trackingService, IAdBannerService bannerService, ILogger<AdsController> logger)
    {
        _trackingService = trackingService;
        _bannerService = bannerService;
        _logger = logger;
    }

    /// <summary>
    /// Tracks a click and redirects to the banner's target URL.
    /// </summary>
    [HttpGet("click/{bannerId:int}")]
    public async Task<IActionResult> Click(int bannerId, string zone, string page, string? redirect = null)
    {
        var banner = await _bannerService.GetByIdAsync(bannerId);
        if (banner == null || string.IsNullOrWhiteSpace(banner.TargetUrl))
        {
            _logger.LogWarning("Click on non-existent or URL-less banner {BannerId}", bannerId);
            return NotFound();
        }

        var ipHash = HashIp(HttpContext.Connection.RemoteIpAddress?.ToString());
        var referrer = Request.Headers.Referer.ToString();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await _trackingService.RecordClickAsync(bannerId, page, zone, ipHash, referrer, userId);

        var targetUrl = !string.IsNullOrWhiteSpace(redirect) ? redirect : banner.TargetUrl;
        return Redirect(targetUrl);
    }

    /// <summary>
    /// Client-side impression beacon (called via JS when banner enters viewport).
    /// </summary>
    [HttpPost("impression/{bannerId:int}")]
    public async Task<IActionResult> Impression(int bannerId, [FromBody] ImpressionDto dto)
    {
        var banner = await _bannerService.GetByIdAsync(bannerId);
        if (banner == null) return NotFound();

        var ipHash = HashIp(HttpContext.Connection.RemoteIpAddress?.ToString());
        var ua = Request.Headers.UserAgent.ToString();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await _trackingService.RecordImpressionAsync(bannerId, dto.PageSlug, dto.ZoneKey, ipHash, ua, dto.Culture, userId);

        return Ok();
    }

    private static string HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "unknown";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(hash)[..16];
    }
}

public record ImpressionDto(string PageSlug, string ZoneKey, string Culture);