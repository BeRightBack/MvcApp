namespace MvcApp.Core;

public enum NavItemType
{
    Link,
    Header,
    Divider
}

public class NavItem
{
    public string Label { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Module { get; set; }
    public bool RequiresAuth { get; set; }
    public bool RequiresAdmin { get; set; }
    public bool RequiresModerator { get; set; }
    public NavItemType Type { get; set; } = NavItemType.Link;
    public string? Icon { get; set; }
}
