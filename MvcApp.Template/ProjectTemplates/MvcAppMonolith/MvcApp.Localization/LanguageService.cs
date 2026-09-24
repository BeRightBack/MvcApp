using MvcApp.Core;

namespace MvcApp.Localization;

public class LanguageService : ILanguageService
{
    private readonly LocalizationDbContext _context;

    public LanguageService(LocalizationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Language> GetLanguages()
    {
        return _context.Languages.ToList();
    }

    public Language GetLanguageByCulture(string culture)
    {
        //return _context.Languages.FirstOrDefault(x =>
        //    x.Culture!.Trim().ToLower() == culture.Trim().ToLower())!;

        var normalized = culture.Trim().ToLowerInvariant(); 
        return _context.Languages.FirstOrDefault(x => x.Culture != null && x.Culture == normalized)!;
    }
}
