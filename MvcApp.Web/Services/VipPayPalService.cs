using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MvcApp.Web.Services;

public class VipPayPalService(HttpClient httpClient, IConfiguration configuration)
{
    private async Task<string> GetAccessTokenAsync()
    {
        var clientId = configuration["PayPal:ClientId"];
        var clientSecret = configuration["PayPal:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("PayPal:ClientId / PayPal:ClientSecret are not configured.");

        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        var requestBody = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await httpClient.PostAsync("https://api.paypal.com/v1/oauth2/token", requestBody);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    public async Task<string> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl)
    {
        var accessToken = await GetAccessTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = currency,
                        value = amount.ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        var requestBody = new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.paypal.com/v2/checkout/orders", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(responseContent);
        return json.RootElement.GetProperty("links").EnumerateArray()
            .First(link => link.GetProperty("rel").GetString() == "approve")
            .GetProperty("href").GetString()!;
    }

    /// <summary>What the provider actually captured — the status alone cannot tell a $5 order from a $500 one.</summary>
    public readonly record struct PayPalCapture(string Status, decimal Amount, string Currency);

    public async Task<PayPalCapture> CaptureOrderAsync(string payPalOrderId)
    {
        var accessToken = await GetAccessTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"https://api.paypal.com/v2/checkout/orders/{payPalOrderId}/capture", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(responseContent);
        var status = json.RootElement.GetProperty("status").GetString() ?? "UNKNOWN";

        // The captured amount is read back so the caller can compare it with what was quoted. The
        // previous version returned only the status, which made any amount check impossible.
        var amount = 0m;
        var currency = string.Empty;

        if (json.RootElement.TryGetProperty("purchase_units", out var units) && units.GetArrayLength() > 0 &&
            units[0].TryGetProperty("payments", out var payments) &&
            payments.TryGetProperty("captures", out var captures) && captures.GetArrayLength() > 0 &&
            captures[0].TryGetProperty("amount", out var capturedAmount))
        {
            if (capturedAmount.TryGetProperty("value", out var value))
            {
                decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
            }

            if (capturedAmount.TryGetProperty("currency_code", out var code))
            {
                currency = code.GetString() ?? string.Empty;
            }
        }

        return new PayPalCapture(status, amount, currency);
    }
}
