using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Middlewares;

public class SiteUnderMaintenanceMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISettingsService settings)
    {
        // Skip for admins and static files
        if (context.User?.Identity?.IsAuthenticated == true &&
            context.User.IsInRole("Admin"))
        {
            await next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/lib") ||
            context.Request.Path.StartsWithSegments("/images"))
        {
            await next(context);
            return;
        }

        var underMaintenance = await settings.GetAsync<bool>("SiteUnderMaintenance") ?? false;
        if (underMaintenance)
        {
            context.Response.StatusCode = 503;
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("""
                <!DOCTYPE html>
                <html><head><title>Under Maintenance</title>
                <style>body{font-family:sans-serif;display:flex;justify-content:center;align-items:center;min-height:100vh;margin:0;background:#f8fafc;color:#1e293b}
                .card{text-align:center;padding:3rem;background:#fff;border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,.08)}
                h1{font-size:2rem;margin-bottom:.5rem}p{color:#64748b}</style>
                </head><body><div class='card'><h1>Under Maintenance</h1>
                <p>We're performing scheduled maintenance. Please check back shortly.</p></div></body></html>
                """);
            return;
        }

        await next(context);
    }
}
