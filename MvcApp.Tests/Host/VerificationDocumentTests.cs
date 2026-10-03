using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MvcApp.Tests.Host;

/// <summary>
/// Guards audit 2.3: submitted identity documents must not be reachable without authorization.
/// They used to live under wwwroot, where the static-file middleware served them to anyone with
/// the URL — no login, no role, nothing.
/// </summary>
public class VerificationDocumentTests : IClassFixture<MvcAppWebFactory>
{
    private readonly MvcAppWebFactory _factory;

    public VerificationDocumentTests(MvcAppWebFactory factory) => _factory = factory;

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Documents_are_not_reachable_as_static_files()
    {
        // The old shape of the URL. Nothing is served from /Verifications any more, so this must
        // 404 rather than hand back a photo.
        var response = await Client().GetAsync("/Verifications/someuser/verify_something.jpg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Document_endpoint_requires_authentication()
    {
        var response = await Client().GetAsync("/Admin/VerificationModeration/Document?id=1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }
}
