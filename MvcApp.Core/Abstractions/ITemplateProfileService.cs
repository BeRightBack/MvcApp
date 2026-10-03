namespace MvcApp.Core.Abstractions;

/// <summary>
/// What a template is, split into the three things it actually has to declare — because they have
/// different lifetimes and different owners:
///
/// <list type="bullet">
///   <item><description><see cref="Content"/> — rows seeded once into a site (seed packs).</description></item>
///   <item><description><see cref="Composition"/> — what the template is built FROM. Decided before a
///   site exists, and what lets a published site ship without the code it does not use.</description></item>
///   <item><description><see cref="Defaults"/> — settings the template seeds and the owner then edits
///   for the life of the site.</description></item>
/// </list>
///
/// A business portal and a dating site share almost nothing here: different content, a different
/// module set, different defaults. Keeping them as separate models is what stops one template's
/// assumptions leaking into another's.
/// </summary>
public class TemplateSeedProfile
{
    public string Template { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    /// <summary>The rows this template seeds.</summary>
    public TemplateContentProfile Content { get; set; } = new();

    /// <summary>What this template is composed of.</summary>
    public TemplateCompositionProfile Composition { get; set; } = new();

    /// <summary>The settings this template defaults.</summary>
    public TemplateDefaultsProfile Defaults { get; set; } = new();
}

/// <summary>Content: the seed packs a template seeds.</summary>
public class TemplateContentProfile
{
    /// <summary>
    /// Seed pack names. Only packs that exist in <c>SeedPackNames.All</c> may be listed — a typo is
    /// caught by <see cref="ITemplateProfileService.GetUnknownPacks"/> rather than silently applying
    /// as "nothing".
    /// </summary>
    public IReadOnlyList<string> Packs { get; set; } = [];
}

/// <summary>
/// Composition: the modules a template contains, and the ones it deliberately excludes.
///
/// Today this is applied at runtime through module flags. It is expressed here as a *build-time* fact
/// because that is where it belongs: a published site should ship only the modules it contains, not
/// every module plus a flag recording which are switched on.
/// </summary>
public class TemplateCompositionProfile
{
    /// <summary>Modules this template contains.</summary>
    public IReadOnlyList<string> Modules { get; set; } = [];

    /// <summary>
    /// Modules this template deliberately excludes. Stated rather than implied by absence, so a reader
    /// can distinguish "we chose not to include this" from "nobody considered it".
    /// </summary>
    public IReadOnlyList<string> OffModules { get; set; } = [];
}

/// <summary>
/// Defaults: setting values a template seeds.
///
/// The active-template setting is derived from <see cref="TemplateSeedProfile.Template"/> by whoever
/// applies these, so it is not listed. Everything in <see cref="Settings"/> is a product decision —
/// the owner knows what a dating site defaults to and what a business portal does.
/// </summary>
public class TemplateDefaultsProfile
{
    /// <summary>Setting key → default value.</summary>
    public IReadOnlyDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
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
