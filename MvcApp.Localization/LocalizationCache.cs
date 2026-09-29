using Microsoft.Extensions.Caching.Memory;
using MvcApp.Core;

namespace MvcApp.Localization;

/// <summary>
/// Process-wide cache of the localization tables.
///
/// Every <c>@Localizer["..."]</c> resolves the current language and then the string
/// itself, so an un-cached localizer issues two queries per lookup - that was the
/// 15 Languages + 15 StringResources round-trips on the home page. Both tables are
/// tiny (5 languages, ~212 strings each), so they are loaded once and served from
/// memory. Writes go through <see cref="AddOrUpdateStringResource"/>, which updates
/// the cache immediately so a just-translated string is visible on the next request.
/// </summary>
public sealed class LocalizationCache(IMemoryCache memoryCache)
{
    private const string LanguagesKey = "LocalizationLanguages";
    private const string LanguagesByCultureKey = "LocalizationLanguagesByCulture";
    private const string ResourcesKey = "LocalizationStringResources";

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly object _sync = new();

    public bool TryGetLanguages(out List<Language> languages) =>
        memoryCache.TryGetValue(LanguagesKey, out languages!);

    public void SetLanguages(List<Language> languages)
    {
        memoryCache.Set(LanguagesKey, languages, EntryOptions());
        memoryCache.Set(LanguagesByCultureKey,
            languages.Where(l => !string.IsNullOrWhiteSpace(l.Culture))
                     .ToDictionary(l => l.Culture!.Trim().ToLowerInvariant(), l => l, StringComparer.OrdinalIgnoreCase),
            EntryOptions());
    }

    public bool TryGetLanguageByCulture(string culture, out Language? language)
    {
        language = null;
        // TryGetValue is annotated MaybeNullWhen(false), which the negated form does not narrow,
        // so the value is checked for null explicitly rather than assumed present.
        if (memoryCache.TryGetValue(LanguagesByCultureKey, out Dictionary<string, Language>? map) && map is not null)
        {
            return map.TryGetValue(culture.Trim().ToLowerInvariant(), out language);
        }
        return false;
    }

    public bool TryGetResources(int languageId, out Dictionary<string, string> resources) =>
        memoryCache.TryGetValue(ResourceKey(languageId), out resources!);

    public void SetResources(int languageId, Dictionary<string, string> resources) =>
        memoryCache.Set(ResourceKey(languageId), resources, EntryOptions());

    public void AddOrUpdateStringResource(string key, int languageId, string value)
    {
        lock (_sync)
        {
            if (memoryCache.TryGetValue(ResourceKey(languageId), out Dictionary<string, string>? resources) && resources is not null)
            {
                resources[key] = value;
                return;
            }

            // Not loaded yet: drop the language list so the next reader re-reads.
            memoryCache.Remove(LanguagesKey);
            memoryCache.Remove(LanguagesByCultureKey);
        }
    }

    public void Invalidate()
    {
        memoryCache.Remove(LanguagesKey);
        memoryCache.Remove(LanguagesByCultureKey);
        if (TryGetLanguages(out var languages))
        {
            foreach (var language in languages)
            {
                memoryCache.Remove(ResourceKey(language.Id));
            }
        }
    }

    private static string ResourceKey(int languageId) => $"{ResourcesKey}:{languageId}";

    private static MemoryCacheEntryOptions EntryOptions() => new()
    {
        SlidingExpiration = Lifetime,
        AbsoluteExpirationRelativeToNow = Lifetime,
        Priority = CacheItemPriority.NeverRemove
    };
}
