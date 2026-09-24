using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Localization.Custom
{
    public class CustomUrlAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value != null)
            {
                var stringValue = value.ToString();
                if (stringValue != null && !new UrlAttribute().IsValid(stringValue))
                {
                    if (validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer)
                    {
                        ErrorMessage = localizer["UrlError"];
                    }
                    else
                    {
                        ErrorMessage = "The field is not a valid URL."; // Default error message
                    }
                }

            }

            return base.IsValid(value, validationContext);
        }
    }
}
