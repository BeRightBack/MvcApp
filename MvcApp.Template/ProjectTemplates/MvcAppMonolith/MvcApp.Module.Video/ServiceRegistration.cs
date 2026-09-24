using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Video;

public static class ServiceRegistration
{
    public static IServiceCollection AddVideo(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);
        services.AddScoped<IRepository<VideoRoom>, Repository<VideoRoom>>();
        services.AddScoped<IRepository<VideoRoomMessage>, Repository<VideoRoomMessage>>();
        return services;
    }
}
