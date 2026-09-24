using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using MvcApp.Core;

namespace MvcApp.Localization.Custom;

public class CustomLocView
{

    [RazorInject]
    public ILanguageService? LanguageService { get; set; }

    [RazorInject]
    public ILocalizationService? LocalizationService { get; set; }

    public delegate HtmlString Localizer(string resourceKey, params object[] args);
    private Localizer? _localizer;

    public Localizer Localize
    {
        get
        {
            if (_localizer == null)
            {
                var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;

                var language = LanguageService!.GetLanguageByCulture(currentCulture);
                if (language != null)
                {
                    _localizer = (resourceKey, args) =>
                    {
                        var stringResource = LocalizationService!.GetStringResource(resourceKey, language.Id);

                        if (stringResource == null || string.IsNullOrEmpty(stringResource.Value))
                        {
                            var _resource_fr = new StringResource
                            {
                                LanguageId = 1,
                                Name = resourceKey,
                                Value = resourceKey + "-fr"
                            };
                            var _resource_en = new StringResource
                            {
                                LanguageId = 2,
                                Name = resourceKey,
                                Value = resourceKey + "-en"
                            };
                            var _resource_es = new StringResource
                            {
                                LanguageId = 3,
                                Name = resourceKey,
                                Value = resourceKey + "-es"
                            };
                            LocalizationService.AddOrUpdateStringResource(_resource_fr);
                            LocalizationService.AddOrUpdateStringResource(_resource_en);
                            LocalizationService.AddOrUpdateStringResource(_resource_es);
                            return new HtmlString(resourceKey);
                        }

                        return new HtmlString(args == null || args.Length == 0
                            ? stringResource.Value
                            : string.Format(stringResource.Value, args));
                    };
                }
            }
            return _localizer!;
        }
    }
}
