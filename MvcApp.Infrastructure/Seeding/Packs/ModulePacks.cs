using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Seeding;

namespace MvcApp.Infrastructure.Seeding.Packs;

public sealed class PlansPack : ISeedPack
{
    public string Name => SeedPackNames.Plans;
    public string DisplayName => "Subscription plans";
    public string Description =>
        "The three VIP tiers and their duration ladder. Shared by /Vip and /iptv-store, so "
        + "every template that sells a membership lists this pack.";
    public IReadOnlyList<string> EntityNames => ["SubscriptionPlan", "SubscriptionDetail"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        // Adopt before apply. A database that already has tiers — an existing deployment, or
        // the Seeder that ran before packs existed — must be claimed into the manifest, not
        // duplicated. ApplyAsync no-ops on a non-empty table but records nothing when it does,
        // which would leave the pack permanently "not applied" and retried on every boot.
        await packs.AdoptAsync<SubscriptionPlan>(Name, d => d.SubscriptionPlans, appliedBy, ct);

        await packs.ApplyAsync<SubscriptionPlan>(Name, d =>
        {
            if (d.SubscriptionPlans.Any()) return;
            d.SubscriptionPlans.AddRange(SubscriptionPlanSeedData.Build());
        }, appliedBy: appliedBy, ct: ct);

        // The details are cascade-created through plan.SubscriptionDetails, so ApplyAsync above
        // only ever tracks SubscriptionPlan. Record them here or the manifest understates what
        // this pack owns. Removing the plans cascades to the details, so the two are consistent
        // either way; this makes IsApplied and the admin counts honest.
        await packs.AdoptAsync<SubscriptionDetail>(Name, d => d.SubscriptionDetails, appliedBy, ct);
    }
}

public sealed class ChatPack : ISeedPack
{
    public string Name => SeedPackNames.Chat;
    public string DisplayName => "Chat";
    public string Description => "Starter chat rooms. Dropped for a Magazine or Minimal site.";
    public IReadOnlyList<string> EntityNames => ["ChatRoom"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<ChatRoom>(Name, d =>
        {
            if (d.ChatRooms.Any()) return;
            d.ChatRooms.AddRange(
                new ChatRoom { Name = "General Discussion", Description = "Talk about anything and everything" },
                new ChatRoom { Name = "Tech Support", Description = "Get help with technical issues" },
                new ChatRoom { Name = "Announcements", Description = "Official announcements and updates" });
        }, appliedBy: appliedBy, ct: ct);
    }
}

public sealed class EventsPack : ISeedPack
{
    public string Name => SeedPackNames.Events;
    public string DisplayName => "Events";
    public string Description => "Event categories for the events module.";
    public IReadOnlyList<string> EntityNames => ["EventCategory"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<EventCategory>(Name, d =>
        {
            if (d.EventCategories.Any()) return;
            d.EventCategories.AddRange(
                new EventCategory { Name = "Party", Icon = "bx-party" },
                new EventCategory { Name = "Networking", Icon = "bx-group" },
                new EventCategory { Name = "Sports", Icon = "bx-dumbbell" },
                new EventCategory { Name = "Music", Icon = "bx-music" },
                new EventCategory { Name = "Food & Drink", Icon = "bx-food" },
                new EventCategory { Name = "Nightlife", Icon = "bx-moon" },
                new EventCategory { Name = "Outdoors", Icon = "bx-sun" },
                new EventCategory { Name = "Arts & Culture", Icon = "bx-palette" },
                new EventCategory { Name = "Tech", Icon = "bx-chip" },
                new EventCategory { Name = "Other", Icon = "bx-calendar" });
        }, appliedBy: appliedBy, ct: ct);
    }
}

public sealed class GamificationPack : ISeedPack
{
    public string Name => SeedPackNames.Gamification;
    public string DisplayName => "Gamification";
    public string Description =>
        "Badge catalogue. Profile and achievement badges are universal; the dating badges "
        + "are only useful on a social or members template.";
    public IReadOnlyList<string> EntityNames => ["Badge"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<Badge>(Name, d =>
        {
            if (d.Badges.Any()) return;
            d.Badges.AddRange(GamificationBadges.Build());
        }, appliedBy: appliedBy, ct: ct);
    }
}

public static class GamificationBadges
{
    public static IReadOnlyList<Badge> Build() =>
    [
        new() { Name = "Profile Pro", Description = "Complete your profile", Icon = "bx bx-user", Category = BadgeCategory.Profile, CriteriaType = "ProfileComplete", CriteriaValue = 1, SortOrder = 1 },
        new() { Name = "Photo Star", Description = "Upload 5 photos", Icon = "bx bx-camera", Category = BadgeCategory.Profile, CriteriaType = "PhotosUploaded", CriteriaValue = 5, SortOrder = 2 },
        new() { Name = "Verified", Description = "Get verified", Icon = "bx bx-check-shield", Category = BadgeCategory.Profile, CriteriaType = "Verified", CriteriaValue = 1, SortOrder = 3 },

        new() { Name = "First Message", Description = "Send your first message", Icon = "bx bx-envelope", Category = BadgeCategory.Social, CriteriaType = "MessagesSent", CriteriaValue = 1, SortOrder = 10 },
        new() { Name = "Social Butterfly", Description = "Send 10 messages", Icon = "bx bx-chat", Category = BadgeCategory.Social, CriteriaType = "MessagesSent", CriteriaValue = 10, SortOrder = 11 },
        new() { Name = "Conversationalist", Description = "Send 50 messages", Icon = "bx bx-conversation", Category = BadgeCategory.Social, CriteriaType = "MessagesSent", CriteriaValue = 50, SortOrder = 12 },

        new() { Name = "Heart Seeker", Description = "Receive your first like", Icon = "bx bx-heart", Category = BadgeCategory.Dating, CriteriaType = "MatchesReceived", CriteriaValue = 1, SortOrder = 20 },
        new() { Name = "Popular", Description = "Receive 10 likes", Icon = "bx bx-heart-circle", Category = BadgeCategory.Dating, CriteriaType = "MatchesReceived", CriteriaValue = 10, SortOrder = 21 },
        new() { Name = "Heartthrob", Description = "Receive 50 likes", Icon = "bx bx-heart", Category = BadgeCategory.Dating, CriteriaType = "MatchesReceived", CriteriaValue = 50, SortOrder = 22 },
        new() { Name = "Super Connector", Description = "Send 10 Super Likes", Icon = "bx bx-star", Category = BadgeCategory.Dating, CriteriaType = "SuperLikesSent", CriteriaValue = 10, SortOrder = 23 },
        new() { Name = "Superstar", Description = "Receive 10 Super Likes", Icon = "bx bxs-star", Category = BadgeCategory.Dating, CriteriaType = "SuperLikesReceived", CriteriaValue = 10, SortOrder = 24 },
        new() { Name = "Boosted", Description = "Use your first Boost", Icon = "bx bx-bolt-circle", Category = BadgeCategory.Dating, CriteriaType = "BoostsUsed", CriteriaValue = 1, SortOrder = 25 },
        new() { Name = "On Fire", Description = "Use 5 Boosts", Icon = "bx bx-flame", Category = BadgeCategory.Dating, CriteriaType = "BoostsUsed", CriteriaValue = 5, SortOrder = 26 },

        new() { Name = "Getting Started", Description = "Earn 100 points", Icon = "bx bx-star", Category = BadgeCategory.Achievement, CriteriaType = "TotalPoints", CriteriaValue = 100, SortOrder = 30 },
        new() { Name = "Rising Star", Description = "Earn 500 points", Icon = "bx bx-star-half", Category = BadgeCategory.Achievement, CriteriaType = "TotalPoints", CriteriaValue = 500, SortOrder = 31 },
        new() { Name = "Champion", Description = "Earn 1000 points", Icon = "bx bx-trophy", Category = BadgeCategory.Achievement, CriteriaType = "TotalPoints", CriteriaValue = 1000, SortOrder = 32 },
        new() { Name = "Veteran", Description = "Active for 30 days", Icon = "bx bx-calendar", Category = BadgeCategory.Achievement, CriteriaType = "DaysActive", CriteriaValue = 30, SortOrder = 33 },
    ];
}
