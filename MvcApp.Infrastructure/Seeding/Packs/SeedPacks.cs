using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Seeding;

namespace MvcApp.Infrastructure.Seeding.Packs;

public interface ISeedPack
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }
    IReadOnlyList<string> EntityNames { get; }
    Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct);
}

public sealed class CommunityPack : ISeedPack
{
    public string Name => SeedPackNames.Community;
    public string DisplayName => "Community forum";
    public string Description => "Forum categories and boards for discussion.";
    public IReadOnlyList<string> EntityNames => ["ForumCategory", "Forum"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<ForumCategory>(Name, d =>
        {
            if (d.ForumCategories.Any()) return;
            d.ForumCategories.AddRange(
                new ForumCategory { Name = "General", Description = "General discussions", SortOrder = 0 },
                new ForumCategory { Name = "Support", Description = "Get help and support", SortOrder = 1 },
                new ForumCategory { Name = "Off-Topic", Description = "Anything not covered elsewhere", SortOrder = 2 });
        }, existing: c => c.ForumCategories,
            appliedBy: appliedBy, ct: ct);

        await packs.ApplyAsync<Forum>(Name, d =>
        {
            if (d.Forums.Any()) return;

            var general = d.ForumCategories.Single(c => c.Name == "General");
            var support = d.ForumCategories.Single(c => c.Name == "Support");
            var offTopic = d.ForumCategories.Single(c => c.Name == "Off-Topic");

            d.Forums.AddRange(
                new Forum { CategoryId = general.Id, Name = "Introductions", Description = "Introduce yourself to the community", SortOrder = 0 },
                new Forum { CategoryId = general.Id, Name = "General Discussion", Description = "Talk about anything", SortOrder = 1 },
                new Forum { CategoryId = support.Id, Name = "Technical Support", Description = "Get help with technical issues", SortOrder = 0 },
                new Forum { CategoryId = support.Id, Name = "Feature Requests", Description = "Suggest new features", SortOrder = 1 },
                new Forum { CategoryId = offTopic.Id, Name = "Random Chat", Description = "Casual conversation", SortOrder = 0 },
                new Forum { CategoryId = offTopic.Id, Name = "Games & Fun", Description = "Gaming discussions and fun threads", SortOrder = 1 });
        }, existing: c => c.Forums,
            appliedBy: appliedBy, ct: ct);
    }
}

public sealed class BlogPack : ISeedPack
{
    public string Name => SeedPackNames.Blog;
    public string DisplayName => "Blog";
    public string Description => "Blog categories and tags for published articles.";
    public IReadOnlyList<string> EntityNames => ["BlogCategory", "BlogTag"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<BlogCategory>(Name, d =>
        {
            if (d.BlogCategories.Any()) return;
            d.BlogCategories.AddRange(
                new BlogCategory { Name = "Technology", Slug = "technology", Description = "Tech news and tutorials", SortOrder = 0 },
                new BlogCategory { Name = "News", Slug = "news", Description = "Company and product announcements", SortOrder = 1 },
                new BlogCategory { Name = "Guides", Slug = "guides", Description = "How-to guides and best practices", SortOrder = 2 });
        }, existing: c => c.BlogCategories,
            appliedBy: appliedBy, ct: ct);

        await packs.ApplyAsync<BlogTag>(Name, d =>
        {
            if (d.BlogTags.Any()) return;
            d.BlogTags.AddRange(
                new BlogTag { Name = "Getting Started", Slug = "getting-started" },
                new BlogTag { Name = "Tips & Tricks", Slug = "tips-tricks" },
                new BlogTag { Name = "Updates", Slug = "updates" },
                new BlogTag { Name = "Tutorial", Slug = "tutorial" });
        }, existing: c => c.BlogTags,
            appliedBy: appliedBy, ct: ct);
    }
}

public sealed class DatingPack : ISeedPack
{
    public string Name => SeedPackNames.Dating;
    public string DisplayName => "Dating";
    public string Description =>
        "Interest tags for member discovery. The VIP plans used to sit here too, but they are "
        + "not dating content — Plans owns them, and every template that sells a tier lists it. "
        + "Not needed for a business, magazine or IPTV site.";
    public IReadOnlyList<string> EntityNames => ["InterestTag"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<InterestTag>(Name, d =>
        {
            if (d.InterestTags.Any()) return;
            d.InterestTags.AddRange(DatingInterestTags.Build());
        }, existing: c => c.InterestTags,
            appliedBy: appliedBy, ct: ct);
    }
}
