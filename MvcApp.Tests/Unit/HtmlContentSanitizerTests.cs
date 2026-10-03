using MvcApp.Common.Html;
using Xunit;

namespace MvcApp.Tests.Unit;

/// <summary>
/// Guards audit 2.1: forum post bodies are rich text, stored as-is and rendered with Html.Raw, so
/// anything a member can get into a body executes in every reader's browser. The sanitiser must
/// drop script-bearing markup while leaving the formatting the WYSIWYG editor can produce intact.
/// </summary>
public class HtmlContentSanitizerTests
{
    private static readonly HtmlContentSanitizer Sanitizer = new();

    [Fact]
    public void Script_tags_and_their_content_are_removed()
    {
        var result = Sanitizer.Sanitize("<p>hi</p><script>alert('xss')</script>");

        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hi", result);
    }

    [Fact]
    public void Event_handler_attributes_are_removed()
    {
        var result = Sanitizer.Sanitize("<img src=\"/uploads/a.png\" onerror=\"alert(1)\">");

        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Javascript_urls_are_stripped_from_links()
    {
        var result = Sanitizer.Sanitize("<a href=\"javascript:alert(1)\">click</a>");

        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("click", result);
    }

    [Fact]
    public void Javascript_urls_are_stripped_from_images()
    {
        var result = Sanitizer.Sanitize("<img src=\"javascript:alert(1)\">");

        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Iframes_and_style_attributes_are_removed()
    {
        var result = Sanitizer.Sanitize("<iframe src=\"https://evil.test\"></iframe><div style=\"position:fixed\">x</div>");

        Assert.DoesNotContain("<iframe", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Data_uris_are_removed()
    {
        var result = Sanitizer.Sanitize("<img src=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\">");

        Assert.DoesNotContain("data:", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_editors_own_formatting_survives()
    {
        // Everything the toolbar can produce must still come out the other side, or members lose
        // their formatting for no security gain.
        const string post =
            "<h2>Title</h2><p>Some <b>bold</b>, <i>italic</i>, <u>underline</u> and <s>struck</s> text.</p>" +
            "<ul><li>one</li><li>two</li></ul>" +
            "<blockquote>quoted</blockquote><pre><code>var x = 1;</code></pre><hr>" +
            "<a href=\"https://example.com\">a link</a><img src=\"/uploads/pic.png\" alt=\"pic\">";

        var result = Sanitizer.Sanitize(post);

        Assert.Contains("<h2>Title</h2>", result);
        Assert.Contains("<b>bold</b>", result);
        Assert.Contains("<i>italic</i>", result);
        Assert.Contains("<u>underline</u>", result);
        Assert.Contains("<s>struck</s>", result);
        Assert.Contains("<li>one</li>", result);
        Assert.Contains("<blockquote>quoted</blockquote>", result);
        Assert.Contains("var x = 1;", result);
        Assert.Contains("<hr", result);
        Assert.Contains("href=\"https://example.com\"", result);
        Assert.Contains("src=\"/uploads/pic.png\"", result);
    }

    [Fact]
    public void Empty_and_null_input_are_handled()
    {
        Assert.Equal(string.Empty, Sanitizer.Sanitize(null));
        Assert.Equal(string.Empty, Sanitizer.Sanitize("   "));
    }
}
