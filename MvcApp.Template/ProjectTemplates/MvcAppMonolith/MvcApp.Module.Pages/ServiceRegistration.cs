using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Core;
using MvcApp.Infrastructure;
using MvcApp.Module.Pages.Services;

namespace MvcApp.Module.Pages;

public static class ServiceRegistration
{
    public static IServiceCollection AddPages(this IServiceCollection services)
    {
        ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);
        services.AddScoped<IRepository<ContentPage>, Repository<ContentPage>>();
        services.AddScoped<IRepository<ContentPageSnippet>, Repository<ContentPageSnippet>>();
        services.AddSingleton<IPageRenderer, PageRenderer>();
        services.AddSingleton<BlazorComponentRegistry>();
        services.AddScoped<IPageBlazorRenderer, PageBlazorRenderer>();
        return services;
    }
}
