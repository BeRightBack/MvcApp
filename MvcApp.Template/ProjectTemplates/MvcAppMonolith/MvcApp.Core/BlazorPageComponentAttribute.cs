namespace MvcApp.Core;

/// <summary>
/// Marks a Razor/Blazor component as a reusable page widget. Components marked
/// with this attribute are registered in the Pages module's Blazor component
/// catalog and become insertable into page bodies, e.g. <c>@Blazor("Banner")</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BlazorPageComponentAttribute : Attribute
{
    public BlazorPageComponentAttribute(string name) => Name = name;

    /// <summary>Unique key used in page markup: <c>@Blazor("Name")</c>.</summary>
    public string Name { get; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string Category { get; set; } = "Blazor";
}
