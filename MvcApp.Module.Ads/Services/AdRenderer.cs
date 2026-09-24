using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Services;

/// <summary>
/// Renders ad zones for a given page. Handles scheduling, targeting, exclusions, and tracking.
/// </summary>
public interface IAdRenderer
{
    /// <summary>
    /// Renders a single ad zone by key for the current page context.
    /// </summary>
    Task<string> RenderZoneAsync(string zoneKey, string pageSlug, bool isLandingPage, ClaimsPrincipal? user, string culture, HttpContext? httpContext = null);

    /// <summary>
    /// Renders all active zones for a page, returning a dictionary of zoneKey -> HTML.
    /// </summary>
    Task<Dictionary<string, string>> RenderAllZonesAsync(string pageSlug, bool isLandingPage, ClaimsPrincipal? user, string culture, HttpContext? httpContext = null);

    /// <summary>
    /// Gets the raw banners for a zone (for template-based rendering).
    /// </summary>
    Task<IReadOnlyList<AdBanner>> GetBannersForZoneAsync(string zoneKey, string pageSlug, bool isLandingPage, ClaimsPrincipal? user, string culture);
}

public class AdRenderer : IAdRenderer
{
    private readonly IAdZoneService _zoneService;
    private readonly IAdBannerService _bannerService;
    private readonly IAdPlacementService _placementService;
    private readonly IAdTrackingService _trackingService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdRenderer> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdRenderer(
        IAdZoneService zoneService,
        IAdBannerService bannerService,
        IAdPlacementService placementService,
        IAdTrackingService trackingService,
        IMemoryCache cache,
        ILogger<AdRenderer> logger,
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory)
    {
        _zoneService = zoneService;
        _bannerService = bannerService;
        _placementService = placementService;
        _trackingService = trackingService;
        _cache = cache;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }

    public async Task<string> RenderZoneAsync(string zoneKey, string pageSlug, bool isLandingPage, ClaimsPrincipal? user, string culture, HttpContext? httpContext = null)
    {
        var zone = await _zoneService.GetByKeyAsync(zoneKey);
        if (zone == null || !zone.IsActive)
            return string.Empty;

        // Check zone-level landing page exclusion
        if (isLandingPage && zone.ExcludeFromLandingPage)
            return string.Empty;

        // Check placement exclusion for this page
        var placement = await _placementService.GetForPageAsync(zone.Id, pageSlug);
        if (placement?.IsExclusion == true)
            return string.Empty;

        // Also check wildcard exclusion
        if (placement?.PageSlug != pageSlug)
        {
            var wildcardPlacement = await _placementService.GetForPageAsync(zone.Id, "*");
            if (wildcardPlacement?.IsExclusion == true && placement?.PageSlug != pageSlug)
                return string.Empty;
        }

        var banners = await GetBannersForZoneAsync(zoneKey, pageSlug, isLandingPage, user, culture);

        // Apply max banners limit (placement override or zone default).
        // When a zone has more active banners than slots, ALL are rendered inside a
        // rotation container and the browser cycles them at a regular interval,
        // starting from the highest-weight banners.
        var maxBanners = placement?.MaxBannersOverride ?? zone.MaxBanners;
        var rotate = maxBanners > 0 && banners.Count > maxBanners;
        var visibleBanners = rotate ? banners.Take(maxBanners).ToList() : banners;

        if (banners.Count == 0)
            return string.Empty;

        // Determine CSS class and wrapper
        var cssClass = placement?.CssClassOverride ?? zone.DefaultCssClass ?? $"ad-zone ad-{zoneKey}";
        var wrapperTemplate = placement?.WrapperTemplateOverride ?? zone.BannerWrapperTemplate;

        var html = new StringBuilder();
        var rotateAttrs = rotate
            ? $" data-ad-rotate=\"6000\" data-ad-rotate-count=\"{maxBanners}\""
            : string.Empty;
        html.AppendLine($"<div class=\"{cssClass}\" data-ad-zone=\"{zoneKey}\"{rotateAttrs}>");

        for (var i = 0; i < banners.Count; i++)
        {
            var bannerHtml = RenderBanner(banners[i], zoneKey, pageSlug, httpContext);
            if (!string.IsNullOrEmpty(wrapperTemplate))
            {
                bannerHtml = string.Format(wrapperTemplate, bannerHtml);
            }

            if (rotate)
            {
                var hidden = i >= maxBanners ? " style=\"display:none\"" : string.Empty;
                html.AppendLine($"<div class=\"ad-rotate-item\"{hidden}>{bannerHtml}</div>");
            }
            else
            {
                html.AppendLine(bannerHtml);
            }
        }

        html.AppendLine("</div>");

        if (rotate)
        {
            html.AppendLine("<script src=\"/_content/MvcApp.Module.Ads/js/ads-rotate.js\" defer></script>");
        }

        // Queue impression tracking (fire-and-forget) — only for banners visible on
        // initial render; rotating banners fire impression beacons as they appear.
        // Capture request values now and resolve scoped services from a NEW scope:
        // the request scope (and its DbContext) is disposed when the response completes.
        var ctx = httpContext ?? _httpContextAccessor.HttpContext;
        var ipHash = HashIp(ctx?.Connection.RemoteIpAddress?.ToString());
        var ua = ctx?.Request.Headers.UserAgent.ToString();
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var bannerIds = visibleBanners.Select(b => b.Id).ToList();

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var tracking = scope.ServiceProvider.GetRequiredService<IAdTrackingService>();
                foreach (var bannerId in bannerIds)
                {
                    await tracking.RecordImpressionAsync(bannerId, pageSlug, zoneKey, ipHash, ua, culture, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record ad impressions for zone {ZoneKey}", zoneKey);
            }
        });

        return html.ToString();
    }

    public async Task<Dictionary<string, string>> RenderAllZonesAsync(string pageSlug, bool isLandingPage, ClaimsPrincipal? user, string culture, HttpContext? httpContext = null)
    {
        var zones = await _zoneService.GetAllAsync(true);
        var result = new Dictionary<string, string>();

        foreach (var zone in zones)
        {
            var html = await RenderZoneAsync(zone.Key, pageSlug, isLandingPage, user, culture, httpContext);
            if (!string.IsNullOrEmpty(html))
                result[zone.Key] = html;
        }

        return result;
    }

    public async Task<IReadOnlyList<AdBanner>> GetBannersForZoneAsync(string zoneKey, string pageSlug, bool isLandingPage, ClaimsPrincipal? user, string culture)
    {
        var zone = await _zoneService.GetByKeyAsync(zoneKey);
        if (zone == null || !zone.IsActive)
            return Array.Empty<AdBanner>();

        if (isLandingPage && zone.ExcludeFromLandingPage)
            return Array.Empty<AdBanner>();

        var placement = await _placementService.GetForPageAsync(zone.Id, pageSlug);
        if (placement?.IsExclusion == true)
            return Array.Empty<AdBanner>();

        var wildcardPlacement = await _placementService.GetForPageAsync(zone.Id, "*");
        if (wildcardPlacement?.IsExclusion == true && placement?.PageSlug != pageSlug)
            return Array.Empty<AdBanner>();

        return await _bannerService.GetByZoneAsync(zone.Id, true, culture, user);
    }

    private string RenderBanner(AdBanner banner, string zoneKey, string pageSlug, HttpContext? httpContext)
    {
        var clickUrl = $"/ads/click/{banner.Id}?zone={Uri.EscapeDataString(zoneKey)}&page={Uri.EscapeDataString(pageSlug)}";
        var cssClass = string.IsNullOrWhiteSpace(banner.CssClass) ? "ad-banner" : $"ad-banner {banner.CssClass}";

        return banner.Type switch
        {
            AdBannerType.Image => RenderImageBanner(banner, clickUrl, cssClass),
            AdBannerType.Html => RenderHtmlBanner(banner, clickUrl, cssClass),
            AdBannerType.Script => RenderScriptBanner(banner, cssClass),
            AdBannerType.Text => RenderTextBanner(banner, clickUrl, cssClass),
            _ => string.Empty
        };
    }

    private string RenderImageBanner(AdBanner banner, string clickUrl, string cssClass)
    {
        var imgUrl = banner.Content ?? "";
        var alt = banner.AltText ?? banner.Name;
        var target = !string.IsNullOrWhiteSpace(banner.TargetUrl) ? $"href=\"{clickUrl}\"" : "";
        var linkStart = !string.IsNullOrWhiteSpace(banner.TargetUrl) ? $"<a {target} class=\"ad-banner-link\" data-ad-banner=\"{banner.Id}\">" : "";
        var linkEnd = !string.IsNullOrWhiteSpace(banner.TargetUrl) ? "</a>" : "";

        return $"<div class=\"{cssClass} ad-banner-image\" data-ad-banner=\"{banner.Id}\">{linkStart}<img src=\"{imgUrl}\" alt=\"{alt}\" loading=\"lazy\" />{linkEnd}</div>";
    }

    private string RenderHtmlBanner(AdBanner banner, string clickUrl, string cssClass)
    {
        var html = banner.Content ?? "";
        // Rewrite relative links to go through click tracker
        if (!string.IsNullOrWhiteSpace(banner.TargetUrl))
        {
            html = Regex.Replace(html, @"href\s*=\s*[""']([^""']*)[""']", m =>
                $"href=\"{clickUrl}&redirect={Uri.EscapeDataString(m.Groups[1].Value)}\"");
        }
        return $"<div class=\"{cssClass} ad-banner-html\" data-ad-banner=\"{banner.Id}\">{html}</div>";
    }

    private string RenderScriptBanner(AdBanner banner, string cssClass)
    {
        // For script banners (AdSense, etc.), render the script tag directly
        var script = banner.Content ?? "";
        return $"<div class=\"{cssClass} ad-banner-script\" data-ad-banner=\"{banner.Id}\">{script}</div>";
    }

    private string RenderTextBanner(AdBanner banner, string clickUrl, string cssClass)
    {
        var text = banner.Content ?? banner.Name;
        var link = !string.IsNullOrWhiteSpace(banner.TargetUrl)
            ? $"<a href=\"{clickUrl}\" class=\"ad-banner-link\" data-ad-banner=\"{banner.Id}\">{text}</a>"
            : text;
        return $"<div class=\"{cssClass} ad-banner-text\" data-ad-banner=\"{banner.Id}\">{link}</div>";
    }

    private static string HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "unknown";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(hash)[..16];
    }
}