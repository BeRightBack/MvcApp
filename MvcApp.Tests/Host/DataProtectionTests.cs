using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MvcApp.Tests.Host;

/// <summary>
/// Guards the deploy-stability of the Data Protection ring. Without an explicit persistent
/// location, keys default to a per-user ring that is not deploy-stable — so every restart or
/// additional instance invalidates auth cookies, session state and antiforgery tokens.
/// </summary>
public class DataProtectionTests : IClassFixture<MvcAppWebFactory>
{
    private readonly MvcAppWebFactory _factory;

    public DataProtectionTests(MvcAppWebFactory factory) => _factory = factory;

    [Fact]
    public void Key_ring_is_persisted_to_the_configured_location()
    {
        var protector = _factory.Services
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("persistence-test");

        // Materialise a key so the ring is actually written.
        var ciphertext = protector.Protect("value");
        Assert.Equal("value", protector.Unprotect(ciphertext));

        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MvcApp",
            "keys");

        Assert.True(Directory.Exists(expected), $"Data Protection key directory was not created: {expected}");
        Assert.NotEmpty(Directory.GetFiles(expected, "*.xml"));
    }
}
