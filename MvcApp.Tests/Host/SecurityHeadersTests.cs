using Xunit;

namespace MvcApp.Tests.Host;

/// <summary>
/// Guards the baseline security headers added by SecurityHeadersMiddleware. Without these,
/// the headers are something each page has to remember — which is how they went missing in
/// the first place.
/// </summary>
public class SecurityHeadersTests : IClassFixture<MvcAppWebFactory>
{
    private readonly MvcAppWebFactory _factory;

    public SecurityHeadersTests(MvcAppWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Baseline_security_headers_are_present()
    {
        var response = await _factory.CreateClient().GetAsync("/");

        Assert.True(
            response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions),
            "X-Content-Type-Options header is missing");
        Assert.Equal("nosniff", contentTypeOptions.Single());

        Assert.True(
            response.Headers.TryGetValues("X-Frame-Options", out var frameOptions),
            "X-Frame-Options header is missing");
        Assert.Equal("SAMEORIGIN", frameOptions.Single());

        Assert.True(
            response.Headers.TryGetValues("Referrer-Policy", out var referrerPolicy),
            "Referrer-Policy header is missing");
        Assert.Equal("strict-origin-when-cross-origin", referrerPolicy.Single());
    }

    [Fact]
    public async Task Headers_are_present_on_redirect_responses_too()
    {
        var response = await _factory
            .CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            })
            .GetAsync("/Admin");

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var values));
        Assert.Equal("nosniff", values.Single());
    }

    [Fact]
    public async Task Server_banner_is_not_advertised()
    {
        var response = await _factory.CreateClient().GetAsync("/");

        Assert.False(response.Headers.Contains("Server"), "the Server banner should not be emitted");
    }
}
