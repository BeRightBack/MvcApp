using System.Globalization;
using Microsoft.AspNetCore.DataProtection;

namespace MvcApp.Common.Payments;

/// <summary>
/// The checkout a payment is being made for: which plan/detail was quoted and for how much.
/// </summary>
public readonly record struct PaymentIntent(int PlanId, int DetailId, decimal Amount, string Currency);

/// <summary>
/// Tamper-proof binding between a checkout and the payment that comes back from the provider.
///
/// Why: the payment success handlers used to take the plan and detail straight from the query
/// string, so the ids the customer was shown and the ids the grant used could be edited in the
/// browser. A buyer could start a checkout for the cheapest detail, take the resulting provider
/// token, and present it with an expensive detail id — the payment completed, and the expensive
/// plan was granted (audit 3.12). There was also no check of the captured amount, because the
/// capture call only returned a status.
///
/// The fix is to stop trusting the parameters: at checkout the plan, detail and quoted amount are
/// sealed into a signed payload that the provider carries back verbatim, and the grant is made from
/// the sealed values. Data Protection (which this app already persists outside the repo) makes the
/// payload unforgeable, so the ids cannot be swapped. That is a binding without needing a database
/// column for the provider's order id.
///
/// Pair it with an amount comparison against the capture: a valid token for the wrong order is
/// still the wrong amount of money.
/// </summary>
public sealed class PaymentIntentProtector
{
    private const string Purpose = "MvcApp.PaymentIntent.v1";

    private readonly IDataProtector _protector;

    public PaymentIntentProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector(Purpose);

    public string Protect(int planId, int detailId, decimal amount, string currency)
        => _protector.Protect(string.Create(CultureInfo.InvariantCulture,
            $"{planId}|{detailId}|{amount}|{currency}"));

    /// <summary>
    /// The bound intent, or null when the payload is missing, tampered with, or unreadable. Callers
    /// must treat null as "no payment to honour" and grant nothing.
    /// </summary>
    public PaymentIntent? Unprotect(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        string plaintext;
        try
        {
            plaintext = _protector.Unprotect(payload);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Forged, truncated, or sealed with a different key ring / application name.
            return null;
        }

        var parts = plaintext.Split('|');
        if (parts.Length != 4 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var planId) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var detailId) ||
            !decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return null;
        }

        return new PaymentIntent(planId, detailId, amount, parts[3]);
    }

    /// <summary>
    /// Seals only an amount and currency, for a checkout whose plan identity is already recorded
    /// server-side before the customer leaves the app (the IPTV flow creates its pending
    /// subscriptions at checkout, so what is owed is already stored and only the figure needs
    /// protecting). The returned intent carries zero ids, which such a caller ignores.
    /// </summary>
    public string ProtectAmount(decimal amount, string currency) => Protect(0, 0, amount, currency);

    /// <summary>The amount/currency intent, or null if the payload is missing or unreadable.</summary>
    public PaymentIntent? UnprotectAmount(string? payload) => Unprotect(payload);
}
