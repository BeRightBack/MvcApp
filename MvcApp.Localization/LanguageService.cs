using MvcApp.Core;

namespace MvcApp.Localization;

public class LanguageService : ILanguageService
{
    private readonly LocalizationDbContext _context;
    private readonly LocalizationCache _cache;

    public LanguageService(LocalizationDbContext context, LocalizationCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public IEnumerable<Language> GetLanguages()
    {
        if (_cache.TryGetLanguages(out var cached))
        {
            return cached;
        }

        var languages = _context.Languages.ToList();
        _cache.SetLanguages(languages);
        return languages;
    }

    public Language GetLanguageByCulture(string culture)
    {
        if (_cache.TryGetLanguageByCulture(culture, out var cached) && cached != null)
        {
            return cached;
        }

        var normalized = culture.Trim().ToLowerInvariant();
        var language = _context.Languages.FirstOrDefault(x => x.Culture != null && x.Culture == normalized);

        // Repopulate both caches so the next lookup is free.
        _cache.SetLanguages(_context.Languages.ToList());
        return language!;
    }
}
