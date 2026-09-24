using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Areas.Admin.Models;

public class TemplateNavViewModel
{
    public string Template { get; set; } = string.Empty;
    public string TemplateDisplayName { get; set; } = string.Empty;
    public List<UiTemplateInfo> Templates { get; set; } = new();
    public List<TemplateNavEditorItem> NavbarItems { get; set; } = new();
    public List<TemplateNavEditorItem> FooterItems { get; set; } = new();
    public List<TemplateNavEditorItem> SocialDropdownItems { get; set; } = new();
    public List<TemplateNavEditorItem> ProfileDropdownItems { get; set; } = new();
    public string SocialDropdownIcon { get; set; } = "bx bx-heart";
}

public class TemplateNavEditorItem
{
    public int Index { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Module { get; set; }
    public bool RequiresAuth { get; set; }
    public bool RequiresAdmin { get; set; }
    public bool RequiresModerator { get; set; }
    public bool IsChecked { get; set; }
    public bool IsAdditional { get; set; }
    public bool? ModuleEnabled { get; set; }
}
