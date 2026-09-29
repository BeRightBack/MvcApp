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

        public async Task InvokeAsync(HttpContext context)
        {
            // Nothing to do per request. The localizer resolves the current culture itself at
            // lookup time, and anything that needs one outside a view (a DisplayName attribute,
            // say) reaches it through LocalizationContext.Current, which reads the current
            // request. Previously the middleware parked a request-scoped service on a static
            // field, so one request could observe another request's instance, and the field
            // outlived the scope it came from.
            await _next(context);
        }
    }
}
