using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MvcApp.Module.Store.Services;

/// <summary>
/// Thin PayPal Orders v2 client used by the Store checkout. Creates an order
/// the buyer approves on PayPal and captures it on return. Mirrors the pattern
/// already used by the IPTV module (MvcApp.Module.IPTV.Services.PayPalService).
/// </summary>
public class StorePayPalService(HttpClient httpClient, IConfiguration configuration)
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

    /// <summary>
    /// Creates a PayPal order for a single purchase unit and returns the approval
    /// URL the buyer must be redirected to.
    /// </summary>
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

    /// <summary>
    /// Captures an approved PayPal order and returns its status (e.g. COMPLETED).
    /// </summary>
    public async Task<string> CaptureOrderAsync(string payPalOrderId)
    {
        var accessToken = await GetAccessTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"https://api.paypal.com/v2/checkout/orders/{payPalOrderId}/capture", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(responseContent);
        return json.RootElement.GetProperty("status").GetString() ?? "UNKNOWN";
    }
}
