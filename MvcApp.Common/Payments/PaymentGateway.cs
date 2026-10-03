namespace MvcApp.Common.Payments;

/// <summary>The rail a payment travels over. A template owner enables the ones they want.</summary>
public enum PaymentProviderKind
{
    /// <summary>Entity-mediated card wallet (PayPal checkout, hosted approval + capture).</summary>
    PayPal,

    /// <summary>PayPal.Me style link: the payer brings their own account, no capture API.</summary>
    PayPalMe,

    /// <summary>Canadian bank push payment (Interac e-Transfer).</summary>
    Interac,

    /// <summary>Any other bank transfer.</summary>
    BankTransfer,

    /// <summary>A card processor (Stripe/Helcim/Square class), selected by configuration.</summary>
    Card,

    /// <summary>Fully offline: the amount is settled outside the app and confirmed by a human.</summary>
    Manual
}

/// <summary>
/// Lifecycle of a payment. <see cref="RequiresManualVerification"/> is the important one for offline
/// rails: the app has recorded what is owed, but no machine can confirm the money arrived.
/// </summary>
public enum PaymentStatus
{
    Pending,
    RequiresManualVerification,
    Completed,
    Failed,
    Cancelled,
    Refunded
}

/// <summary>What is being paid for. <paramref name="Reference"/> is the app's own id (order or subscription).</summary>
public sealed record PaymentRequest(
    decimal Amount,
    string Currency,
    string Reference,
    string Description,
    string? ReturnUrl = null,
    string? CancelUrl = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

/// <summary>
/// The result of starting a payment: either somewhere to send the payer, or instructions the app
/// displays (offline rails), plus the provider reference that ties the two together.
/// </summary>
public sealed record PaymentInitiation(
    PaymentStatus Status,
    string ProviderReference,
    string? RedirectUrl = null,
    string? Instructions = null,
    bool RequiresManualVerification = false);

/// <summary>
/// The outcome of asking the provider what happened. <see cref="Amount"/> and <see cref="Currency"/>
/// are always carried, because a status alone cannot tell a small payment from a large one — the
/// defect that made the old per-module handlers grant entitlements for the wrong amount.
/// </summary>
public sealed record PaymentVerification(
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    string ProviderReference,
    string? FailureReason = null)
{
    /// <summary>
    /// True only when the payment completed AND matches the expected amount and currency exactly.
    /// Callers must gate grants on this, never on <see cref="Status"/> alone.
    /// </summary>
    public bool Matches(decimal expectedAmount, string expectedCurrency)
        => Status == PaymentStatus.Completed
           && string.Equals(Currency, expectedCurrency, StringComparison.OrdinalIgnoreCase)
           && Amount == expectedAmount;
}

/// <summary>
/// One payment rail. Implementations must never claim a payment completed on their own authority:
/// completion comes from the provider (verified signature or authenticated capture call) or, for
/// offline rails, from an explicit human confirmation.
/// </summary>
public interface IPaymentGateway
{
    PaymentProviderKind Kind { get; }

    /// <summary>Whether the rail can bill repeatedly without the payer re-approving each time.</summary>
    bool SupportsRecurring { get; }

    Task<PaymentInitiation> InitiateAsync(PaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ask the provider what happened to a reference. Implementations must return the captured amount
    /// so the caller can compare it with what was quoted.
    /// </summary>
    Task<PaymentVerification> VerifyAsync(string providerReference, CancellationToken cancellationToken = default);
}

/// <summary>
/// Picks a rail by kind. Resolving one that is not enabled is an explicit failure rather than a
/// silent fallback, so a misconfigured template fails loudly instead of taking money on the wrong rail.
/// </summary>
public interface IPaymentGatewayResolver
{
    IReadOnlyCollection<PaymentProviderKind> Available { get; }

    bool TryResolve(PaymentProviderKind kind, out IPaymentGateway gateway);

    IPaymentGateway Resolve(PaymentProviderKind kind);
}
