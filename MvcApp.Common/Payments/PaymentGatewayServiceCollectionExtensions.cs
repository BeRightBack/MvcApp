using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MvcApp.Common.Payments;

/// <summary>
/// Registers the enabled rails plus the resolver. A template owner enables what they want under
/// <c>Payments:Enabled</c>; everything registered here becomes selectable at checkout.
/// </summary>
public static class PaymentGatewayServiceCollectionExtensions
{
    /// <summary>
    /// Registers the offline rail and the resolver. Card/entity rails are added alongside this as they
    /// are implemented, so the caller's checkout code only ever talks to <see cref="IPaymentGateway"/>.
    /// </summary>
    public static IServiceCollection AddPaymentGateways(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ManualPaymentOptions>(configuration.GetSection(ManualPaymentOptions.SectionName));
        services.AddSingleton<IPaymentGateway, ManualPaymentGateway>();

        services.AddSingleton<IPaymentGatewayResolver, PaymentGatewayResolver>();

        return services;
    }
}

/// <summary>
/// Resolves rails by kind from everything registered as <see cref="IPaymentGateway"/>.
/// </summary>
internal sealed class PaymentGatewayResolver(
    IEnumerable<IPaymentGateway> gateways,
    ILogger<PaymentGatewayResolver> logger) : IPaymentGatewayResolver
{
    private readonly Dictionary<PaymentProviderKind, IPaymentGateway> _byKind =
        gateways.GroupBy(g => g.Kind).ToDictionary(g => g.Key, g => g.First());

    public IReadOnlyCollection<PaymentProviderKind> Available => _byKind.Keys.ToArray();

    public bool TryResolve(PaymentProviderKind kind, out IPaymentGateway gateway)
        => _byKind.TryGetValue(kind, out gateway!);

    public IPaymentGateway Resolve(PaymentProviderKind kind)
    {
        if (_byKind.TryGetValue(kind, out var gateway))
        {
            return gateway;
        }

        // Loud, not silent: a template asking for a rail that is not enabled must not quietly end up
        // on a different one.
        logger.LogError("Payment rail {Kind} was requested but is not enabled. Enabled: {Enabled}.",
            kind, string.Join(", ", _byKind.Keys));

        throw new InvalidOperationException(
            $"Payment rail '{kind}' is not enabled. Enabled rails: {string.Join(", ", _byKind.Keys)}.");
    }
}
