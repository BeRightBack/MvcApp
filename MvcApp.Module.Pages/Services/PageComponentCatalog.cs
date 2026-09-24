namespace MvcApp.Module.Pages.Services;

/// <summary>
/// Catalog of the built-in component helpers exposed on
/// <see cref="ContentPageTemplateBase"/> for the editor's component picker.
/// </summary>
public record PageComponentInfo(string Name, string Syntax, string Description);

public static class PageComponentCatalog
{
    public static readonly List<PageComponentInfo> Items =
    [
        new("Button", "Button(\"Label\", \"/url\", \"primary\", \"lg\")", "Styled link button. Style: primary/secondary/success/danger/light/dark/outline-*."),
        new("Badge", "Badge(\"New\", \"success\")", "Small label badge. Style: primary/secondary/success/danger/warning/info/light/dark."),
        new("Alert", "Alert(\"Message\", \"info\", true)", "Dismissible alert box. Style: info/success/warning/danger."),
        new("Card", "Card(\"body html\", \"Title\", \"/image.png\", \"footer\")", "Bootstrap card with optional title, image and footer. Body may be HTML."),
        new("Callout", "Callout(\"Title\", \"text\", \"primary\")", "Highlighted callout with left border. Style: primary/success/warning/danger."),
        new("Video", "Video(\"https://www.youtube.com/watch?v=...\")", "Responsive 16:9 embed for YouTube, youtu.be or Vimeo links."),
        new("Image", "Image(\"/img.png\", \"alt text\", \"img-fluid rounded mb-3\")", "Image tag with CSS classes."),
        new("Icon", "Icon(\"check-circle\")", "Boxicons icon, e.g. check-circle, bxs-star, bxs-rocket."),
        new("Accordion", "Accordion(\"Question?\", \"answer html\")", "Collapsible accordion item with heading and body."),
        new("Quote", "Quote(\"quote text\", \"Author, Company\")", "Centered blockquote with optional attribution."),
        new("Divider", "Divider()", "Horizontal rule with spacing."),
        new("Spacer", "Spacer(4)", "Vertical spacer, size 1-5."),
        new("Row", "Row(\"...cols...\")", "Bootstrap row (grid) wrapper around columns."),
        new("Col", "Col(\"...content...\", 6)", "Bootstrap column; width 1-12."),
        new("List", "List(\"item one\", \"item two\")", "Unordered list of items."),
        new("Stats", "Stats(1200, \"Members\", \"bxs-user\")", "Single statistic cell for a stats row (wrap in Row/Col).")
    ];
}
