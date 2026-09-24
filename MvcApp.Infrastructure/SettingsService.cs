using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class SettingsService(UserDbContext db) : ISettingsService
{
    public async Task<string?> GetAsync(string key)
    {
        var setting = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
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
    }
}
