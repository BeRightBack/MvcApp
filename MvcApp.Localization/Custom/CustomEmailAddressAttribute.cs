using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Localization.Custom
{
    public class CustomEmailAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value != null)
            {
                var stringValue = value.ToString();

                if (!string.IsNullOrEmpty(stringValue) && !new EmailAddressAttribute().IsValid(stringValue))
                {
                    if (validationContext?.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer)
                    {
                        var localizedFieldName = localizer[validationContext.DisplayName];
                        var emailError1 = localizer["The"];
                        var emailError2 = localizer["field is not a valid e-mail address."];
                        ErrorMessage = $"{emailError1} {localizedFieldName} {emailError2}";
                    }
                    else if (validationContext != null)
                    {
                        ErrorMessage = $"The {validationContext.DisplayName} field is not a valid e-mail address.";
                    }

                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}
