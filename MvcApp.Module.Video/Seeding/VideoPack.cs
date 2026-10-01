using Microsoft.EntityFrameworkCore;
using MvcApp.Infrastructure;
using MvcApp.Infrastructure.Seeding;
using MvcApp.Infrastructure.Seeding.Packs;
using MvcApp.Core.Seeding;

namespace MvcApp.Module.Video.Seeding;

/// <summary>
/// VideoRooms were written into the AddVideoEntities migration, which meant the "Dating
/// Lounge" row could never be removed from a site that is not a dating or social one.
/// This pack claims the migration rows through adoption and only seeds them on a genuinely
/// fresh database, so the whole set is removable as one unit.
/// </summary>
public sealed class VideoPack : ISeedPack
{
    public string Name => SeedPackNames.Video;
    public string DisplayName => "Video";
    public string Description => "Starter video rooms for the video-chat module.";
    public IReadOnlyList<string> EntityNames => ["VideoRoom"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        if (await packs.IsAppliedAsync(Name, ct))
            return;

        var claimed = await packs.AdoptAsync<VideoRoom>(Name, d => d.Set<VideoRoom>(), adoptedBy: appliedBy, ct: ct);
        if (claimed > 0)
            return;

        await packs.ApplyAsync<VideoRoom>(Name, d =>
        {
            if (d.Set<VideoRoom>().Any()) return;
            d.Set<VideoRoom>().AddRange(Build());
        }, appliedBy: appliedBy, ct: ct);
    }

    internal static IReadOnlyList<VideoRoom> Build() =>
    [
        new() { Name = "Main Lounge", Description = "The main video lounge — come say hello on camera", MaxSpots = 10 },
        new() { Name = "Dating Lounge", Description = "Meet new people face-to-face", MaxSpots = 10 },
        new() { Name = "VIP Lounge", Description = "Smaller, more intimate room", MaxSpots = 4 },
    ];
}