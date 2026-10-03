using MvcApp.Core.Abstractions;
using MvcApp.Core.Seeding;

namespace MvcApp.Services;

/// <summary>
/// The seed contract for every template. A profile declares three independent things — what content
/// it seeds, what it is composed of, and what it defaults — so switching templates no longer leaves
/// one template's content sitting in the next template's database.
///
/// Nothing here applies or removes anything. A profile is data: the packs own the rows, the manifest
/// owns the bookkeeping, and removal stays an explicit admin action.
///
/// Only packs that exist in <c>SeedPackNames.All</c> may be listed. A module with no reference rows
/// to seed — Store, Pages, IPTV — belongs in <c>Composition.Modules</c> rather than
/// <c>Content.Packs</c>; its content is supplied by the owner, not invented here.
///
/// These profiles live in the platform today, which is itself a coupling worth removing: a template
/// should be addable without editing core. Keeping the three concerns separate is the prerequisite
/// for moving them out to data.
/// </summary>
public class TemplateProfileService : ITemplateProfileService
{
    private static readonly List<TemplateSeedProfile> _profiles =
    [
        new()
        {
            Template = "Default",
            Purpose = "Neutral starting point: the community shell only.",
            Content = new() { Packs = [SeedPackNames.Community] },
            Composition = new() { Modules = ["Forum", "Messages", "Pages"], OffModules = ["Iptv"] },
        },
        new()
        {
            Template = "Dating",
            Purpose = "Adult dating club. Interest tags and VIP tiers are the point.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Dating, SeedPackNames.Plans] },
            Composition = new()
            {
                Modules = ["Forum", "Chat", "Messages", "Video", "Utility", "Gamification", "Pages"],
                OffModules = ["Iptv"],
            },
        },
        new()
        {
            Template = "Luxury",
            Purpose = "Members-only premium club. VIP tiers, chat and video.",
            Content = new()
            {
                Packs = [SeedPackNames.Community, SeedPackNames.Dating, SeedPackNames.Plans, SeedPackNames.Chat, SeedPackNames.Video],
            },
            Composition = new()
            {
                Modules = ["Forum", "Chat", "Messages", "Video", "Utility", "Gamification", "Pages"],
                OffModules = ["Iptv"],
            },
        },
        new()
        {
            Template = "Social",
            Purpose = "Social community. Forum, chat, messages and video.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Chat, SeedPackNames.Video] },
            Composition = new() { Modules = ["Forum", "Chat", "Video", "Messages", "Pages"], OffModules = ["Iptv"] },
        },
        new()
        {
            Template = "Store",
            Purpose = "Commerce front. The community shell; products come from the owner.",
            Content = new() { Packs = [SeedPackNames.Community] },
            Composition = new() { Modules = ["Store", "Forum", "Messages", "Pages", "Ads"], OffModules = ["Iptv"] },
        },
        new()
        {
            Template = "Business",
            Purpose = "Corporate portal. Content, pages, forums and a store; no dating, no IPTV.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Blog, SeedPackNames.Ads] },
            Composition = new()
            {
                Modules = ["Forum", "Messages", "Pages", "Blog", "Store", "Ads", "Utility"],
                OffModules = ["Iptv"],
            },
        },
        new()
        {
            Template = "Professional",
            Purpose = "Business and professional networking, with a store.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Blog, SeedPackNames.Ads] },
            Composition = new() { Modules = ["Forum", "Messages", "Pages", "Blog", "Store", "Ads"], OffModules = ["Iptv"] },
        },
        new()
        {
            Template = "Magazine",
            Purpose = "Editorial publication. Blog-led, with pages and discussion.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Blog, SeedPackNames.Ads] },
            Composition = new() { Modules = ["Blog", "Pages", "Forum", "Messages", "Ads"], OffModules = ["Iptv"] },
        },
        new()
        {
            Template = "Minimal",
            Purpose = "Content-first reading site. Blog and pages, nothing else.",
            Content = new() { Packs = [SeedPackNames.Blog] },
            Composition = new() { Modules = ["Blog", "Pages"], OffModules = ["Iptv"] },
        },
        new()
        {
            Template = "Gaming",
            Purpose = "Gaming community. Chat, events, gamification and leaderboards.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Chat, SeedPackNames.Events, SeedPackNames.Gamification] },
            Composition = new()
            {
                Modules = ["Forum", "Chat", "Messages", "Events", "Gamification", "Utility", "Pages"],
                OffModules = ["Iptv"],
            },
        },
        new()
        {
            Template = "IPTV",
            Purpose = "Streaming service. Subscription plans and a support forum.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Plans] },
            Composition = new() { Modules = ["Iptv", "Forum", "Pages", "Messages"], OffModules = [] },
        },
        new()
        {
            Template = "Frenzyzone",
            Purpose = "Business portal: services, publishing, a storefront and contact. No dating, no IPTV, no membership tiers.",
            Content = new() { Packs = [SeedPackNames.Community, SeedPackNames.Blog, SeedPackNames.Ads] },
            Composition = new()
            {
                Modules = ["Forum", "Messages", "Pages", "Blog", "Store", "Ads", "Utility"],
                OffModules = ["Iptv"],
            },
        },
    ];

    public IReadOnlyList<TemplateSeedProfile> GetProfiles() => _profiles;

    public TemplateSeedProfile? GetProfile(string template) =>
        _profiles.FirstOrDefault(p => p.Template.Equals(template, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> GetTemplates() =>
        _profiles.Select(p => p.Template).ToList();

    public IReadOnlyList<string> GetUnknownPacks() =>
    [
        .. _profiles
            .SelectMany(p => p.Content.Packs)
            .Where(pack => !SeedPackNames.All.Contains(pack, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];
}
