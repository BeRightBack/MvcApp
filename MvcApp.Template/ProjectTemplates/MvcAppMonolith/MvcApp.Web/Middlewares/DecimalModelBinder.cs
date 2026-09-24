using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace MvcApp.Web.Middlewares
{
    public class DecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult != ValueProviderResult.None)
            {
                var value = valueProviderResult.FirstValue;
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid decimal value.");
            }
            return Task.CompletedTask;
        }
    }
}
