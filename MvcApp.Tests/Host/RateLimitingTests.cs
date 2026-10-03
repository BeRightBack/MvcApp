using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MvcApp.Tests.Host;

/// <summary>
/// Proves the rate limiter is actually in effect. The previous state was a named policy that no
/// endpoint referenced: it looked like protection in code review and throttled nothing at runtime.
/// </summary>
public class RateLimitingTests : IClassFixture<ThrottledWebFactory>
{
    private readonly ThrottledWebFactory _factory;

    public RateLimitingTests(ThrottledWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Dynamic_requests_are_rejected_once_the_limit_is_exceeded()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 8; i++)
        {
            statuses.Add((await client.GetAsync("/Admin")).StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}
