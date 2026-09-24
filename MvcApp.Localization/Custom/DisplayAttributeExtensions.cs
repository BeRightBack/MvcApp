using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;


namespace MvcApp.Localization.Custom
{

    public static class DisplayAttributeExtensions
    {
        public static string GetLocalizedName(this DisplayAttribute attribute, IStringLocalizer localizer)
        {
            var key = attribute.Name ?? "DefaultName";
            return localizer[key];
        }
        public static string GetLocalizedName<T>(this IStringLocalizer localizer, string propertyName)
        {
            var displayAttribute = typeof(T).GetProperty(propertyName)?.GetCustomAttributes(typeof(DisplayAttribute), true).FirstOrDefault() as DisplayAttribute;
            return displayAttribute?.Name == null ? propertyName : localizer[displayAttribute.Name];
        }
    }
}
