using Microsoft.Extensions.DependencyInjection;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Services;

namespace MvcApp.Module.IPTV;

public static class ServiceRegistration
{
    public static IServiceCollection AddIptv(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);

        services.AddTransient<IShoppingCartService, ShoppingCartService>();
        services.AddTransient<ISubscriptionService, SubscriptionService>();
        services.AddHttpClient<PayPalService>();
        services.AddHttpClient<PayPalMeService>();
        services.AddScoped<InteractService>();

        services.AddSingleton<SubscriptionStatusUpdater>();
        services.AddHostedService(sp => sp.GetRequiredService<SubscriptionStatusUpdater>());

        return services;
    }
}
