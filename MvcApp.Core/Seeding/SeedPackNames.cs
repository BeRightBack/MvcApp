namespace MvcApp.Core.Seeding;

/// <summary>
/// The seed packs that exist, and nothing else.
///
/// A name in this list is a promise that a registered ISeedPack owns those rows. A name that
/// is not here cannot be applied, removed, or shown on the admin page, which is deliberate:
/// it stops a profile from quietly asking for content that nothing implements.
///
/// Three names that an earlier draft listed are gone on purpose — Store, Pages and Iptv.
/// None of them has reference rows to seed. Products, pages and streaming plans are all
/// content an admin or the owner supplies, and inventing product names or prices would be
/// fabricating data. They appear per template as modules instead, where a profile can turn
/// the feature on without a pack that owns invented rows.
///
/// This lives in Core so MvcApp.Services can reference the pack names when it validates a
/// template profile, without Services having to reference Infrastructure.
/// </summary>
public static class SeedPackNames
{
    public const string Community = "Community";
    public const string Blog = "Blog";
    public const string Dating = "Dating";
    public const string Events = "Events";
    public const string Gamification = "Gamification";
    public const string Chat = "Chat";
    public const string Ads = "Ads";
    public const string Video = "Video";

    public static readonly IReadOnlyList<string> All =
    [
        Community, Blog, Dating, Events, Gamification, Chat, Ads, Video,
    ];
}