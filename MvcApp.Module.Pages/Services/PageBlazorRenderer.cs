using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace MvcApp.Module.Pages.Services;

/// <summary>
/// Renders registered Blazor page widgets to static HTML using the Blazor
/// <see cref="HtmlRenderer"/>. Components are instantiated with the current
/// request's service provider, so <c>@inject</c>ed services resolve normally.
/// JSON parameter values are coerced to the exact CLR type of each
/// <c>[Parameter]</c> property before binding.
/// </summary>
public class PageBlazorRenderer(
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory,
    BlazorComponentRegistry registry) : IPageBlazorRenderer
{
    private readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _parameterCache = new();

    public Task<string> RenderAsync(string name, string? parametersJson = null)
    {
        if (!registry.TryGetInfo(name, out var info))
            throw new InvalidOperationException($"Unknown Blazor page widget '{name}'.");
        return RenderAsync(info.ComponentType, parametersJson);
    }

    public async Task<string> RenderAsync(Type componentType, string? parametersJson = null)
    {
        using var renderer = new HtmlRenderer(serviceProvider, loggerFactory);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync(componentType, BuildParameters(componentType, parametersJson));
            return root.ToHtmlString();
        });
        return html;
    }

    private ParameterView BuildParameters(Type componentType, string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson)) return ParameterView.Empty;

        using var doc = JsonDocument.Parse(parametersJson);
        var parameters = _parameterCache.GetOrAdd(componentType, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase));

        var dict = new Dictionary<string, object?>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!parameters.TryGetValue(prop.Name, out var pi)) continue;
            dict[prop.Name] = ConvertValue(ConvertJson(prop.Value), pi.PropertyType);
        }
        return ParameterView.FromDictionary(dict);
    }

    private static object? ConvertJson(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => el.EnumerateArray().Select(ConvertJson).ToList(),
            JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJson(p.Value)),
            _ => null
        };

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null) return null;
        var nullable = Nullable.GetUnderlyingType(targetType);
        var t = nullable ?? targetType;
        if (t == typeof(object) || t.IsInstanceOfType(value)) return value;
        if (t.IsEnum)
        {
            if (value is string s) return Enum.Parse(t, s, ignoreCase: true);
            return Enum.ToObject(t, Convert.ChangeType(value, Enum.GetUnderlyingType(t), CultureInfo.InvariantCulture));
        }
        if (t == typeof(Guid) && value is string g) return Guid.Parse(g);
        if (value is IConvertible) return Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
        return value;
    }
}
