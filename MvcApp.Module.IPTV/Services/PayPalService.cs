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

    public async Task<string> CaptureOrderAsync(string orderId)
    {
        var accessToken = await GetAccessTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"https://api.paypal.com/v2/checkout/orders/{orderId}/capture", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return responseContent;
    }
}
