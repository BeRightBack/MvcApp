using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class SettingsService(UserDbContext db, SettingsCache cache) : ISettingsService
{
    public async Task<string?> GetAsync(string key)
    {
        var snapshot = await GetSnapshotAsync();
        return snapshot.TryGetValue(key, out var value) ? value : null;
    }

    private async Task<Dictionary<string, string>> GetSnapshotAsync()
    {
        if (cache.TryGetSnapshot(out var cached))
        {
            return cached;
        }

        var rows = await db.SystemSettings.AsNoTracking().ToListAsync();
        var snapshot = new Dictionary<string, string>(rows.Count, StringComparer.Ordinal);
        foreach (var row in rows)
        {
            snapshot[row.Key] = row.Value ?? string.Empty;
        }

        cache.SetSnapshot(snapshot);
        return snapshot;
    }

    public async Task<T?> GetAsync<T>(string key) where T : struct
    {
        var value = await GetAsync(key);
        if (value == null) return null;
        if (typeof(T) == typeof(bool) && bool.TryParse(value, out var b)) return (T)(object)b;
        if (typeof(T) == typeof(int) && int.TryParse(value, out var i)) return (T)(object)i;
        return null;
    }

    public async Task SetAsync(string key, string value, string? updatedBy = null)
    {
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = updatedBy;
        }
        else
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = updatedBy
            });
        }
        await db.SaveChangesAsync();
        cache.Invalidate();
    }
}
