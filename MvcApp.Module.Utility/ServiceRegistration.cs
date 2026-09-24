using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Module.Utility.Services;

namespace MvcApp.Module.Utility
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddUtility(this IServiceCollection services)
        {
            ModuleConfigurationRegistry.AddEntityConfigurationAssembly(typeof(ServiceRegistration).Assembly);

            // Shared repository infrastructure (UserDbContext via IRepository<T>)
            services.AddScoped<IRepository<Project>, Repository<Project>>();
            services.AddScoped<IRepository<ToDoTask>, Repository<ToDoTask>>();
            services.AddScoped<IRepository<ToDoSubTask>, Repository<ToDoSubTask>>();

            // Domain services
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IToDoTaskService, ToDoTaskService>();
            services.AddScoped<IToDoSubTaskService, ToDoSubTaskService>();
            return services;
        }
    }
}
