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
        private readonly BackgroundTranslationService _backgroundTranslation;

        public DbStringLocalizer(LocalizationDbContext context,
            IOptions<RequestLocalizationOptions> localizationOptions,
            Translator translator,
            IConfiguration configuration,
            BackgroundTranslationService backgroundTranslation)
        {
            _context = context;
            _supportedCultures = localizationOptions.Value.SupportedCultures!;
            _translator = translator;
            _configuration = configuration;
            _backgroundTranslation = backgroundTranslation;
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

                    // Missing key: hand it to the background translator and return the same value
                    // the inline path returned. The page never waits for DeepL; the translation is
                    // stored and picked up on a later request.
                    _backgroundTranslation.Enqueue(name);
                }

                return new LocalizedString(name, name, true);
            }
        }

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _context.StringResources.Select(e => new LocalizedString(e.Name!, e.Value!));

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
