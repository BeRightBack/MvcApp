using Microsoft.Extensions.Configuration;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services;

public class BrandingService(ISettingsService settings, IConfiguration configuration) : IBrandingService
{
    private const string DefaultSiteName = "MvcApp.Web";

    public async Task<string> GetSiteNameAsync()
    {
        return await settings.GetAsync("Branding.SiteName")
               ?? configuration["Branding:SiteName"]
               ?? DefaultSiteName;
    }

    public async Task<string?> GetLogoUrlAsync()
    {
        var url = await settings.GetAsync("Branding.LogoUrl");
        if (string.IsNullOrWhiteSpace(url))
            url = configuration["Branding:LogoUrl"];
        return string.IsNullOrWhiteSpace(url) ? null : url;
    }

    public async Task<string?> GetTaglineAsync()
    {
        var tagline = await settings.GetAsync("Branding.Tagline");
        if (string.IsNullOrWhiteSpace(tagline))
            tagline = configuration["Branding:Tagline"];
        return string.IsNullOrWhiteSpace(tagline) ? null : tagline;
    }
}
