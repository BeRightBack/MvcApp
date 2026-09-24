using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MvcApp.Localization;

namespace MvcApp.Module.Pages.Controllers;

/// <summary>
/// Base controller for the Pages module. Wires the system's DB-backed,
/// auto-translating localization (see <c>MvcApp.Web.Controllers.BaseController</c>)
/// so module controllers and views get the same <c>Localize</c>/<c>LocString</c>
/// helpers as the rest of the app. Derived controllers inject their own
/// repositories/services via the constructor.
/// </summary>
public abstract class PagesBaseController : Controller
{
    protected readonly IStringLocalizer<SharedResource> Localizer;

    protected PagesBaseController(IStringLocalizer<SharedResource> localizer)
    {
        Localizer = localizer;
    }

    public HtmlString Localize(string resourceKey, params object[] args)
    {
        var value = Localizer[resourceKey].Value;
        return new HtmlString(args.Length == 0 ? value : string.Format(value, args));
    }

    public string LocString(string resourceKey, params object[] args)
        => Localize(resourceKey, args).ToString();
}
