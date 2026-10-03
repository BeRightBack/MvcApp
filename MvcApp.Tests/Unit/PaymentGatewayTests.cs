using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Common.Payments;
using Xunit;

namespace MvcApp.Tests.Unit;

/// <summary>
/// The payment gate exists so a template owner can enable whichever rails they want without the
/// checkout code knowing which one it is. Two properties matter enough to pin down: an offline rail
/// must never report a payment completed on its own authority (only a human can see money arrive),
/// and asking for a rail that is not enabled must fail loudly rather than quietly take the money on
/// a different one.
/// </summary>
public class PaymentGatewayTests
{
    private static ServiceProvider Build(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        PaymentGatewayServiceCollectionExtensions.AddPaymentGateways(services, configuration);

        return services.BuildServiceProvider();
    }

    private static readonly PaymentRequest SampleRequest =
        new(Amount: 49.99m, Currency: "CAD", Reference: "42", Description: "VIP plan");

    [Fact]
    public async Task An_offline_rail_records_what_is_owed_and_waits_for_a_human()
    {
        using var provider = Build(
            ("Payments:Manual:Kind", "Interac"),
            ("Payments:Manual:Recipient", "pay@example.com"),
            ("Payments:Manual:ReferencePrefix", "REF-"));

        var gateway = provider.GetRequiredService<IPaymentGatewayResolver>()
            .Resolve(PaymentProviderKind.Interac);

        var initiation = await gateway.InitiateAsync(SampleRequest);

        Assert.Equal(PaymentStatus.RequiresManualVerification, initiation.Status);
        Assert.True(initiation.RequiresManualVerification);
        Assert.Equal("REF-42", initiation.ProviderReference);

        // The payer must be told the amount and the reference — that reference is what lets an
        // administrator match a deposit to an order.
        Assert.Contains("49.99", initiation.Instructions);
        Assert.Contains("CAD", initiation.Instructions);
        Assert.Contains("REF-42", initiation.Instructions);
        Assert.Contains("pay@example.com", initiation.Instructions);

        // Nothing to redirect to: this rail never leaves the app before the money is sent.
        Assert.Null(initiation.RedirectUrl);
    }

    [Fact]
    public async Task An_offline_rail_never_reports_a_payment_completed_by_itself()
    {
        using var provider = Build(("Payments:Manual:Kind", "Manual"));
        var gateway = provider.GetRequiredService<IPaymentGatewayResolver>()
            .Resolve(PaymentProviderKind.Manual);

        var verification = await gateway.VerifyAsync("REF-42");

        Assert.Equal(PaymentStatus.RequiresManualVerification, verification.Status);
        Assert.NotEqual(PaymentStatus.Completed, verification.Status);

        // And an unconfirmed offline payment can never satisfy an amount check.
        Assert.False(verification.Matches(49.99m, "CAD"));
    }

    [Fact]
    public void Offline_rails_do_not_claim_recurring_support()
    {
        using var provider = Build(("Payments:Manual:Kind", "Interac"));

        Assert.False(provider.GetRequiredService<IPaymentGatewayResolver>()
            .Resolve(PaymentProviderKind.Interac).SupportsRecurring);
    }

    [Fact]
    public void Resolving_a_rail_that_is_not_enabled_fails_loudly()
    {
        using var provider = Build(("Payments:Manual:Kind", "Manual"));
        var resolver = provider.GetRequiredService<IPaymentGatewayResolver>();

        Assert.True(resolver.TryResolve(PaymentProviderKind.Manual, out _));
        Assert.False(resolver.TryResolve(PaymentProviderKind.Card, out _));
        Assert.DoesNotContain(PaymentProviderKind.Card, resolver.Available);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(PaymentProviderKind.Card));
        Assert.Contains("Card", ex.Message);
    }

    [Fact]
    public async Task Several_rails_can_be_registered_and_resolve_to_their_own_implementation()
    {
        using var provider = Build(("Payments:Manual:Kind", "Interac"));

        var services = new ServiceCollection();
        services.AddLogging();
        PaymentGatewayServiceCollectionExtensions.AddPaymentGateways(
            services, new ConfigurationBuilder().Build());
        services.AddSingleton<IPaymentGateway>(new StubGateway(PaymentProviderKind.Card));
        using var provider2 = services.BuildServiceProvider();

        var resolver = provider2.GetRequiredService<IPaymentGatewayResolver>();

        Assert.Equal(PaymentProviderKind.Card, resolver.Resolve(PaymentProviderKind.Card).Kind);
        Assert.True(resolver.Resolve(PaymentProviderKind.Card).SupportsRecurring);
        Assert.Contains(PaymentProviderKind.Manual, resolver.Available);

        // A card rail reports completion and the amount, so the caller can verify it.
        var verification = await resolver.Resolve(PaymentProviderKind.Card).VerifyAsync("pi_123");
        Assert.True(verification.Matches(10.00m, "CAD"));
    }

    [Theory]
    [InlineData(PaymentStatus.Completed, 10.00, "CAD", true)]
    [InlineData(PaymentStatus.Completed, 9.99, "CAD", false)]
    [InlineData(PaymentStatus.Completed, 10.00, "USD", false)]
    [InlineData(PaymentStatus.Pending, 10.00, "CAD", false)]
    [InlineData(PaymentStatus.RequiresManualVerification, 10.00, "CAD", false)]
    [InlineData(PaymentStatus.Failed, 10.00, "CAD", false)]
    public void A_payment_only_matches_when_it_completed_for_the_exact_amount(
        PaymentStatus status, double amount, string currency, bool expected)
    {
        var verification = new PaymentVerification(status, (decimal)amount, currency, "ref");

        Assert.Equal(expected, verification.Matches(10.00m, "CAD"));
    }

    private sealed class StubGateway(PaymentProviderKind kind) : IPaymentGateway
    {
        public PaymentProviderKind Kind => kind;
        public bool SupportsRecurring => true;

        public Task<PaymentInitiation> InitiateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaymentInitiation(PaymentStatus.Pending, "pi_123", RedirectUrl: "https://example.test/pay"));

        public Task<PaymentVerification> VerifyAsync(string providerReference, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaymentVerification(PaymentStatus.Completed, 10.00m, "CAD", providerReference));
    }
}
