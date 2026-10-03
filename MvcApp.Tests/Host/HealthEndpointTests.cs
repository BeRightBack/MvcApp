using System.Net;
using Xunit;

namespace MvcApp.Tests.Host;

/// <summary>
/// Liveness vs readiness. The finding this closes: "/health" reported Healthy (a TCP connection
/// to MySQL succeeded) while every page returned 500, because the schema was behind. Readiness now
/// reflects migration state and the seeding outcome, so a deploy gate can act on it.
/// </summary>
public class HealthEndpointTests : IClassFixture<MvcAppWebFactory>
{
    private readonly MvcAppWebFactory _factory;

    public HealthEndpointTests(MvcAppWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_is_healthy_and_does_not_touch_dependencies()
    {
        var response = await _factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task Readiness_becomes_healthy_once_seeding_completes()
    {
        var body = await HealthProbe.WaitForHealthyAsync(_factory.CreateClient(), "/health/ready");

        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Health_alias_matches_readiness()
    {
        var client = _factory.CreateClient();
        await HealthProbe.WaitForHealthyAsync(client, "/health/ready");

        var alias = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, alias.StatusCode);
        Assert.Equal("Healthy", (await alias.Content.ReadAsStringAsync()).Trim());
    }
}
