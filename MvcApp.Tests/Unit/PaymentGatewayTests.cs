using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Common.Payments;
using Xunit;

namespace MvcApp.Tests.Unit;

/// <summary>
/// The payment gate exists so a template owner can configure whichever rails they want without the
/// checkout code knowing which one it is. The properties pinned down here are the ones that would
/// otherwise fail quietly in production: an offline rail must never report a payment completed on its
/// own authority (only a human can see money arrive), asking for a rail that is not enabled must fail
/// loudly rather than take the money on a different one, and a rail someone configured must not be
/// silently dropped.
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
        // Mirror the real container: a client factory and the configuration are both present there,
        // and the PayPal rail resolves both.
        services.AddHttpClient();
        services.AddSingleton<IConfiguration>(configuration);
        PaymentGatewayServiceCollectionExtensions.AddPaymentGateways(services, configuration);

        return services.BuildServiceProvider();
    }

    private static readonly PaymentRequest SampleRequest =
        new(Amount: 49.99m, Currency: "CAD", Reference: "42", Description: "VIP plan");

    [Fact]
    public async Task An_offline_rail_records_what_is_owed_and_waits_for_a_human()
    {
        using var provider = Build(
            ("Payments:Manual:Rails:Interac:Recipient", "pay@example.com"),
            ("Payments:Manual:Rails:Interac:ReferencePrefix", "REF-"));

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
    public async Task Two_offline_rails_can_be_configured_independently()
    {
        // The normal case: an e-transfer address and a payment link do not share a destination.
        using var provider = Build(
            ("Payments:Manual:Rails:Interac:Recipient", "transfer@example.com"),
            ("Payments:Manual:Rails:Interac:ReferencePrefix", "BANK-"),
            ("Payments:Manual:Rails:PayPalMe:Recipient", "https://paypal.me/example"),
            ("Payments:Manual:Rails:PayPalMe:ReferencePrefix", "ME-"));

        var resolver = provider.GetRequiredService<IPaymentGatewayResolver>();

        var interac = await resolver.Resolve(PaymentProviderKind.Interac).InitiateAsync(SampleRequest);
        var payPalMe = await resolver.Resolve(PaymentProviderKind.PayPalMe).InitiateAsync(SampleRequest);

        Assert.Contains("transfer@example.com", interac.Instructions);
        Assert.Contains("BANK-42", interac.ProviderReference);

        Assert.Contains("https://paypal.me/example", payPalMe.Instructions);
        Assert.Contains("ME-42", payPalMe.ProviderReference);

        // Neither leaks the other's destination.
        Assert.DoesNotContain("paypal.me", interac.Instructions);
    }

    [Fact]
    public async Task An_offline_rail_never_reports_a_payment_completed_by_itself()
    {
        using var provider = Build(("Payments:Manual:Rails:Manual:Recipient", "somewhere"));
        var gateway = provider.GetRequiredService<IPaymentGatewayResolver>()
            .Resolve(PaymentProviderKind.Manual);

        var verification = await gateway.VerifyAsync("REF-42");

        Assert.Equal(PaymentStatus.RequiresManualVerification, verification.Status);
        Assert.NotEqual(PaymentStatus.Completed, verification.Status);

        // And an unconfirmed offline payment can never satisfy an amount check.
        Assert.False(verification.Matches(49.99m, "CAD"));

        // Capturing is equally powerless: only an administrator can advance an offline payment.
        var capture = await gateway.CaptureAsync("REF-42");
        Assert.NotEqual(PaymentStatus.Completed, capture.Status);
    }

    [Fact]
    public void Offline_rails_do_not_claim_recurring_support()
    {
        using var provider = Build(("Payments:Manual:Rails:Interac:Recipient", "pay@example.com"));

        Assert.False(provider.GetRequiredService<IPaymentGatewayResolver>()
            .Resolve(PaymentProviderKind.Interac).SupportsRecurring);
    }

    [Fact]
    public void Resolving_a_rail_that_is_not_enabled_fails_loudly_and_says_how()
    {
        using var provider = Build(("Payments:Manual:Rails:Manual:Recipient", "somewhere"));
        var resolver = provider.GetRequiredService<IPaymentGatewayResolver>();

        Assert.True(resolver.TryResolve(PaymentProviderKind.Manual, out _));
        Assert.False(resolver.TryResolve(PaymentProviderKind.Card, out _));
        Assert.DoesNotContain(PaymentProviderKind.Card, resolver.Available);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(PaymentProviderKind.Card));
        Assert.Contains("Card", ex.Message);
        // The message must point at where a rail is configured, or the operator is left guessing.
        Assert.Contains("Payments:Manual:Rails", ex.Message);
    }

    [Fact]
    public void A_misspelled_rail_name_is_a_configuration_error_not_a_silent_drop()
    {
        // Skipping it would leave a checkout option that looks configured and is missing at runtime,
        // which surfaces as a customer who cannot pay.
        var ex = Assert.Throws<InvalidOperationException>(() => Build(
            ("Payments:Manual:Rails:InteracTransfer:Recipient", "pay@example.com")));

        Assert.Contains("InteracTransfer", ex.Message);
        Assert.Contains("Interac", ex.Message);
    }

    [Fact]
    public async Task Several_rails_can_be_registered_and_resolve_to_their_own_implementation()
    {
        // PayPal is always present; a card rail is not registered here because it needs a processor
        // account, so the gate must not pretend to offer one.
        using var provider = Build(("Payments:Manual:Rails:Interac:Recipient", "pay@example.com"));
        var resolver = provider.GetRequiredService<IPaymentGatewayResolver>();

        Assert.Contains(PaymentProviderKind.PayPal, resolver.Available);
        Assert.Contains(PaymentProviderKind.Interac, resolver.Available);
        Assert.DoesNotContain(PaymentProviderKind.Card, resolver.Available);

        // A rail registered alongside these resolves to itself, and is usable through the interface.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        var secondConfiguration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(secondConfiguration);
        PaymentGatewayServiceCollectionExtensions.AddPaymentGateways(services, secondConfiguration);
        services.AddSingleton<IPaymentGateway>(new StubGateway(PaymentProviderKind.Card));
        using var provider2 = services.BuildServiceProvider();

        var resolver2 = provider2.GetRequiredService<IPaymentGatewayResolver>();

        Assert.Equal(PaymentProviderKind.Card, resolver2.Resolve(PaymentProviderKind.Card).Kind);
        Assert.True(resolver2.Resolve(PaymentProviderKind.Card).SupportsRecurring);

        var verification = await resolver2.Resolve(PaymentProviderKind.Card).VerifyAsync("pi_123");
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

        public Task<PaymentVerification> CaptureAsync(string providerReference, CancellationToken cancellationToken = default)
            => VerifyAsync(providerReference, cancellationToken);
    }
}
