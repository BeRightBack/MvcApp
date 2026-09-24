using System.Net;
using System.Security.Claims;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Middlewares;

public class BannedUserMiddleware(RequestDelegate next)
{
    private const string BanPageTemplate = """
        <!DOCTYPE html>
        <html><head><title>Account Suspended</title>
        <style>body{font-family:sans-serif;display:flex;justify-content:center;align-items:center;min-height:100vh;margin:0;background:#f8fafc;color:#1e293b}
        .card{text-align:center;padding:3rem;background:#fff;border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,.08);max-width:520px}
        h1{font-size:2rem;margin-bottom:.5rem}p{color:#64748b;margin:.5rem 0}code{background:#f1f5f9;padding:2px 6px;border-radius:4px}
        button{margin-top:1rem;padding:.5rem 1.25rem;border:0;border-radius:8px;background:#dc2626;color:#fff;cursor:pointer}</style>
        </head><body><div class='card'><h1>Account Suspended</h1>
        <p>Your account has been suspended __EXPIRES__.</p>
        <p>Reason: <code>__REASON__</code></p>
        <form method='post' action='/Account/Logout'><button type='submit'>Sign out</button></form>
        </div></body></html>
        """;

    public async Task InvokeAsync(HttpContext context, IBanService banService)
    {
        // Admins keep full access even if a ban record exists.
        if (context.User?.Identity?.IsAuthenticated == true &&
            context.User.IsInRole("Admin"))
        {
            await next(context);
            return;
        }

        // Static assets and account flows must always be reachable so a suspended
        // user can still sign out and see readable error pages.
        if (context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/lib") ||
            context.Request.Path.StartsWithSegments("/images") ||
            context.Request.Path.StartsWithSegments("/Account"))
        {
            await next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var ban = await banService.GetActiveBanAsync(userId);
                if (ban != null)
                {
                    var expires = ban.BannedUntil.HasValue
                        ? $"until {ban.BannedUntil.Value:yyyy-MM-dd HH:mm} UTC"
                        : "indefinitely";

                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "text/html";
                    var html = BanPageTemplate
                        .Replace("__EXPIRES__", expires)
                        .Replace("__REASON__", WebUtility.HtmlEncode(ban.Reason));
                    await context.Response.WriteAsync(html);
                    return;
                }
            }
        }

        await next(context);
    }
}
