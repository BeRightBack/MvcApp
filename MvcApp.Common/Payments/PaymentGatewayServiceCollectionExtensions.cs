using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MvcApp.Common.Payments;

/// <summary>
/// Registers the payment rails and the resolver. Everything the gate offers is decided here from
/// configuration, so a template or a deployment configures its payment options rather than editing
/// module code.
///
/// Rails shipping today:
/// <list type="bullet">
///   <item><description><b>PayPal</b> — always registered. Needs <c>PayPal:ClientId</c> and
///   <c>PayPal:ClientSecret</c>, and refuses to start a payment without them rather than failing
///   obscurely later.</description></item>
///   <item><description><b>Offline rails</b> — one per entry under <c>Payments:Manual:Rails</c>
///   (<c>Interac</c>, <c>PayPalMe</c>, <c>BankTransfer</c>, <c>Manual</c>), each with its own
///   destination and wording.</description></item>
/// </list>
///
/// A card rail (Stripe/Helcim/Square class) is deliberately NOT registered yet: it needs a processor
/// account plus that processor's API integration, so registering one now would offer a checkout
/// option that cannot work. When it is added it registers alongside these and nothing else changes —
/// callers only ever talk to <see cref="IPaymentGateway"/>.
/// </summary>
public static class PaymentGatewayServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PayPal rail, the offline rails configured under
    /// <see cref="ManualPaymentOptions.SectionName"/>, the activation policy and the resolver.
    /// </summary>
    public static IServiceCollection AddPaymentGateways(this IServiceCollection services, IConfiguration configuration)
    {
        // The platform registers its OWN infrastructure requirement. The PayPal rail needs a client
        // factory, and until composition existed that factory arrived incidentally from a module's
        // AddHttpClient<T>() call — so a deployment composed without that module could not resolve a
        // rail at all and failed on every request that touched it. Idempotent: repeated
        // AddHttpClient() calls do not duplicate the core services.
        services.AddHttpClient();

        // Registered as an ordinary gateway so modules resolve it through IPaymentGateway instead of
        // each holding their own copy of the integration. It uses the shared client factory and stays
        // a singleton, so the resolver never captures a stale handler.
        services.AddSingleton<IPaymentGateway, PayPalGateway>();

        foreach (var (kind, rail) in ReadConfiguredOfflineRails(configuration))
        {
            services.AddSingleton<IPaymentGateway>(provider => new ManualPaymentGateway(
                kind,
                rail,
                provider.GetService<ILogger<ManualPaymentGateway>>() ?? NullLogger<ManualPaymentGateway>.Instance));
        }

        // Whether an unverifiable payment claim activates on its own is a per-template choice.
        services.Configure<ActivationOptions>(configuration.GetSection(ActivationOptions.SectionName));
        services.AddSingleton<ActivationPolicy>();

        services.AddSingleton<IPaymentGatewayResolver, PaymentGatewayResolver>();

        return services;
    }

    /// <summary>
    /// Reads <c>Payments:Manual:Rails:&lt;Kind&gt;</c>. An unrecognised key is a configuration error
    /// and throws rather than being skipped: silently dropping a rail someone configured would leave a
    /// checkout option that looks configured but is missing at runtime — the failure would show up as
    /// a customer unable to pay.
    /// </summary>
    private static IEnumerable<(PaymentProviderKind Kind, ManualRailOptions Rail)> ReadConfiguredOfflineRails(
        IConfiguration configuration)
    {
        var options = configuration.GetSection(ManualPaymentOptions.SectionName).Get<ManualPaymentOptions>()
                      ?? new ManualPaymentOptions();

        foreach (var (name, rail) in options.Rails)
        {
            if (!Enum.TryParse<PaymentProviderKind>(name, ignoreCase: true, out var kind))
            {
                throw new InvalidOperationException(
                    $"'{ManualPaymentOptions.SectionName}:Rails:{name}' is not a known payment rail. "
                    + $"Valid names: {string.Join(", ", Enum.GetNames<PaymentProviderKind>())}.");
            }

            yield return (kind, rail);
        }
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
        // on a different one, and the message should say how to enable it.
        logger.LogError("Payment rail {Kind} was requested but is not enabled. Enabled: {Enabled}.",
            kind, string.Join(", ", _byKind.Keys));

        throw new InvalidOperationException(
            $"Payment rail '{kind}' is not enabled. Enabled rails: {string.Join(", ", _byKind.Keys)}. "
            + $"Offline rails are added under {ManualPaymentOptions.SectionName}:Rails.");
    }
}
