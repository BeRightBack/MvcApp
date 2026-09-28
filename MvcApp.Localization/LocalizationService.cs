using MvcApp.Core;

namespace MvcApp.Localization;

public class LocalizationService : ILocalizationService
{
    private readonly LocalizationDbContext _context;
    private readonly LocalizationCache _cache;

    public LocalizationService(LocalizationDbContext context, LocalizationCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public StringResource GetStringResource(string resourceKey, int languageId)
    {
        var key = resourceKey.Trim();

        if (_cache.TryGetResources(languageId, out var resources))
        {
            return resources.TryGetValue(key, out var value)
                ? new StringResource { Name = key, LanguageId = languageId, Value = value }
                : null!;
        }

        var row = _context.StringResources.FirstOrDefault(x =>
            x.Name == key && x.LanguageId == languageId);

        // Warm the cache for this language so the next lookup is free. The table can hold
        // duplicate names for one language, and the query above returns the FIRST match, so
        // the first occurrence wins here too (ToDictionary would throw on the duplicates).
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var x in _context.StringResources.Where(x => x.LanguageId == languageId))
        {
            var name = x.Name!.Trim();
            if (!map.ContainsKey(name))
            {
                map[name] = x.Value ?? string.Empty;
            }
        }

        _cache.SetResources(languageId, map);

        return row!;
    }

    public void AddOrUpdateStringResource(StringResource resource)
    {
        lock (_context)
        {
            var existing = _context.StringResources
                .FirstOrDefault(x => x.Name == resource.Name && x.LanguageId == resource.LanguageId);

            if (existing != null)
            {
                existing.Value = resource.Value;
                _context.Update(existing);
            }
            else
            {
                _context.Add(resource);
            }

            _context.SaveChanges();
        }

        if (!string.IsNullOrEmpty(resource.Value))
        {
            _cache.AddOrUpdateStringResource(resource.Name!.Trim(), resource.LanguageId!.Value, resource.Value!);
        }
    }
}
