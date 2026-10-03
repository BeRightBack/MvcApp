using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MvcApp.Tests.Host;

/// <summary>
/// First host-level assertions. These encode behaviour the app must keep as the structural
/// hardening lands (auth defaults, anti-forgery, headers, middleware order) — they are the
/// regression net for those changes, replacing "we read the code and it looked right".
/// </summary>
public class HostSmokeTests : IClassFixture<MvcAppWebFactory>
{
    private readonly MvcAppWebFactory _factory;

    public HostSmokeTests(MvcAppWebFactory factory) => _factory = factory;

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Admin_area_requires_authentication()
    {
        var response = await Client().GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Home_page_renders()
    {
        var response = await Client().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_route_returns_404_not_500()
    {
        var response = await Client().GetAsync("/definitely-not-a-route-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        // Readiness now gates on seeding, which runs off the startup path — so poll.
        var body = await HealthProbe.WaitForHealthyAsync(Client(), "/health");

        Assert.Equal("Healthy", body);
    }
}
