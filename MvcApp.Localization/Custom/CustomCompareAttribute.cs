using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Localization.Custom
{
    public class CustomCompareAttribute : ValidationAttribute
    {
        public string ComparisonProperty { get; }

        public CustomCompareAttribute(string comparisonProperty)
        {
            ComparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var comparisonProperty = validationContext.ObjectType.GetProperty(ComparisonProperty);
            if (comparisonProperty == null)
            {
                return new ValidationResult($"Unknown property: {ComparisonProperty}");
            }

            var comparisonValue = comparisonProperty.GetValue(validationContext.ObjectInstance);

            if (value == null || comparisonValue == null)
            {
                if (validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer)
                {
                    var nullValue = localizer["The value cannot be null."];
                    ErrorMessage = string.Format(nullValue);
                }
                else
                {
                    ErrorMessage = "The value cannot be null.";
                }

                return new ValidationResult(ErrorMessage);
            }

            if (!value.Equals(comparisonValue))
            {
                if (validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer)
                {
                    var localizedFieldName = localizer[validationContext.DisplayName];
                    var localizedComparisonFieldName = localizer[ComparisonProperty];
                    ErrorMessage = string.Format(localizer["CompareError"], localizedFieldName, localizedComparisonFieldName);
                }
                else
                {
                    ErrorMessage = $"The {validationContext.DisplayName} field does not match the {ComparisonProperty} field.";
                }

                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
