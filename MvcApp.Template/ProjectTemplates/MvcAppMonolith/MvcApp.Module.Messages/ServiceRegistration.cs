using Microsoft.Extensions.DependencyInjection;

namespace MvcApp.Module.Messages;

public static class ServiceRegistration
{
    public static IServiceCollection AddMessages(this IServiceCollection services)
    {
        return services;
    }
}