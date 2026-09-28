using Microsoft.Extensions.Caching.Memory;

namespace MvcApp.Infrastructure;

/// <summary>
/// Process-wide cache of the SystemSettings table.
///
/// The navigation calls ISettingsService once per nav item (and once per nav surface) to
/// resolve module flags, so a single page used to issue around 37 settings queries - each one
/// a full round-trip to a remote database. The table is tiny, so the whole thing is loaded
/// once and served from memory.
///
/// Writers must call <see cref="Invalidate"/>: ISettingsService.SetAsync, the admin
/// SystemSettings controller and the settings seeder all do. A short sliding expiration is
/// kept as a safety net for any writer that slips through.
/// </summary>
public sealed class SettingsCache(IMemoryCache memoryCache)
{
    private const string SnapshotKey = "SystemSettingsSnapshot";

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public bool TryGetSnapshot(out Dictionary<string, string> snapshot) =>
        memoryCache.TryGetValue(SnapshotKey, out snapshot!);

    public void SetSnapshot(Dictionary<string, string> snapshot) =>
        memoryCache.Set(SnapshotKey, snapshot, new MemoryCacheEntryOptions
        {
            SlidingExpiration = Lifetime,
            AbsoluteExpirationRelativeToNow = Lifetime,
            Priority = CacheItemPriority.NeverRemove
        });

    public void Invalidate() => memoryCache.Remove(SnapshotKey);
}
