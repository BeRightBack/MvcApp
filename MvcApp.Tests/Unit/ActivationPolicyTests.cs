using Microsoft.Extensions.Options;
using MvcApp.Common.Payments;
using Xunit;

namespace MvcApp.Tests.Unit;

/// <summary>
/// Whether an unverifiable payment claim activates on its own is a per-template setting, so the
/// default matters more than any single mode: it must be the safe one. The previous behaviour
/// (activation on the customer's own claim) let any signed-in user grant themselves a subscription
/// for free, so an unconfigured template must not inherit that.
/// </summary>
public class ActivationPolicyTests
{
    private static ActivationPolicy Policy(ActivationMode mode, decimal threshold = 0m)
        => new(Options.Create(new ActivationOptions { Mode = mode, AutoActivateBelowAmount = threshold }));

    [Fact]
    public void The_default_mode_requires_a_human()
    {
        // No configuration at all — what a new template gets.
        var policy = new ActivationPolicy(Options.Create(new ActivationOptions()));

        Assert.Equal(ActivationMode.AdminConfirmed, policy.Mode);
        Assert.False(policy.ShouldActivateAutomatically(0m));
        Assert.False(policy.ShouldActivateAutomatically(9.99m));
        Assert.False(policy.ShouldActivateAutomatically(5000m));
    }

    [Fact]
    public void Admin_confirmed_never_activates_without_one()
    {
        var policy = Policy(ActivationMode.AdminConfirmed);

        Assert.False(policy.ShouldActivateAutomatically(1m));
        Assert.False(policy.ShouldActivateAutomatically(999m));
    }

    [Fact]
    public void Automatic_on_claim_activates_for_any_amount()
    {
        var policy = Policy(ActivationMode.AutomaticOnClaim);

        Assert.True(policy.ShouldActivateAutomatically(1m));
        Assert.True(policy.ShouldActivateAutomatically(500m));
    }

    [Theory]
    [InlineData(49.99, true)]    // below the threshold
    [InlineData(0.01, true)]     // smallest sane amount
    [InlineData(50.00, false)]   // equal to the threshold is NOT below it
    [InlineData(120.00, false)]  // above it
    [InlineData(0.00, false)]    // a zero claim is not a payment
    public void Below_threshold_only_auto_activates_below_it(decimal amount, bool expected)
    {
        var policy = Policy(ActivationMode.AutomaticBelowThreshold, threshold: 50m);

        Assert.Equal(expected, policy.ShouldActivateAutomatically(amount));
    }

    [Fact]
    public void A_threshold_mode_without_a_threshold_activates_nothing()
    {
        // Misconfiguration must fail closed — not auto-activate everything.
        var policy = Policy(ActivationMode.AutomaticBelowThreshold, threshold: 0m);

        Assert.False(policy.ShouldActivateAutomatically(1m));
        Assert.False(policy.ShouldActivateAutomatically(1000m));
    }
}
