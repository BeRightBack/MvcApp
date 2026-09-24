using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Store;

public static class ServiceRegistration
{
    public static IServiceCollection AddStore(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);
        services.AddScoped<IRepository<Product>, Repository<Product>>();
        services.AddScoped<IRepository<ProductCategory>, Repository<ProductCategory>>();
        services.AddScoped<IRepository<CartItem>, Repository<CartItem>>();
        services.AddScoped<IRepository<Order>, Repository<Order>>();
        services.AddScoped<IRepository<OrderItem>, Repository<OrderItem>>();
        services.AddHttpClient<Services.StorePayPalService>();
        return services;
    }
}
