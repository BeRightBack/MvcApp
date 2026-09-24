using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RazorEngineCore;
using MvcApp.Core;

namespace MvcApp.Module.Pages.Services;

public partial class PageRenderer(
    IServiceScopeFactory scopeFactory,
    ILogger<PageRenderer> logger) : IPageRenderer
{
    private readonly IRazorEngine _engine = new RazorEngine();
    private readonly ConcurrentDictionary<string, Lazy<Task<IRazorEngineCompiledTemplate<ContentPageTemplateBase>>>> _cache = new();

    public async Task<string> RenderAsync(string razorBody, ContentPage model, ClaimsPrincipal? user = null)
    {
        if (string.IsNullOrWhiteSpace(razorBody))
            return string.Empty;

        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(razorBody)));
        var compiled = _cache.GetOrAdd(key, _ => new Lazy<Task<IRazorEngineCompiledTemplate<ContentPageTemplateBase>>>(
            () => _engine.CompileAsync<ContentPageTemplateBase>(razorBody, builder =>
            {
                builder.AddAssemblyReference(typeof(ContentPageTemplateBase).Assembly);
                builder.AddAssemblyReference(typeof(ContentPage).Assembly);
                builder.AddUsing("System");
                builder.AddUsing("System.Linq");
                builder.AddUsing("System.Collections.Generic");
                builder.AddUsing("System.Text");
                builder.AddUsing("MvcApp.Core");
                builder.AddUsing("MvcApp.Core.Abstractions");
            }))).Value;

        var template = await compiled;
        using var scope = scopeFactory.CreateScope();
        var output = await template.RunAsync(instance =>
        {
            instance.Model = model;
            instance.Services = scope.ServiceProvider;
            instance.User = user;
        });
        return await ReplaceBlazorWidgetsAsync(output, scope.ServiceProvider);
    }

    private async Task<string> ReplaceBlazorWidgetsAsync(string output, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(output) || !output.Contains("__PAGEBLAZOR__", StringComparison.Ordinal))
            return output;

        var sb = new StringBuilder(output);
        var matches = BlazorTokenRegex().Matches(output);
        foreach (Match match in matches)
        {
            var name = match.Groups["name"].Value;
            var json = string.Empty;
            try
            {
                var b64 = match.Groups["b64"].Value;
                json = string.IsNullOrEmpty(b64) ? string.Empty : Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                var renderer = services.GetRequiredService<IPageBlazorRenderer>();
                var html = await renderer.RenderAsync(name, json);
                sb.Replace(match.Value, html);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to render Blazor page widget '{Name}'.", name);
                sb.Replace(match.Value, $"<div class=\"alert alert-danger mb-3\">Blazor widget '{name}' failed to render: {HttpUtility.HtmlEncode(ex.Message)}</div>");
            }
        }
        return sb.ToString();
    }

    [GeneratedRegex("__PAGEBLAZOR__:(?<name>[A-Za-z0-9_.-]+):(?<b64>[A-Za-z0-9+/=]*)__", RegexOptions.Compiled)]
    private static partial Regex BlazorTokenRegex();
}
