using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MvcApp.Core;

namespace MvcApp.Module.IPTV.Services;

public class PayPalService(HttpClient httpClient, IConfiguration configuration)
{
    private async Task<string> GetAccessTokenAsync()
    {
        var clientId = configuration["PayPal:ClientId"];
        var clientSecret = configuration["PayPal:ClientSecret"];
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        var requestBody = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await httpClient.PostAsync("https://api.paypal.com/v1/oauth2/token", requestBody);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(responseContent);
        return jsonDocument.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        return await Task.Run(() =>
        {
            if (fromCurrency == toCurrency)
            {
                return amount;
            }

            // Example conversion rates, replace with actual API call if needed
            var conversionRates = new Dictionary<string, decimal>
            {
                { "USD", 1.0m },
                { "EUR", 0.95m },
                { "CAD", 1.33m }
            };

            if (!conversionRates.ContainsKey(fromCurrency) || !conversionRates.ContainsKey(toCurrency))
            {
                throw new ArgumentException("Unsupported currency.");
            }

            var rate = conversionRates[toCurrency] / conversionRates[fromCurrency];
            return amount * rate;
        });
    }

    public async Task<string> CreateOrderAsync(string returnUrl, string cancelUrl, IEnumerable<ShoppingCartItem> cartItems, string currency)
    {
        var accessToken = await GetAccessTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var purchaseUnits = new List<object>();

        foreach (var item in cartItems)
        {
            var convertedPrice = await ConvertCurrencyAsync(item.SubscriptionDetail!.Price, "USD", currency);
            purchaseUnits.Add(new
            {
                amount = new
                {
                    currency_code = currency,
                    value = convertedPrice.ToString("F2", CultureInfo.InvariantCulture) // Ensure correct format
                },
                description = item.SubscriptionPlan!.Name
            });
        }

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = purchaseUnits,
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        var requestBody = new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.paypal.com/v2/checkout/orders", requestBody);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {errorContent}");
            Console.WriteLine($"Request: {JsonSerializer.Serialize(orderRequest)}");
            Console.WriteLine($"Response: {responseContent}");
            response.EnsureSuccessStatusCode();
        }

        return responseContent;
    }

    /// <summary>
    /// The total this service would charge for a cart, in the given currency.
    ///
    /// Exposed so payment confirmation can verify the captured amount against the SAME conversion
    /// that created the order. Recomputing it at the call site would mean a second copy of the rate
    /// table, which is how a verification ends up disagreeing with the charge it is verifying.
    /// </summary>
    public async Task<decimal> ExpectedTotalAsync(IEnumerable<ShoppingCartItem> cartItems, string currency)
    {
        var total = 0m;
        foreach (var item in cartItems)
        {
            total += await ConvertCurrencyAsync(item.SubscriptionDetail?.Price ?? 0m, "USD", currency);
        }

        return decimal.Round(total, 2);
    }

    /// <summary>What the provider actually captured, summed across the order's purchase units.</summary>
    public readonly record struct PayPalCapture(string Status, decimal Amount, string Currency);

    /// <summary>
    /// Captures an approved order and returns the status plus the TOTAL captured amount.
    ///
    /// The total is summed across purchase_units because CreateOrderAsync builds one unit per cart
    /// item — reading only the first unit's amount would understate any multi-item order and let a
    /// partially-paid cart through. The previous version returned the raw JSON for the caller to
    /// pick the status out of, so no amount check was possible at all (audit 3.12).
    /// </summary>
    public async Task<PayPalCapture> CaptureOrderAsync(string orderId)
    {
        var accessToken = await GetAccessTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"https://api.paypal.com/v2/checkout/orders/{orderId}/capture", content);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var status = json.RootElement.GetProperty("status").GetString() ?? "UNKNOWN";

        var amount = 0m;
        var currency = string.Empty;

        if (json.RootElement.TryGetProperty("purchase_units", out var units))
        {
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
        }

        return new PayPalCapture(status, amount, currency);
    }
}
