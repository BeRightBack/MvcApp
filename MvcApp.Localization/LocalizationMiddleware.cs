using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace MvcApp.Localization
{
    public class LocalizationMiddleware
    {
        private readonly RequestDelegate _next;

        public LocalizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IStringLocalizer<SharedResource> localizer)
        {
            LocalizationContext.Localizer = localizer;
            await _next(context);
        }
    }
}
