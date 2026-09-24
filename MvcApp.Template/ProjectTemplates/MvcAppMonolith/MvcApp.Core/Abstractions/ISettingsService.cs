namespace MvcApp.Core.Abstractions;

public interface ISettingsService
{
    Task<string?> GetAsync(string key);
    Task<T?> GetAsync<T>(string key) where T : struct;
    Task SetAsync(string key, string value, string? updatedBy = null);
}
