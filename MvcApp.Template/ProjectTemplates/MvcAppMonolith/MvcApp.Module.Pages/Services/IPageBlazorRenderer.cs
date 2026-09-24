namespace MvcApp.Module.Pages.Services;

/// <summary>
/// Server-prerenders a registered Blazor page widget to HTML so it can be
/// embedded in a Razor-rendered page body.
/// </summary>
public interface IPageBlazorRenderer
{
    Task<string> RenderAsync(string name, string? parametersJson = null);

    Task<string> RenderAsync(Type componentType, string? parametersJson = null);
}
