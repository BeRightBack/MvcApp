using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Localization.Custom
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class CustomRequiredAttribute : ValidationAttribute, IClientModelValidator
    {
        public string RequiredValue { get; }

        public CustomRequiredAttribute(string requiredValue)
        {
            RequiredValue = requiredValue;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;
            if (localizer != null)
            {
                var localizedValue = localizer[RequiredValue];
                ErrorMessage = string.Format(localizedValue, validationContext.DisplayName);
            }
            else
            {
                ErrorMessage = RequiredValue;
            }

            if (value == null || string.IsNullOrWhiteSpace(value!.ToString()))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-required", GetErrorMessage(context));
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
            var localizedValue = localizer[RequiredValue];
            return string.Format(localizedValue, localizedFieldName);
        }
    }
}
