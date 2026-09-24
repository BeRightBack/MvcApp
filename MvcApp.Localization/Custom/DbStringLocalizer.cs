using DeepL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MvcApp.Core;
using System.Globalization;
namespace MvcApp.Localization.Custom
{
    public class DbStringLocalizer<T> : IStringLocalizer<T>
    {
        private readonly LocalizationDbContext _context;
        private readonly IList<CultureInfo> _supportedCultures;
        private readonly Translator _translator;
        private readonly IConfiguration _configuration;

        public DbStringLocalizer(LocalizationDbContext context,
            IOptions<RequestLocalizationOptions> localizationOptions,
            Translator translator,
            IConfiguration configuration)
        {
            _context = context;
            _supportedCultures = localizationOptions.Value.SupportedCultures!;
            _translator = translator;
            _configuration = configuration;
        }

        public static HttpContext HttpContext => new HttpContextAccessor().HttpContext!;

        public string SourceLang => _configuration["DeepLConfig:SourceLang"]!;

        public LocalizedString this[string name]
        {
            get
            {
                // Resolve Services
                ILanguageService _languageService = (ILanguageService)HttpContext.RequestServices.GetService(typeof(ILanguageService))!;
                ILocalizationService _localizationService = (ILocalizationService)HttpContext.RequestServices.GetService(typeof(ILocalizationService))!;

                var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;

                var language = _languageService?.GetLanguageByCulture(currentCulture);
                language ??= _languageService?.GetLanguages().FirstOrDefault();

                if (language != null)
                {
                    var stringResource = _localizationService?.GetStringResource(name, language.Id);

                    if (stringResource != null && !string.IsNullOrEmpty(stringResource.Value))
                    {
                        return new LocalizedString(name, stringResource.Value, false);
                    }

                    if (stringResource == null || string.IsNullOrEmpty(stringResource.Value))
                    {
                        foreach (var item in _supportedCultures)
                        {
                            var languageId = _languageService?.GetLanguageByCulture(item.Name)?.Id;
                            var targetLanguage = GetTargetLanguage(languageId!.Value);



                            var translatedValue = TranslateText(name, targetLanguage!).Result;
                            var _resource = new StringResource
                            {
                                LanguageId = languageId!,
                                Name = name,
                                Value = translatedValue
                            };
                            _localizationService?.AddOrUpdateStringResource(_resource);
                        }

                        return new LocalizedString(name, name, true);
                    }
                }
                return new LocalizedString(name, name, true);
            }
        }

        private string? GetTargetLanguage(int culture)
        {
            return culture switch
            {
                1 => _configuration?["DeepLConfig:TargetLangEn"]!,
                4 => _configuration?["DeepLConfig:TargetLangIt"]!,
                2 => _configuration?["DeepLConfig:TargetLangFr"]!,
                3 => _configuration?["DeepLConfig:TargetLangEs"]!,
                6 => _configuration?["DeepLConfig:TargetLangDe"]!,
                5 => _configuration?["DeepLConfig:TargetLangPt"]!,
                _ => null,
            };
        }

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _context.StringResources.Select(e => new LocalizedString(e.Name!, e.Value!));

        public IStringLocalizer WithCulture(CultureInfo culture) => this;

        private async Task<string> TranslateText(string text, string targetLanguage)
        {
            const int maxRetries = 5; // Maximum number of retries
            const int initialDelay = 1000; // Initial delay in milliseconds (1 second)
            int retryCount = 0;

            while (true)
            {
                try
                {
                    var result = await _translator.TranslateTextAsync(text, SourceLang, targetLanguage);
                    return result.Text;
                }
                catch (TooManyRequestsException)
                {
                    retryCount++;
                    if (retryCount > maxRetries)
                    {
                        // Log the error and return the original text as a fallback
                        Console.WriteLine("Max retries reached. Returning original text.");
                        return text;
                    }

                    // Exponential backoff
                    int delay = initialDelay * (int)Math.Pow(2, retryCount - 1);
                    Console.WriteLine($"Too many requests. Retrying in {delay}ms...");
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    // Log other exceptions and return the original text as a fallback
                    Console.WriteLine($"Translation failed: {ex.Message}");
                    return text;
                }
            }
        }

    }
}

