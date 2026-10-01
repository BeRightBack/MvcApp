namespace MvcApp.Core.Abstractions;

public class TemplateSeedProfile
{
    public string Template { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public IReadOnlyList<string> Packs { get; set; } = [];
    public IReadOnlyList<string> Modules { get; set; } = [];
    public IReadOnlyList<string> OffModules { get; set; } = [];
}

public interface ITemplateProfileService
{
    IReadOnlyList<TemplateSeedProfile> GetProfiles();
    TemplateSeedProfile? GetProfile(string template);
    IReadOnlyList<string> GetTemplates();

    /// <summary>
    /// Names in a profile that no seed pack implements. Empty on a correct registry — this
    /// exists so a typo in a profile is caught rather than silently applied as "nothing".
    /// </summary>
    IReadOnlyList<string> GetUnknownPacks();
}
