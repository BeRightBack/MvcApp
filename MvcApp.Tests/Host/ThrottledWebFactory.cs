using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MvcApp.Tests.Host;

/// <summary>
/// Same host as <see cref="MvcAppWebFactory"/> but with a deliberately tiny rate limit, so the
/// limiter can be asserted to actually reject without hammering the app hundreds of times.
/// </summary>
public sealed class ThrottledWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "5",
                ["RateLimiting:WindowSeconds"] = "60"
            });
        });
    }
}
