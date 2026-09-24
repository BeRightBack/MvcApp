using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MvcApp.Module.Ads.Entities;

/// <summary>
/// An ad zone represents a named position on a page where banners can be rendered.
/// Examples: "top-header", "sidebar-left", "content-top", "footer", etc.
/// </summary>
public class AdZone
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Key { get; set; } = string.Empty; // unique machine key, e.g. "top-header"

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty; // display name, e.g. "Top Header"

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Maximum number of banners to render in this zone at once (0 = unlimited).
    /// When a zone has more active banners than slots, they rotate (weighted by Weight).
    /// </summary>
    public int MaxBanners { get; set; } = 1;

    /// <summary>
    /// Expected banner image width in pixels for this zone (null = any size).
    /// </summary>
    public int? BannerWidth { get; set; }

    /// <summary>
    /// Expected banner image height in pixels for this zone (null = any size).
    /// </summary>
    public int? BannerHeight { get; set; }

    /// <summary>
    /// Display format, e.g. "970×250" (null when the zone accepts any size).
    /// </summary>
    [NotMapped]
    public string? Format => BannerWidth.HasValue && BannerHeight.HasValue ? $"{BannerWidth}×{BannerHeight}" : null;

    /// <summary>
    /// Whether this zone is excluded from the landing page (home page) by default.
    /// </summary>
    public bool ExcludeFromLandingPage { get; set; } = false;

    /// <summary>
    /// Default CSS classes applied to the zone container.
    /// </summary>
    [StringLength(200)]
    public string? DefaultCssClass { get; set; }

    /// <summary>
    /// Default wrapper HTML for each banner in this zone. Use {0} for banner HTML.
    /// </summary>
    [StringLength(500)]
    public string? BannerWrapperTemplate { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [BindNever]
    [ValidateNever]
    public virtual ICollection<AdBanner> Banners { get; set; } = new List<AdBanner>();
    [BindNever]
    [ValidateNever]
    public virtual ICollection<AdPlacement> Placements { get; set; } = new List<AdPlacement>();
}

/// <summary>
/// A single banner/ad creative that belongs to a zone.
/// Supports image, HTML, or external script (e.g. AdSense).
/// </summary>
public class AdBanner
{
    public int Id { get; set; }

    [Required]
    public int ZoneId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Banner type: Image, Html, Script, or Text.
    /// </summary>
    public AdBannerType Type { get; set; } = AdBannerType.Image;

    /// <summary>
    /// For Image type: image URL or path. For Html: raw HTML. For Script: script tag content.
    /// </summary>
    [StringLength(4000)]
    public string? Content { get; set; }

    /// <summary>
    /// Destination URL when banner is clicked (for Image/Html types).
    /// </summary>
    [StringLength(500)]
    public string? TargetUrl { get; set; }

    /// <summary>
    /// Alt text for image banners.
    /// </summary>
    [StringLength(200)]
    public string? AltText { get; set; }

    /// <summary>
    /// CSS classes for the banner element.
    /// </summary>
    [StringLength(200)]
    public string? CssClass { get; set; }

    /// <summary>
    /// Weight for ordering within the zone (higher = shown first).
    /// </summary>
    public int Weight { get; set; } = 0;

    /// <summary>
    /// Start date for scheduling (null = immediately).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for scheduling (null = no end).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Target specific user roles (comma-separated). Empty = all users.
    /// </summary>
    [StringLength(500)]
    public string? TargetRoles { get; set; }

    /// <summary>
    /// Target specific cultures (comma-separated, e.g. "en,fr"). Empty = all.
    /// </summary>
    [StringLength(200)]
    public string? TargetCultures { get; set; }

    /// <summary>
    /// Maximum impressions per day (0 = unlimited).
    /// </summary>
    public int MaxImpressionsPerDay { get; set; } = 0;

    /// <summary>
    /// Maximum clicks per day (0 = unlimited).
    /// </summary>
    public int MaxClicksPerDay { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [BindNever]
    [ValidateNever]
    public virtual AdZone Zone { get; set; } = null!;
    [BindNever]
    [ValidateNever]
    public virtual ICollection<AdImpression> Impressions { get; set; } = new List<AdImpression>();
    [BindNever]
    [ValidateNever]
    public virtual ICollection<AdClick> Clicks { get; set; } = new List<AdClick>();

    // Computed: whether banner is currently within its schedule
    [NotMapped]
    public bool IsScheduled => (StartDate == null || StartDate <= DateTime.UtcNow) &&
                                (EndDate == null || EndDate >= DateTime.UtcNow);
}

public enum AdBannerType
{
    Image = 0,
    Html = 1,
    Script = 2,
    Text = 3
}

/// <summary>
/// Placement rules: override or exclude zones on specific pages.
/// </summary>
public class AdPlacement
{
    public int Id { get; set; }

    [Required]
    public int ZoneId { get; set; }

    /// <summary>
    /// Page slug this placement applies to. Use "*" for all pages.
    /// </summary>
    [Required, StringLength(250)]
    public string PageSlug { get; set; } = "*";

    /// <summary>
    /// If true, this zone is EXCLUDED from the specified page(s).
    /// If false, this is an override (e.g. different max banners, custom CSS).
    /// </summary>
    public bool IsExclusion { get; set; } = false;

    /// <summary>
    /// Override MaxBanners for this page (null = use zone default).
    /// </summary>
    public int? MaxBannersOverride { get; set; }

    /// <summary>
    /// Override CSS class for this page.
    /// </summary>
    [StringLength(200)]
    public string? CssClassOverride { get; set; }

    /// <summary>
    /// Override banner wrapper template for this page.
    /// </summary>
    [StringLength(500)]
    public string? WrapperTemplateOverride { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [BindNever]
    [ValidateNever]
    public virtual AdZone Zone { get; set; } = null!;
}

/// <summary>
/// Tracks an impression (view) of a banner.
/// </summary>
public class AdImpression
{
    public long Id { get; set; }

    [Required]
    public int BannerId { get; set; }

    /// <summary>
    /// The page slug where the impression occurred.
    /// </summary>
    [StringLength(250)]
    public string? PageSlug { get; set; }

    /// <summary>
    /// Zone key where the impression occurred.
    /// </summary>
    [StringLength(100)]
    public string? ZoneKey { get; set; }

    /// <summary>
    /// IP address (hashed or truncated for privacy).
    /// </summary>
    [StringLength(45)]
    public string? IpHash { get; set; }

    /// <summary>
    /// User agent (truncated).
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Culture at time of impression.
    /// </summary>
    [StringLength(10)]
    public string? Culture { get; set; }

    /// <summary>
    /// User ID if authenticated (null for anonymous).
    /// </summary>
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [BindNever]
    [ValidateNever]
    public virtual AdBanner Banner { get; set; } = null!;
}

/// <summary>
/// Tracks a click on a banner.
/// </summary>
public class AdClick
{
    public long Id { get; set; }

    [Required]
    public int BannerId { get; set; }

    /// <summary>
    /// The page slug where the click occurred.
    /// </summary>
    [StringLength(250)]
    public string? PageSlug { get; set; }

    /// <summary>
    /// Zone key where the click occurred.
    /// </summary>
    [StringLength(100)]
    public string? ZoneKey { get; set; }

    /// <summary>
    /// IP address (hashed).
    /// </summary>
    [StringLength(45)]
    public string? IpHash { get; set; }

    /// <summary>
    /// Referrer URL.
    /// </summary>
    [StringLength(500)]
    public string? Referrer { get; set; }

    /// <summary>
    /// User ID if authenticated.
    /// </summary>
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [BindNever]
    [ValidateNever]
    public virtual AdBanner Banner { get; set; } = null!;
}