using MvcApp.Services;
using Xunit;

namespace MvcApp.Tests;

public class EmailTemplatesTests
{
    [Fact]
    public void Build_ProducesHtmlShellContainingHeadingMessageAndCta()
    {
        var html = EmailTemplates.Build("TestSite", "Welcome", "Hello world", null, null, "Welcome aboard", "Get started", "https://example.com/start");

        Assert.Contains("<html", html);
        Assert.Contains("Welcome", html);
        Assert.Contains("Hello world", html);
        Assert.Contains("Welcome aboard", html);
        Assert.Contains("Get started", html);
        Assert.Contains("https://example.com/start", html);
    }

    [Fact]
    public void Build_WithoutCta_OmitsCtaBlock()
    {
        var html = EmailTemplates.Build("TestSite", "Subject", "Body");

        Assert.Contains("<html", html);
        Assert.DoesNotContain("display:inline-block;padding:12px 28px", html);
    }

    [Fact]
    public void ToHtml_EncodesPlainText()
    {
        var html = EmailTemplates.ToHtml("1 < 2 & 3 > 0");

        Assert.Contains("&lt;", html);
        Assert.Contains("&amp;", html);
    }

    [Fact]
    public void ToHtml_PassesThroughHtml()
    {
        const string html = "<p>already html</p>";

        Assert.Equal(html, EmailTemplates.ToHtml(html));
    }
}
