namespace MvcApp.Infrastructure.Seeding;

public static class SeedPackNames
{
    public const string Universal = "Universal";
    public const string Community = "Community";
    public const string Blog = "Blog";
    public const string Dating = "Dating";
    public const string Events = "Events";
    public const string Gamification = "Gamification";
    public const string Chat = "Chat";
    public const string Ads = "Ads";
    public const string Video = "Video";
    public const string Pages = "Pages";
    public const string Iptv = "Iptv";

    public static readonly IReadOnlyList<string> All =
    [
        Universal, Community, Blog, Dating, Events, Gamification,
        Chat, Ads, Video, Pages, Iptv,
    ];
}

public sealed record SeedPackResult(string PackName, int RowsRecorded, int RowsAlreadyPresent);

public sealed record SeedPackRemoval(string PackName, int RowsDeleted, IReadOnlyDictionary<string, int> ByEntityType);

public sealed record SeedPackEntityCount(string EntityType, int Rows);
