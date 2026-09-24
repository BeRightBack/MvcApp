using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;

namespace MvcApp.Common.Filters;

[AttributeUsage(AttributeTargets.Class)]
public class ModuleEnabledFilter(string moduleName) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var manager = context.HttpContext.RequestServices.GetRequiredService<IModuleManager>();
        if (!await manager.IsModuleEnabledAsync(moduleName))
        {
            context.Result = new NotFoundResult();
        }
    }
}