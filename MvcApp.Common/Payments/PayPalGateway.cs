using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MvcApp.Common.Payments;

/// <summary>
/// The PayPal rail: hosted approval, then an authenticated capture.
///
/// One implementation, in the platform, because a rail is not a module's business. Before this there
/// were five near-identical copies — VipPayPalService plus the IPTV module's PayPalService,
/// PayPalMeService and InteractService, plus StorePayPalService — each re-reading
/// PayPal:ClientId/ClientSecret, each with its own hardcoded rate table, and each having to be fixed
/// separately for the same defect.
///
/// Culture is pinned to invariant when writing amounts: a decimal serialised under a comma-decimal
/// locale would send "12,50" to the provider.
/// </summary>
public sealed class PayPalGateway(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PayPalGateway> logger) : IPaymentGateway
{
    private const string LiveApi = "https://api.paypal.com";
    private const string ClientIdKey = "PayPal:ClientId";
    private const string ClientSecretKey = "PayPal:ClientSecret";

    public PaymentProviderKind Kind => PaymentProviderKind.PayPal;

    /// <summary>PayPal offers subscriptions, but recurring is not implemented on this rail yet.</summary>
    public bool SupportsRecurring => false;

    public async Task<PaymentInitiation> InitiateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var client = CreateClient(accessToken);

        var order = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = request.Currency,
                        value = request.Amount.ToString("F2", CultureInfo.InvariantCulture)
                    },
                    description = request.Description
                }
            },
            application_context = new
            {
                return_url = request.ReturnUrl,
                cancel_url = request.CancelUrl
            }
        };

        var response = await client.PostAsync(
            $"{LiveApi}/v2/checkout/orders",
            new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json"),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(body);
        var orderId = json.RootElement.GetProperty("id").GetString() ?? string.Empty;
        var approvalUrl = json.RootElement.GetProperty("links").EnumerateArray()
            .First(link => link.GetProperty("rel").GetString() == "approve")
            .GetProperty("href").GetString()!;

        logger.LogInformation("PayPal order {OrderId} created for {Amount} {Currency}.",
            orderId, request.Amount, request.Currency);

        return new PaymentInitiation(PaymentStatus.Pending, orderId, RedirectUrl: approvalUrl);
    }

    public async Task<PaymentVerification> CaptureAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var client = CreateClient(accessToken);

        var response = await client.PostAsync(
            $"{LiveApi}/v2/checkout/orders/{providerReference}/capture",
            new StringContent(string.Empty, Encoding.UTF8, "application/json"),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(body);
        var status = json.RootElement.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString() ?? "UNKNOWN"
            : "UNKNOWN";

        var (amount, currency) = ReadCapturedTotal(json.RootElement);

        if (status == "COMPLETED" && string.IsNullOrEmpty(currency))
        {
            // A completed status with no readable amount is not something to grant on.
            logger.LogWarning(
                "PayPal capture {Reference} reported COMPLETED but carried no amount; refusing to treat it as verified.",
                providerReference);
        }

        return new PaymentVerification(
            Status: status == "COMPLETED" ? PaymentStatus.Completed : PaymentStatus.Pending,
            Amount: amount,
            Currency: currency,
            ProviderReference: providerReference);
    }

    /// <summary>
    /// On this rail, approving and settling are the same call: the provider settles at capture, so
    /// there is nothing left to poll. Verification therefore captures — asking again would simply
    /// fail, since a capture is single use.
    /// </summary>
    public Task<PaymentVerification> VerifyAsync(string providerReference, CancellationToken cancellationToken = default)
        => CaptureAsync(providerReference, cancellationToken);

    /// <summary>
    /// Sums the captured amounts across every purchase unit. Summing rather than reading the first
    /// unit is what makes a multi-item order verifiable: reading one unit would understate the total
    /// and let a partially paid cart through (this is exactly the shape the IPTV module had).
    /// </summary>
    private static (decimal Amount, string Currency) ReadCapturedTotal(JsonElement root)
    {
        var amount = 0m;
        var currency = string.Empty;

        if (!root.TryGetProperty("purchase_units", out var units))
        {
            return (amount, currency);
        }

        foreach (var unit in units.EnumerateArray())
        {
            if (!unit.TryGetProperty("payments", out var payments) ||
                !payments.TryGetProperty("captures", out var captures))
            {
                continue;
            }

            foreach (var capture in captures.EnumerateArray())
            {
                if (!capture.TryGetProperty("amount", out var captured))
                {
                    continue;
                }

                if (captured.TryGetProperty("value", out var value))
                {
                    decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed);
                    amount += parsed;
                }

                if (string.IsNullOrEmpty(currency) && captured.TryGetProperty("currency_code", out var code))
                {
                    currency = code.GetString() ?? string.Empty;
                }
            }
        }

        return (amount, currency);
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var clientId = configuration[ClientIdKey];
        var clientSecret = configuration[ClientSecretKey];

        // Loud, not silent: a rail with no credentials must not look like a rail that works.
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                $"The PayPal rail is enabled but {ClientIdKey} / {ClientSecretKey} are not configured.");
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

        var response = await client.PostAsync(
            $"{LiveApi}/v1/oauth2/token",
            new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded"),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return json.RootElement.GetProperty("access_token").GetString()!;
    }
}
