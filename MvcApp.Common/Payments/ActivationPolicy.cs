using Microsoft.Extensions.Options;

namespace MvcApp.Common.Payments;

/// <summary>
/// Who activates a subscription whose payment arrived over a rail no machine can verify (bank
/// transfer, Interac e-Transfer, PayPal.Me). A template owner chooses; the platform does not decide.
/// </summary>
public enum ActivationMode
{
    /// <summary>A human checks the deposit before the subscription is activated. The default.</summary>
    AdminConfirmed,

    /// <summary>The customer's own claim activates it. Fastest, but nothing is verified.</summary>
    AutomaticOnClaim,

    /// <summary>Claims below a configured amount auto-activate; larger ones wait for a human.</summary>
    AutomaticBelowThreshold
}

/// <summary>Bound from <c>Payments:Activation</c>, so this is a per-template choice.</summary>
public sealed class ActivationOptions
{
    public const string SectionName = "Payments:Activation";

    /// <summary>
    /// Defaults to <see cref="ActivationMode.AdminConfirmed"/> deliberately: a claim is not a payment,
    /// and the previous behaviour (self-activation on a customer's own POST, generating working
    /// credentials) let any signed-in user grant themselves a subscription for free. An owner who
    /// wants the fast path can turn it on knowing exactly what they are trading.
    /// </summary>
    public ActivationMode Mode { get; set; } = ActivationMode.AdminConfirmed;

    /// <summary>Only used by <see cref="ActivationMode.AutomaticBelowThreshold"/>.</summary>
    public decimal AutoActivateBelowAmount { get; set; }
}

/// <summary>
/// Decides whether an unverified payment claim may activate on its own. Kept free of EF and the
/// controller so the policy is testable on its own — the decision is the part worth pinning down.
/// </summary>
public sealed class ActivationPolicy(IOptions<ActivationOptions> options)
{
    public ActivationMode Mode => options.Value.Mode;

    /// <summary>Whether a claim of <paramref name="amount"/> activates without a human.</summary>
    public bool ShouldActivateAutomatically(decimal amount) => Mode switch
    {
        ActivationMode.AutomaticOnClaim => true,
        ActivationMode.AutomaticBelowThreshold =>
            options.Value.AutoActivateBelowAmount > 0m
            && amount > 0m
            && amount < options.Value.AutoActivateBelowAmount,
        _ => false
    };
}
