using MvcApp.Core.Abstractions;
using MvcApp.Core.Seeding;

namespace MvcApp.Services;

/// <summary>
/// The seed contract for every template. A profile says which seed packs a template needs
/// and which modules it turns on, so switching templates no longer leaves one template's
/// content sitting in the next template's database.
///
/// Nothing here applies or removes anything. A profile is data: the packs own the rows, the
/// manifest owns the bookkeeping, and removal stays an explicit admin action.
///
/// Only packs that exist in <c>SeedPackNames.All</c> may be listed. A module with no
/// reference rows to seed — Store, Pages, IPTV — belongs in <see cref="TemplateSeedProfile.Modules"/>
/// rather than in <see cref="TemplateSeedProfile.Packs"/>; its content is supplied by the owner,
/// not invented here.
/// </summary>
public class TemplateProfileService : ITemplateProfileService
{
    private static readonly List<TemplateSeedProfile> _profiles =
    [
        new()
        {
            Template = "Default",
            Purpose = "Neutral starting point: the community shell only.",
            Packs = [SeedPackNames.Community],
            Modules = ["Forum", "Messages", "Pages"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Dating",
            Purpose = "Adult dating club. Interest tags and VIP tiers are the point.",
            Packs = [SeedPackNames.Community, SeedPackNames.Dating],
            Modules = ["Forum", "Chat", "Messages", "Video", "Utility", "Gamification", "Pages"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Luxury",
            Purpose = "Members-only premium club. VIP tiers, chat and video.",
            Packs = [SeedPackNames.Community, SeedPackNames.Dating, SeedPackNames.Chat, SeedPackNames.Video],
            Modules = ["Forum", "Chat", "Messages", "Video", "Utility", "Gamification", "Pages"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Social",
            Purpose = "Social community. Forum, chat, messages and video.",
            Packs = [SeedPackNames.Community, SeedPackNames.Chat, SeedPackNames.Video],
            Modules = ["Forum", "Chat", "Video", "Messages", "Pages"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Store",
            Purpose = "Commerce front. The community shell; products come from the owner.",
            Packs = [SeedPackNames.Community],
            Modules = ["Store", "Forum", "Messages", "Pages", "Ads"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Business",
            Purpose = "Corporate portal. Content, pages, forums and a store; no dating, no IPTV.",
            Packs = [SeedPackNames.Community, SeedPackNames.Blog, SeedPackNames.Ads],
            Modules = ["Forum", "Messages", "Pages", "Blog", "Store", "Ads", "Utility"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Professional",
            Purpose = "Business and professional networking, with a store.",
            Packs = [SeedPackNames.Community, SeedPackNames.Blog, SeedPackNames.Ads],
            Modules = ["Forum", "Messages", "Pages", "Blog", "Store", "Ads"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Magazine",
            Purpose = "Editorial publication. Blog-led, with pages and discussion.",
            Packs = [SeedPackNames.Community, SeedPackNames.Blog, SeedPackNames.Ads],
            Modules = ["Blog", "Pages", "Forum", "Messages", "Ads"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Minimal",
            Purpose = "Content-first reading site. Blog and pages, nothing else.",
            Packs = [SeedPackNames.Blog],
            Modules = ["Blog", "Pages"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "Gaming",
            Purpose = "Gaming community. Chat, events, gamification and leaderboards.",
            Packs = [SeedPackNames.Community, SeedPackNames.Chat, SeedPackNames.Events, SeedPackNames.Gamification],
            Modules = ["Forum", "Chat", "Messages", "Events", "Gamification", "Utility", "Pages"],
            OffModules = ["Iptv"],
        },
        new()
        {
            Template = "IPTV",
            Purpose = "Streaming service. Subscription plans and a support forum.",
            Packs = [SeedPackNames.Community],
            Modules = ["Iptv", "Forum", "Pages", "Messages"],
            OffModules = [],
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
            .SelectMany(p => p.Packs)
            .Where(pack => !SeedPackNames.All.Contains(pack, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];
}