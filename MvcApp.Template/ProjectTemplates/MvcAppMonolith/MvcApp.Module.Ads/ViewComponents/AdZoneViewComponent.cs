using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using MvcApp.Core.Abstractions;
using MvcApp.Module.Ads.Services;

namespace MvcApp.Module.Ads.ViewComponents;

/// <summary>
/// Renders an ad zone by key for the current request. Returns empty output
/// when the Ads module is disabled, so layouts can include it unconditionally.
/// </summary>
public class AdZoneViewComponent : ViewComponent
{
    private readonly IAdRenderer _renderer;
    private readonly IModuleManager _moduleManager;

    public AdZoneViewComponent(IAdRenderer renderer, IModuleManager moduleManager)
    {
        _renderer = renderer;
        _moduleManager = moduleManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(string zoneKey)
    {
        if (!await _moduleManager.IsModuleEnabledAsync("Ads"))
            return new HtmlContentViewComponentResult(HtmlString.Empty);

        var http = ViewContext.HttpContext;
        var path = http.Request.Path.Value ?? "/";
        var pageSlug = path == "/" ? "home" : path.Trim('/');
        var isLandingPage = path == "/";
        var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;

        var html = await _renderer.RenderZoneAsync(zoneKey, pageSlug, isLandingPage, http.User, culture, http);
        return new HtmlContentViewComponentResult(new HtmlString(html));
    }
}
