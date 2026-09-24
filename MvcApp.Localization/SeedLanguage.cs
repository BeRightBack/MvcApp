using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using MvcApp.Core;
using System.Globalization;

namespace MvcApp.Localization
{
    public class SeedLanguage(LocalizationDbContext context, IOptions<RequestLocalizationOptions> localizationOptions)
    {
        private readonly IList<CultureInfo> _supportedCultures = localizationOptions.Value.SupportedCultures!;

        public async Task EnsureSeedLanguageAsync()
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.Languages.Any())
            {
                return;
            }

            if (_supportedCultures == null)
            {
                throw new ArgumentNullException(nameof(_supportedCultures));
            }

            foreach (var culture in _supportedCultures)
            {
                var language = new Language
                {
                    Id = new(),
                    Name = culture.DisplayName,
                    Culture = culture.ToString()
                };

                context.Languages.Add(language);
            }

            await context.SaveChangesAsync();
        }
    }
}