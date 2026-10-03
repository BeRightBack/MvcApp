using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MvcApp.Tests.Host;

/// <summary>
/// Boots the real application host in-process (TestServer — no Kestrel, no port binding) so that
/// request-level behaviour can be asserted: routing, authorization, status codes, response headers
/// and middleware order. This is the harness the structural hardening is verified against; the
/// content root resolves to MvcApp.Web through the Mvc.Testing manifest.
///
/// The host boots with the application's real configuration, so it needs the database that
/// appsettings.json points at to be reachable (the app will not start without it). Tests in this
/// namespace are therefore environment-dependent by design — a readiness check is exactly what we
/// want them to fail on.
/// </summary>
public sealed class MvcAppWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production so the pipeline matches deployment (exception handler instead of the
        // developer exception page), which is what the assertions care about.
        builder.UseEnvironment("Production");
    }
}
