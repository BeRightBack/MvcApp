namespace MvcApp.Web.Middlewares;

/// <summary>
/// Emits the baseline security response headers that no individual page should have to remember,
/// and drops the Kestrel server banner.
///
/// Deliberately NOT included yet:
///  - Content-Security-Policy: the views still contain inline event handlers and inline script
///    blocks, so a strict policy would break rendering until that markup is externalised. It is a
///    follow-up, not an oversight.
///  - Permissions-Policy restrictions on camera/microphone: the video module uses WebRTC, so
///    narrowing those directives would break calling.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            headers.Remove("Server");

            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}
