using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Infrastructure.Seeding;
using MvcApp.Infrastructure.Seeding.Packs;
using MvcApp.Module.Video.Seeding;

namespace MvcApp.Module.Video;

public static class ServiceRegistration
{
    public static IServiceCollection AddVideo(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);
        services.AddScoped<IRepository<VideoRoom>, Repository<VideoRoom>>();
        services.AddScoped<IRepository<VideoRoomMessage>, Repository<VideoRoomMessage>>();

        // VideoRoom lives in this module, so the pack that owns the rooms does too.
        services.AddScoped<ISeedPack, VideoPack>();

        return services;
    }
}