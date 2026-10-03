using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MvcApp.Common.Payments;

/// <summary>
/// Configuration for an offline rail (bank transfer, Interac e-Transfer, or a plain manual
/// settlement). Bound from <c>Payments:Manual</c>, so a template owner sets their own recipient and
/// wording without touching code.
/// </summary>
public sealed class ManualPaymentOptions
{
    public const string SectionName = "Payments:Manual";

    /// <summary>Which kind this instance reports as, so one class serves several offline rails.</summary>
    public PaymentProviderKind Kind { get; set; } = PaymentProviderKind.Manual;

    /// <summary>Who receives the money (e-transfer address, bank details, or free text).</summary>
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
public sealed class ManualPaymentGateway(IOptions<ManualPaymentOptions> options, ILogger<ManualPaymentGateway> logger)
    : IPaymentGateway
{
    private readonly ManualPaymentOptions _options = options.Value;

    public PaymentProviderKind Kind => _options.Kind;

    /// <summary>Offline rails cannot bill repeatedly; the payer must send each time.</summary>
    public bool SupportsRecurring => false;

    public Task<PaymentInitiation> InitiateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var reference = $"{_options.ReferencePrefix}{request.Reference}";

        var instructions = _options.InstructionsTemplate
            .Replace("{amount}", request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{currency}", request.Currency)
            .Replace("{reference}", reference)
            .Replace("{recipient}", _options.Recipient);

        logger.LogInformation(
            "Offline payment requested for {Reference}: {Amount} {Currency} via {Kind}.",
            reference, request.Amount, request.Currency, Kind);

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
