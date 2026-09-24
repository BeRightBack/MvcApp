using System.Reflection;

namespace MvcApp.Infrastructure;

public static class ModuleConfigurationRegistry
{
    private static readonly List<Assembly> _assemblies = [];

    public static IReadOnlyList<Assembly> Assemblies => _assemblies.AsReadOnly();

    public static void AddEntityConfigurationAssembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);
    }
}
