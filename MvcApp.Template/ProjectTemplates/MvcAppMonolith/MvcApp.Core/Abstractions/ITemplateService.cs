namespace MvcApp.Core.Abstractions;

public interface ITemplateService
{
    Task<string> GetActiveTemplateAsync();
    Task<List<UiTemplateInfo>> GetAvailableTemplatesAsync();
    Task SetActiveTemplateAsync(string name, string? updatedBy = null);
    Task<bool> IsLandingPageEnabledAsync(string template);
    Task SetLandingPageEnabledAsync(string template, bool enabled, string? updatedBy = null);
}

public class UiTemplateInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CssPath { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
}
