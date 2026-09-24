using DeepL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MvcApp.Core;
using System.Globalization;

namespace MvcApp.Localization.Custom
{
    public class LocalizationHelper
    {
        private readonly Translator _translator;
        private readonly IConfiguration _configuration;
        private const string SourceLang = "EN"; // Example source language

        public LocalizationHelper(Translator translator, IConfiguration configuration)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<LocalizedString> GetLocalizedStringAsync(
            IStringLocalizer<SharedResource> localizer,
            ILocalizationService localizationService,
            ILanguageService languageService,
            IEnumerable<CultureInfo> supportedCultures,
            string name,
            int languageId)
        {
            var stringResource = localizationService?.GetStringResource(name, languageId);

            if (stringResource != null && !string.IsNullOrEmpty(stringResource.Value))
            {
                return new LocalizedString(name, stringResource.Value, false);
            }

            if (stringResource == null || string.IsNullOrEmpty(stringResource.Value))
            {
                foreach (var item in supportedCultures)
                {
                    var cultureLanguageId = languageService?.GetLanguageByCulture(item.Name)?.Id;
                    if (cultureLanguageId == null)
                    {
                        continue;
                    }

                    var targetLanguage = GetTargetLanguage(cultureLanguageId.Value);
                    if (string.IsNullOrEmpty(targetLanguage))
                    {
                        continue;
                    }

                    var translatedValue = await TranslateText(name, targetLanguage);
                    if (string.IsNullOrEmpty(translatedValue))
                    {
                        continue;
                    }

                    var resource = new StringResource
                    {
                        LanguageId = cultureLanguageId.Value,
                        Name = name,
                        Value = translatedValue
                    };
                    localizationService?.AddOrUpdateStringResource(resource);
                }

                return new LocalizedString(name, name, true);
            }

            return new LocalizedString(name, name, true);
        }

        private string? GetTargetLanguage(int languageId)
        {
            return languageId switch
            {
                1 => _configuration["DeepLConfig:TargetLangEn"],
                4 => _configuration["DeepLConfig:TargetLangIt"],
                2 => _configuration["DeepLConfig:TargetLangFr"],
                3 => _configuration["DeepLConfig:TargetLangEs"],
                6 => _configuration["DeepLConfig:TargetLangDe"],
                5 => _configuration["DeepLConfig:TargetLangPt"],
                _ => null,
            };
        }

        private async Task<string?> TranslateText(string text, string targetLanguage)
        {
            var result = await _translator.TranslateTextAsync(text, SourceLang, targetLanguage);
            return result?.Text;
        }
    }
}


