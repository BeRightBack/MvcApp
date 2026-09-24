using Microsoft.Extensions.Localization;

namespace MvcApp.Localization
{
    public static class LocalizationContext
    {
        public static IStringLocalizer<SharedResource>? Localizer { get; set; }
    }
}
