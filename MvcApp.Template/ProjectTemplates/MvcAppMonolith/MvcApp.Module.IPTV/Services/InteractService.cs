using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace MvcApp.Module.IPTV.Services;

public class InteractService(IConfiguration configuration)
{
    private static async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency == toCurrency)
        {
            return await Task.FromResult(amount);
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
        return await Task.FromResult(amount * rate);
    }

    public async Task<string> GenerateInteractPaymentInstructionsAsync(decimal amount)
    {
        var interactEmail = configuration["Interact:Email"];
        var convertedAmount = await ConvertCurrencyAsync(amount, "USD", "CAD");
        var interactInstructions = $"Please send an Interact e-Transfer of <strong>${convertedAmount.ToString("F2", CultureInfo.InvariantCulture)} cad</strong> to <strong>{interactEmail}</strong>.";
        return interactInstructions;
    }
}
