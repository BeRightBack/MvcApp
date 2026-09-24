namespace MvcApp.Core.Abstractions;

public interface IModuleManager
{
    Task<bool> IsModuleEnabledAsync(string moduleName);
    Task<List<ModuleInfo>> GetAllModulesAsync();
    Task SetModuleEnabledAsync(string moduleName, bool enabled);
}

public class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}