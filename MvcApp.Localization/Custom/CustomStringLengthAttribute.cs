using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MvcApp.Localization.Custom
{
    public class CustomStringLengthAttribute : StringLengthAttribute, IClientModelValidator
    {
        public CustomStringLengthAttribute(int minimumLength, int maximumLength, string errorValue) : base(maximumLength)
        {
            MinimumLength = minimumLength;
            ErrorValue = errorValue;
        }

        public string ErrorValue { get; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer)
            {
                var localizedFieldName = localizer[validationContext.DisplayName];
                var localizedValue = localizer[ErrorValue];
                ErrorMessage = string.Format(localizedValue, localizedFieldName, MinimumLength, MaximumLength);
            }
            else
            {
                ErrorMessage = string.Format("The {0} must be at least {1} and at max {2} characters long.", validationContext.DisplayName, MinimumLength, MaximumLength);
            }

            return base.IsValid(value, validationContext);
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-length", GetErrorMessage(context));
            MergeAttribute(context.Attributes, "data-val-length-min", MinimumLength.ToString(CultureInfo.InvariantCulture));
            MergeAttribute(context.Attributes, "data-val-length-max", MaximumLength.ToString(CultureInfo.InvariantCulture));
        }

        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }

        private string GetErrorMessage(ClientModelValidationContext context)
        {
            var localizer = (IStringLocalizer<SharedResource>?)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IStringLocalizer<SharedResource>));
            if (localizer == null)
            {
                throw new InvalidOperationException("IStringLocalizer<SharedResource> service is not available.");
            }
            var displayName = context.ModelMetadata.DisplayName ?? context.ModelMetadata.Name;
            if (displayName == null)
            {
                throw new InvalidOperationException("ModelMetadata does not have a display name or name.");
            }
            var localizedFieldName = localizer[displayName];
            var localizedValue = localizer[ErrorValue];
            return string.Format(localizedValue, localizedFieldName, MinimumLength, MaximumLength);
        }
    }
}
