using MvcApp.Core;

namespace MvcApp.Localization;

public interface ILanguageService
{
    IEnumerable<Language> GetLanguages();
    Language GetLanguageByCulture(string culture);
}
