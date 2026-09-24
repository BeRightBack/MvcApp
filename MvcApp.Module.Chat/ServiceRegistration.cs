using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Chat;

public static class ServiceRegistration
{
    public static IServiceCollection AddChat(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);
        services.AddScoped<IRepository<ChatRoom>, Repository<ChatRoom>>();
        services.AddScoped<IRepository<ChatRoomMessage>, Repository<ChatRoomMessage>>();
        return services;
    }
}
