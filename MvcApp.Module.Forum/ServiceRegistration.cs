using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Forum;

public static class ServiceRegistration
{
    public static IServiceCollection AddForum(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);
        services.AddScoped<IRepository<ForumCategory>, Repository<ForumCategory>>();
        services.AddScoped<IRepository<MvcApp.Core.Forum>, Repository<MvcApp.Core.Forum>>();
        services.AddScoped<IRepository<ForumThread>, Repository<ForumThread>>();
        services.AddScoped<IRepository<ForumPost>, Repository<ForumPost>>();
        return services;
    }
}
