using Ganss.Xss;

namespace MvcApp.Common.Html;

/// <summary>Sanitises user-authored rich text before it is stored or rendered.</summary>
public interface IHtmlContentSanitizer
{
    string Sanitize(string? html);
}

/// <summary>
/// Allow-list sanitiser for user-authored HTML (forum posts today).
///
/// Why: the forum renders post bodies with <c>@Html.Raw</c> (Thread.cshtml) and the write paths
/// store whatever the browser POSTs, so a member can store scripts that then execute in every
/// reader's session — stored XSS (audit 2.1). Posts are meant to be rich text and the editor is a
/// WYSIWYG, so the answer is not to HTML-encode the body but to allow only the markup that editor
/// can actually produce.
///
/// The allow-list below mirrors the toolbar in Views/Shared/_RichTextEditor.cshtml: bold, italic,
/// underline, strikethrough, h1-h3, paragraph, bullet/numbered lists, blockquote, code block,
/// horizontal rule, links and images. Everything else — script, style, iframe, object, event
/// handlers, <c>style</c> attributes, data: URIs — is dropped.
/// </summary>
public sealed class HtmlContentSanitizer : IHtmlContentSanitizer
{
    private static readonly string[] AllowedTagNames =
    [
        "p", "br", "div", "span",
        "b", "strong", "i", "em", "u", "s", "strike", "del",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li",
        "blockquote", "pre", "code",
        "hr", "a", "img"
    ];

    private static readonly string[] AllowedAttributeNames =
    [
        "href", "src", "alt", "title"
    ];

    private static readonly string[] AllowedSchemeNames =
    [
        "http", "https", "mailto"
    ];

    private readonly HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            // Replaced, not added to: the library defaults include more than this editor can produce.
            AllowedTags = new HashSet<string>(AllowedTagNames, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(AllowedAttributeNames, StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(AllowedSchemeNames, StringComparer.OrdinalIgnoreCase),

            // Which attributes are URLs at all. This is what makes AllowedSchemes apply: with it
            // empty (the default when you supply your own options object) the scheme check never
            // runs and javascript:/data: values sail straight through — verified by probe.
            UriAttributes = new HashSet<string>(
                ["href", "src", "cite", "action", "formaction", "poster", "background", "longdesc", "usemap", "data"],
                StringComparer.OrdinalIgnoreCase),

            // No CSS at all: the toolbar cannot emit style attributes, so none are needed, and
            // dropping them removes a whole class of CSS-based mischief.
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedCssClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedAtRules = new HashSet<AngleSharp.Css.Dom.CssRuleType>()
        };

        _sanitizer = new HtmlSanitizer(options);
    }

    public string Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);
}
