using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace MvcApp.Localization
{
    /// <summary>
    /// Provides the string localizer to code that cannot be handed one by injection, such as a
    /// <c>DisplayNameAttribute</c> subclass.
    /// </summary>
    /// <remarks>
    /// This resolves the localizer from the CURRENT request's service provider rather than
    /// parking an instance on a static field. Two reasons:
    /// <list type="bullet">
    /// <item>A static holding a request-scoped service is shared across concurrent requests, and
    /// it outlives the scope it came from.</item>
    /// <item>Outside a request - a hosted service, a background job, a unit test - there is no
    /// <c>HttpContext</c>, and the old code dereferenced a null one.</item>
    /// </list>
    /// When there is no current request this returns null and callers fall back to the
    /// untranslated text, which is the correct result rather than an exception.
    /// </remarks>
    public static class LocalizationContext
    {
        // HttpContextAccessor reads a static AsyncLocal internally, so a single instance here
        // sees the current request on the current execution context.
        private static readonly HttpContextAccessor Accessor = new();

        /// <summary>
        /// The localizer for the current request, or null when there is no current request.
        /// </summary>
        public static IStringLocalizer<SharedResource>? Current
        {
            get
            {
                var services = Accessor.HttpContext?.RequestServices;
                return services?.GetService<IStringLocalizer<SharedResource>>();
            }
        }

        /// <summary>
        /// Translates <paramref name="name"/> for the current request, returning it unchanged when
        /// no request is in flight.
        /// </summary>
        public static string Translate(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return Current?[name].Value ?? name;
        }
    }
}
