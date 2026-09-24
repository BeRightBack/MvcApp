using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MvcApp.Localization.Custom
{
    public static class ValidationAttributeExtensions
    {
        public static string GetLocalizedErrorMessage(this ValidationAttribute attribute, IStringLocalizer localizer, ValidationContext validationContext)
        {
            var displayName = GetDisplayName(validationContext);
            return attribute switch
            {
                StringLengthAttribute stringLengthAttribute => localizer["StringLengthErrorMessage", displayName, stringLengthAttribute.MinimumLength, stringLengthAttribute.MaximumLength],
                _ => attribute.ErrorMessage ?? "Validation error"
            };
        }

        public static string GetDisplayName(ValidationContext validationContext)
        {
            var displayAttribute = validationContext.ObjectType.GetProperty(validationContext.MemberName!)
                ?.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.GetName() ?? validationContext.MemberName!;
        }
    }
}
