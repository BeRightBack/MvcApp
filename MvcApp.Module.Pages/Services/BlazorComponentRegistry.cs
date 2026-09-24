using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using MvcApp.Core;

namespace MvcApp.Module.Pages.Services;

public sealed record BlazorComponentInfo(
    string Name,
    string DisplayName,
    string Description,
    string Category,
    Type ComponentType);

/// <summary>
/// Catalog of Blazor components available as page widgets. Components are
/// discovered from assemblies registered via
/// <see cref="RegisterAssembly(Assembly)"/> by looking for the
/// <see cref="BlazorPageComponentAttribute"/>. Registering new assemblies (or
/// annotating new components) automatically makes them available in the page
/// editor's Blazor picker.
/// </summary>
public class BlazorComponentRegistry
{
    private readonly ConcurrentDictionary<string, BlazorComponentInfo> _components = new();

    public IReadOnlyList<BlazorComponentInfo> Components =>
        _components.Values.OrderBy(c => c.Category).ThenBy(c => c.DisplayName).ToList();

    public void RegisterAssembly(Assembly assembly)
    {
        if (assembly == null) return;
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !typeof(IComponent).IsAssignableFrom(type)) continue;
            var attr = type.GetCustomAttribute<BlazorPageComponentAttribute>();
            if (attr == null) continue;
            _components[attr.Name] = new BlazorComponentInfo(
                attr.Name,
                string.IsNullOrWhiteSpace(attr.DisplayName) ? attr.Name : attr.DisplayName!,
                attr.Description ?? string.Empty,
                string.IsNullOrWhiteSpace(attr.Category) ? "Blazor" : attr.Category,
                type);
        }
    }

    public bool TryGetInfo(string name, out BlazorComponentInfo info)
        => _components.TryGetValue(name, out info!);

    public bool Contains(string name) => _components.ContainsKey(name);
}
