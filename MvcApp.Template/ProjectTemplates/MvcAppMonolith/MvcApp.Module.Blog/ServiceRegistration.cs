using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Blog;

public static class ServiceRegistration
{
    public static IServiceCollection AddBlog(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);
        services.AddScoped<IRepository<BlogPost>, Repository<BlogPost>>();
        services.AddScoped<IRepository<BlogCategory>, Repository<BlogCategory>>();
        services.AddScoped<IRepository<BlogTag>, Repository<BlogTag>>();
        services.AddScoped<IRepository<BlogComment>, Repository<BlogComment>>();
        return services;
    }
}
