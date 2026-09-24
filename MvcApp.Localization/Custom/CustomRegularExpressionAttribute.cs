using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Localization.Custom
{
    public class CustomRegularExpressionAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value != null)
            {
                var stringValue = value.ToString();
                if (stringValue != null && !new RegularExpressionAttribute("^[a-zA-Z0-9]*$").IsValid(stringValue))
                {
                    if (validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer)
                    {
                        var localizedFieldName = localizer[validationContext.DisplayName];
                        ErrorMessage = string.Format(localizer["RegularExpressionError"], localizedFieldName);
                    }
                    else
                    {
                        ErrorMessage = string.Format("The {0} field is not a valid regular expression.", validationContext.DisplayName);
                    }

                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
