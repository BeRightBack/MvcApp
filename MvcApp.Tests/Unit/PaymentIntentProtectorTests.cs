using Microsoft.AspNetCore.DataProtection;
using MvcApp.Common.Payments;
using Xunit;

namespace MvcApp.Tests.Unit;

/// <summary>
/// Guards audit 3.12: the payment success handlers used to read the plan and detail from the query
/// string and check only the capture *status*, so a buyer could start a checkout for the cheapest
/// detail, take that order's provider token, and present it with an expensive detail id — the
/// payment completed and the expensive plan was granted. The binding must be unforgeable, and a
/// valid token must only ever be honoured for the exact plan/detail/amount it was issued for.
/// </summary>
public class PaymentIntentProtectorTests
{
    private static PaymentIntentProtector NewProtector()
        => new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Round_trips_the_quoted_intent()
    {
        var protector = NewProtector();

        var payload = protector.Protect(planId: 3, detailId: 42, amount: 9.99m, currency: "USD");
        var intent = protector.Unprotect(payload);

        Assert.NotNull(intent);
        Assert.Equal(3, intent!.Value.PlanId);
        Assert.Equal(42, intent.Value.DetailId);
        Assert.Equal(9.99m, intent.Value.Amount);
        Assert.Equal("USD", intent.Value.Currency);
    }

    [Fact]
    public void An_expensive_detail_cannot_be_claimed_with_a_cheap_checkouts_binding()
    {
        var protector = NewProtector();

        // The attacker's position: they hold a legitimate binding for a cheap detail and want the
        // grant to use an expensive one.
        var cheap = protector.Protect(planId: 1, detailId: 10, amount: 1.00m, currency: "USD");
        var intent = protector.Unprotect(cheap);

        Assert.Equal(1.00m, intent!.Value.Amount);
        Assert.Equal(10, intent.Value.DetailId);

        // There is no way to mint the expensive binding without the key ring, and mutating the cheap
        // one destroys it rather than changing it.
        var tampered = cheap[..^1] + (cheap[^1] == 'A' ? 'B' : 'A');
        Assert.Null(protector.Unprotect(tampered));
    }

    [Fact]
    public void A_payload_from_another_key_ring_is_rejected()
    {
        // A forged or foreign-key payload must not be honoured — otherwise anyone could hand-craft a
        // binding claiming any plan they liked.
        var payload = NewProtector().Protect(1, 10, 1.00m, "USD");
        var other = NewProtector().Unprotect(payload);

        Assert.Null(other);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-protected-payload")]
    [InlineData("CfDJ8AAAAA-not-really-base64")]
    public void Missing_garbage_or_unreadable_payloads_are_rejected(string? payload)
    {
        Assert.Null(NewProtector().Unprotect(payload));
    }
}
