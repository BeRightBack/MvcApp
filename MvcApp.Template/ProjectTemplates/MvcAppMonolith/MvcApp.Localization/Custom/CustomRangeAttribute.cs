using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Localization.Custom
{
    public class CustomRangeAttribute(int min, int max) : ValidationAttribute
    {
        private readonly int Min = min;
        private readonly int Max = max;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value != null)
            {
                var intValue = (int)value;
                if (intValue < Min || intValue > Max)
                {
                    if (validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer)
                    {
                        var localizedFieldName = localizer[validationContext.DisplayName];
                        ErrorMessage = string.Format(localizer["RangeError", Min, Max], localizedFieldName);
                    }
                    else
                    {
                        ErrorMessage = string.Format("The {0} field value must be between {1} and {2}.", validationContext.DisplayName, Min, Max);
                    }
                }
            }

            return base.IsValid(value, validationContext);
        }

    }
}
