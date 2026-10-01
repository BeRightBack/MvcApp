using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register domain services
            services.AddSingleton<IEmailQueue, EmailQueue>();
            services.AddHostedService<EmailQueueHostedService>();
            services.AddScoped<SmtpEmailSender>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));

            // Module management
            services.AddScoped<IModuleManager, ModuleManager>();

            // UI template system
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddSingleton<ITemplateProfileService, TemplateProfileService>();

            // Site branding
            services.AddScoped<IBrandingService, BrandingService>();

            // Template-driven navigation
            services.AddScoped<INavService, NavService>();

            // Serilog MySQL sink logs (own database, raw SQL - not part of the EF model)
            services.Configure<SystemLogOptions>(configuration.GetSection(SystemLogOptions.SectionName));
            services.AddScoped<ISystemLogService, SystemLogService>();
            services.AddScoped<SystemLogDatabaseInitializer>();
            services.AddHostedService<SystemLogRetentionHostedService>();

            // Other cross-cutting services
            services.AddMemoryCache();

            return services;
        }
    }
}
