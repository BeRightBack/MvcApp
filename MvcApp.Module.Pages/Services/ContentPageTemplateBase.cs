using System.Security.Claims;
using System.Text;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using RazorEngineCore;
using MvcApp.Core;
using MvcApp.Localization;
using MvcApp.Module.Ads.Services;

namespace MvcApp.Module.Pages.Services;

/// <summary>
/// Base template for DB-stored page bodies. Exposes the page as a strongly
/// typed <see cref="Model"/> plus reusable component helpers callable from
/// the page's Razor markup, e.g. <c>@Button("Visit", "https://example.com")</c>.
/// All helpers return raw HTML that reuses the site's Bootstrap/Boxicons assets.
/// The renderer also injects <see cref="Services"/> (the current request scope)
/// and <see cref="User"/> (the current principal), so page bodies can localize
/// text (<c>@Localize(...)</c>), branch on identity (<c>@User</c>), and pull
/// system content/services (<c>@Service&lt;T&gt;()</c>).
/// </summary>
public class ContentPageTemplateBase : RazorEngineTemplateBase
{
    public new ContentPage Model { get; set; } = new();

    /// <summary>
    /// The request scope the page is being rendered in. Lets page bodies
    /// resolve scoped system services, e.g.
    /// <c>@Service&lt;IRepository&lt;ContentPageSnippet&gt;&gt;()</c>.
    /// </summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// The current request principal, so page bodies can react to the signed-in
    /// user, e.g. <c>@if (IsInRole("Admin")) { ... }</c>.
    /// </summary>
    public ClaimsPrincipal? User { get; set; }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;

    public string? CurrentUserName => User?.Identity?.Name;

    /// <summary>
    /// Localizes a string using the system's DB-backed, auto-translating
    /// localizer. Falls back to the resource key when no localizer is available.
    /// </summary>
    public string Localize(string resourceKey, params object[] args)
    {
        if (Services is null) return resourceKey;
        var localizer = Services.GetService<IStringLocalizer<SharedResource>>();
        if (localizer is null) return resourceKey;
        var value = localizer[resourceKey].Value;
        return args.Length == 0 ? value : string.Format(value, args);
    }

    /// <summary>
    /// Alias for <see cref="Localize"/> using the <c>Localizer</c> name that
    /// standard Razor views use for the <c>IStringLocalizer</c> property. In the
    /// template DSL it is callable like a method: <c>@Localizer("Home.Welcome")</c>.
    /// </summary>
    public string Localizer(string resourceKey, params object[] args) => Localize(resourceKey, args);

    /// <summary>
    /// Alias for <see cref="Localize"/> (<c>LocString</c>/<c>Locstring</c>).
    /// </summary>
    public string LocString(string resourceKey, params object[] args) => Localize(resourceKey, args);

    /// <summary>
    /// Case-insensitive alias for <see cref="LocString"/> so page bodies written
    /// with either casing compile.
    /// </summary>
    public string Locstring(string resourceKey, params object[] args) => Localize(resourceKey, args);

    /// <summary>
    /// Resolves a service from the current request scope (or <c>null</c> if it
    /// is not registered), letting page bodies read system content.
    /// </summary>
    public T? Service<T>() where T : class
        => Services?.GetService<T>();

    public string Button(string label, string href = "#", string? style = "primary", string? size = "", string? extra = "")
    {
        var cls = "btn btn-" + E(style ?? "primary");
        if (!string.IsNullOrWhiteSpace(size)) cls += " btn-" + E(size);
        if (!string.IsNullOrWhiteSpace(extra)) cls += " " + E(extra);
        return $"<a class=\"{cls}\" href=\"{E(href)}\">{E(label)}</a>";
    }

    public string Badge(string text, string? style = "secondary")
        => $"<span class=\"badge bg-{E(style ?? "secondary")}\">{E(text)}</span>";

    public string Alert(string message, string? style = "info", bool dismissible = true)
    {
        var cls = "alert alert-" + E(style ?? "info") + (dismissible ? " alert-dismissible fade show" : "");
        var close = dismissible ? " <button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"alert\"></button>" : "";
        return $"<div class=\"{cls}\" role=\"alert\">{RawBody(message)}{close}</div>";
    }

    public string Card(string body, string? title = "", string? image = "", string? footer = "")
    {
        var sb = new StringBuilder("<div class=\"card mb-3\">");
        if (!string.IsNullOrWhiteSpace(image))
            sb.Append($"<img src=\"{E(image)}\" class=\"card-img-top\" alt=\"{E(title)}\" />");
        sb.Append("<div class=\"card-body\">");
        if (!string.IsNullOrWhiteSpace(title))
            sb.Append($"<h5 class=\"card-title\">{E(title)}</h5>");
        sb.Append($"<div class=\"card-text\">{RawBody(body)}</div>");
        sb.Append("</div>");
        if (!string.IsNullOrWhiteSpace(footer))
            sb.Append($"<div class=\"card-footer text-muted\">{RawBody(footer)}</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    public string Callout(string title, string body, string? style = "primary")
    {
        var tone = (style ?? "primary") switch
        {
            "danger" => "text-danger border-danger",
            "warning" => "text-warning border-warning",
            "success" => "text-success border-success",
            _ => "text-primary border-primary"
        };
        return $"<div class=\"border-start border-4 {tone} ps-3 mb-3\"><h5 class=\"mb-1\">{E(title)}</h5><p class=\"mb-0\">{RawBody(body)}</p></div>";
    }

    public string Video(string url)
    {
        var src = E(VideoEmbedUrl(url));
        return $"<div class=\"ratio ratio-16x9 mb-3\"><iframe src=\"{src}\" title=\"video\" allowfullscreen></iframe></div>";
    }

    public string Image(string src, string? alt = "", string? cssClass = "img-fluid rounded mb-3")
    {
        var cls = string.IsNullOrWhiteSpace(cssClass) ? "" : $" class=\"{E(cssClass)}\"";
        return $"<img src=\"{E(src)}\" alt=\"{E(alt ?? "")}\"{cls} />";
    }

    public string Icon(string name, string? extra = "")
    {
        var cls = "bx bx-" + E(name);
        if (!string.IsNullOrWhiteSpace(extra)) cls += " " + E(extra);
        return $"<i class=\"{cls}\"></i>";
    }

    public string Accordion(string title, string body, string? id = "accordionItem")
    {
        var safeId = E(id ?? "accordionItem");
        var collapseId = safeId + "Body";
        return $"<div class=\"accordion mb-3\" id=\"{safeId}\"><div class=\"accordion-item\">"
             + $"<h2 class=\"accordion-header\"><button class=\"accordion-button\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#{collapseId}\">{E(title)}</button></h2>"
             + $"<div id=\"{collapseId}\" class=\"accordion-collapse collapse show\" data-bs-parent=\"#{safeId}\"><div class=\"accordion-body\">{RawBody(body)}</div></div></div></div>";
    }

    public string Quote(string text, string? attribution = "")
    {
        var footer = string.IsNullOrWhiteSpace(attribution)
            ? ""
            : $"<footer class=\"blockquote-footer\">{E(attribution)}</footer>";
        return $"<figure class=\"text-center my-4\"><blockquote class=\"blockquote\"><p>{E(text)}</p></blockquote>{footer}</figure>";
    }

    public string Divider()
        => "<hr class=\"my-4\" />";

    public string Spacer(int size = 4)
        => $"<div class=\"py-{E(size.ToString())}\"></div>";

    public string Stats(int value, string label, string? icon = "")
    {
        var i = string.IsNullOrWhiteSpace(icon) ? "" : $"<i class=\"bx bx-{E(icon)} text-primary fs-3\"></i>";
        return $"<div class=\"col-md-3 text-center mb-3\">{i}<div class=\"display-5 fw-bold\">{value}</div><p class=\"text-muted mb-0\">{E(label)}</p></div>";
    }

    public string Row(string innerHtml)
        => $"<div class=\"row g-4\">{RawBody(innerHtml)}</div>";

    public string Col(string innerHtml, int md = 6)
        => $"<div class=\"col-md-{E(md.ToString())}\">{RawBody(innerHtml)}</div>";

    public string List(params string[] items)
    {
        var sb = new StringBuilder("<ul>");
        foreach (var it in items)
            sb.Append($"<li>{RawBody(it)}</li>");
        sb.Append("</ul>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders the value as raw HTML (unencoded) so markup in content is preserved.
    /// </summary>
    public string RawBody(string content)
        => content;

    /// <summary>
    /// Embeds a registered Blazor page widget by name. Returns a placeholder token
    /// that <see cref="PageRenderer"/> replaces with the server-prerendered widget
    /// HTML after the template runs. Pass JSON parameters as the second argument,
    /// e.g. <c>@Blazor("Banner", "{\"Title\":\"Hello\"}")</c>.
    /// </summary>
    public string Blazor(string name, string? parametersJson = null)
    {
        var json = string.IsNullOrWhiteSpace(parametersJson) ? string.Empty : parametersJson;
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return $"__PAGEBLAZOR__:{name}:{b64}__";
    }

    /// <summary>
    /// Renders an ad zone by its key (e.g., "top-header", "sidebar-right").
    /// Returns HTML for the zone with all active, targeted banners.
    /// </summary>
    public string AdZone(string zoneKey)
    {
        if (Services is null) return $"<!-- AdZone '{zoneKey}': no Services -->";
        var renderer = Services.GetService<IAdRenderer>();
        if (renderer is null) return $"<!-- AdZone '{zoneKey}': IAdRenderer not registered -->";

        // We need the current page slug, culture, and landing page flag from context
        // These are typically available via the Model (ContentPage) and request context
        var pageSlug = Model?.Slug ?? "";
        var isLandingPage = string.IsNullOrEmpty(pageSlug) || pageSlug == "home" || pageSlug == "index";
        var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;

        // Use sync-over-async since this runs in a template
        var task = renderer.RenderZoneAsync(zoneKey, pageSlug, isLandingPage, User, culture);
        return task.GetAwaiter().GetResult();
    }

    private static string VideoEmbedUrl(string url)
    {
        if (url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase))
        {
            var idx = url.IndexOf("v=", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var id = url[(idx + 2)..];
                var amp = id.IndexOf('&');
                if (amp >= 0) id = id[..amp];
                return $"https://www.youtube.com/embed/{id}";
            }
        }
        else if (url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
        {
            var id = url[(url.IndexOf("youtu.be/", StringComparison.OrdinalIgnoreCase) + 9)..];
            var q = id.IndexOf('?');
            if (q >= 0) id = id[..q];
            return $"https://www.youtube.com/embed/{id}";
        }
        else if (url.Contains("vimeo.com/", StringComparison.OrdinalIgnoreCase))
        {
            var id = url[(url.IndexOf("vimeo.com/", StringComparison.OrdinalIgnoreCase) + 10)..];
            var q = id.IndexOf('?');
            if (q >= 0) id = id[..q];
            return $"https://player.vimeo.com/video/{id}";
        }
        return url;
    }

    private static string E(string? value)
        => HttpUtility.HtmlEncode(value ?? string.Empty);
}
