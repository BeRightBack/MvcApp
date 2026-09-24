using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace MvcApp.Module.IPTV.Services;

public class PayPalMeService(HttpClient httpClient, IConfiguration configuration)
{
    private async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
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
            { "GBP", 0.85m },
            { "CAD", 1.33m }
        };

        if (!conversionRates.ContainsKey(fromCurrency) || !conversionRates.ContainsKey(toCurrency))
        {
            throw new ArgumentException("Unsupported currency.");
        }

        var rate = conversionRates[toCurrency] / conversionRates[fromCurrency];
        return await Task.FromResult(amount * rate);
    }

    public async Task<string> GeneratePayPalMeLink(decimal amount, string currency)
    {
        var payPalMeUsername = configuration["PayPal:PayPalMeUsername"];
        var convertedAmount = await ConvertCurrencyAsync(amount, "USD", currency);
        return $"https://www.paypal.me/{payPalMeUsername}/{convertedAmount.ToString("F2", CultureInfo.InvariantCulture)}{currency.ToLower()}";
    }
}
