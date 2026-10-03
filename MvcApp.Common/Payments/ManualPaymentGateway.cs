using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MvcApp.Common.Payments;

/// <summary>
/// Configuration for the offline rails, bound from <c>Payments:Manual</c>.
///
/// Rails are keyed by <see cref="PaymentProviderKind"/> name, so a template owner can offer Interac
/// e-Transfer and PayPal.Me side by side with different destinations and different wording — which is
/// the normal case, since those two rarely share an address:
///
/// <code>
/// "Payments": {
///   "Manual": {
///     "Rails": {
///       "Interac":  { "Recipient": "payments@example.com", "ReferencePrefix": "IPTV-" },
///       "PayPalMe": { "Recipient": "https://paypal.me/yourname", "ReferencePrefix": "IPTV-" }
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class ManualPaymentOptions
{
    public const string SectionName = "Payments:Manual";

    /// <summary>Offline rails to offer, keyed by rail name.</summary>
    public Dictionary<string, ManualRailOptions> Rails { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>One offline rail's settings.</summary>
public sealed class ManualRailOptions
{
    /// <summary>
    /// Who receives the money: an e-transfer address, a payment link, bank details, or free text.
    /// This is shown to the payer, so it must be the real destination.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Instructions shown to the payer. <c>{amount}</c>, <c>{currency}</c>, <c>{reference}</c> and
    /// <c>{recipient}</c> are substituted. Keep the reference visible — it is what lets an
    /// administrator match a deposit to an order.
    /// </summary>
    public string InstructionsTemplate { get; set; } =
        "Send {amount} {currency} to {recipient} and include the reference {reference} in the message.";

    /// <summary>Prefix applied to the reference the payer must quote.</summary>
    public string ReferencePrefix { get; set; } = "REF-";
}

/// <summary>
/// Offline settlement: the app records what is owed and shows the payer how to send it, then waits.
///
/// This rail exists because a site owner may not be able to take cards at all, or may prefer not to
/// (a support/donation model, a bank-transfer-only business, or a line of business a card processor
/// will not underwrite). What it deliberately does NOT do is present itself as anything other than
/// what it is: the instructions state the amount and the reference, and
/// <see cref="VerifyAsync"/> never reports <see cref="PaymentStatus.Completed"/> on its own, because
/// no machine can see the money arrive. Activation is an explicit, auditable human step.
/// </summary>
public sealed class ManualPaymentGateway(
    PaymentProviderKind kind,
    ManualRailOptions rail,
    ILogger<ManualPaymentGateway> logger) : IPaymentGateway
{
    public PaymentProviderKind Kind => kind;

    /// <summary>Offline rails cannot bill repeatedly; the payer must send each time.</summary>
    public bool SupportsRecurring => false;

    public Task<PaymentInitiation> InitiateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var reference = $"{rail.ReferencePrefix}{request.Reference}";

        var instructions = rail.InstructionsTemplate
            .Replace("{amount}", request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{currency}", request.Currency)
            .Replace("{reference}", reference)
            .Replace("{recipient}", rail.Recipient);

        logger.LogInformation(
            "Offline payment requested for {Reference}: {Amount} {Currency} via {Kind}.",
            reference, request.Amount, request.Currency, kind);

        return Task.FromResult(new PaymentInitiation(
            Status: PaymentStatus.RequiresManualVerification,
            ProviderReference: reference,
            Instructions: instructions,
            RequiresManualVerification: true));
    }

    /// <summary>
    /// Always pending: an offline rail has nothing to poll, and reporting completion here would be
    /// the app inventing a payment. An administrator confirms receipt against the reference.
    /// </summary>
    public Task<PaymentVerification> VerifyAsync(string providerReference, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentVerification(
            Status: PaymentStatus.RequiresManualVerification,
            Amount: 0m,
            Currency: string.Empty,
            ProviderReference: providerReference,
            FailureReason: null));

    /// <summary>
    /// Also always pending: there is nothing to capture. An offline rail settles outside the app, so
    /// this deliberately cannot advance the payment — only an administrator can.
    /// </summary>
    public Task<PaymentVerification> CaptureAsync(string providerReference, CancellationToken cancellationToken = default)
        => VerifyAsync(providerReference, cancellationToken);
}
