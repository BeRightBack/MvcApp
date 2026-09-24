using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Module.Ads.Entities;
using MvcApp.Module.Ads.Services;

namespace MvcApp.Module.Ads;

public static class ServiceRegistration
{
    public static IServiceCollection AddAdsModule(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);

        // Repositories (shared UserDbContext via generic IRepository<T>)
        services.AddScoped<IRepository<AdZone>, Repository<AdZone>>();
        services.AddScoped<IRepository<AdBanner>, Repository<AdBanner>>();
        services.AddScoped<IRepository<AdPlacement>, Repository<AdPlacement>>();
        services.AddScoped<IRepository<AdImpression>, Repository<AdImpression>>();
        services.AddScoped<IRepository<AdClick>, Repository<AdClick>>();

        // Services
        services.AddScoped<IAdZoneService, AdZoneService>();
        services.AddScoped<IAdBannerService, AdBannerService>();
        services.AddScoped<IAdPlacementService, AdPlacementService>();
        services.AddScoped<IAdTrackingService, AdTrackingService>();
        services.AddScoped<IAdRenderer, AdRenderer>();
        services.AddScoped<IAdMediaService, AdMediaService>();

        // HttpContextAccessor + cache for tracking/rendering
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        return services;
    }
}
