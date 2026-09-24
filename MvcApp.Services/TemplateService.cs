using MvcApp.Core.Abstractions;

namespace MvcApp.Services;

public class TemplateService : ITemplateService
{
    private readonly ISettingsService _settings;
    private static readonly List<UiTemplateInfo> _templates =
    [
        new()
        {
            Name = "Default",
            DisplayName = "Default",
            Description = "Clean and modern default template",
            CssPath = "~/css/templates/default.css",
            Thumbnail = "/images/templates/default-thumb.png"
        },
        new()
        {
            Name = "Dating",
            DisplayName = "Dating",
            Description = "Warm and romantic design for dating and social platforms",
            CssPath = "~/css/templates/dating.css",
            Thumbnail = "/images/templates/dating-thumb.png"
        },
        new()
        {
            Name = "Store",
            DisplayName = "Online Store",
            Description = "Commerce-focused design with product cards and prominent CTAs",
            CssPath = "~/css/templates/store.css",
            Thumbnail = "/images/templates/store-thumb.png"
        },
        new()
        {
            Name = "Business",
            DisplayName = "Business CMS",
            Description = "Professional corporate look for CMS portals and dashboards",
            CssPath = "~/css/templates/business.css",
            Thumbnail = "/images/templates/business-thumb.png"
        },
        new()
        {
            Name = "Magazine",
            DisplayName = "Magazine",
            Description = "Bold editorial style with high-impact typography for content sites",
            CssPath = "~/css/templates/magazine.css",
            Thumbnail = "/images/templates/magazine-thumb.png"
        },
        new()
        {
            Name = "Professional",
            DisplayName = "Professional",
            Description = "Clean corporate look for business and professional networking",
            CssPath = "~/css/templates/professional.css",
            Thumbnail = "/images/templates/professional-thumb.png"
        },
        new()
        {
            Name = "Gaming",
            DisplayName = "Gaming",
            Description = "Dark neon style for gaming communities and esports",
            CssPath = "~/css/templates/gaming.css",
            Thumbnail = "/images/templates/gaming-thumb.png"
        },
        new()
        {
            Name = "Minimal",
            DisplayName = "Minimal",
            Description = "Ultra-light, maximal whitespace, content-first design",
            CssPath = "~/css/templates/minimal.css",
            Thumbnail = "/images/templates/minimal-thumb.png"
        },
        new()
        {
            Name = "Luxury",
            DisplayName = "Luxury",
            Description = "Dark gold and black premium feel for exclusive brands",
            CssPath = "~/css/templates/luxury.css",
            Thumbnail = "/images/templates/luxury-thumb.png"
        },
        new()
        {
            Name = "Social",
            DisplayName = "Social",
            Description = "Vibrant blue and white style for social media platforms",
            CssPath = "~/css/templates/social.css",
            Thumbnail = "/images/templates/social-thumb.png"
        },
        new()
        {
            Name = "IPTV",
            DisplayName = "IPTV",
            Description = "Faithful Quebec IPTV landing with hero, live TV and streaming plans",
            CssPath = "~/css/templates/iptv.css",
            Thumbnail = "/images/templates/iptv-thumb.png"
        }
    ];

    public TemplateService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<string> GetActiveTemplateAsync()
    {
        return await _settings.GetAsync("SiteTemplate") ?? "Default";
    }

    public Task<List<UiTemplateInfo>> GetAvailableTemplatesAsync()
    {
        return Task.FromResult(_templates);
    }

    public async Task SetActiveTemplateAsync(string name, string? updatedBy = null)
    {
        var template = _templates.FirstOrDefault(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (template == null)
            throw new ArgumentException($"Template '{name}' not found.");

        await _settings.SetAsync("SiteTemplate", template.Name, updatedBy);
    }

    public async Task<bool> IsLandingPageEnabledAsync(string template)
    {
        var enabled = await _settings.GetAsync<bool>($"Template.{template}.LandingEnabled");
        return enabled ?? true;
    }

    public async Task SetLandingPageEnabledAsync(string template, bool enabled, string? updatedBy = null)
    {
        await _settings.SetAsync($"Template.{template}.LandingEnabled", enabled.ToString().ToLower(), updatedBy);
    }
}
