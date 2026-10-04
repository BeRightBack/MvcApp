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
    public string Description =>
        "Blog categories and tags for published articles, plus two example articles so a site that "
        + "declares this pack has something to publish and list.";
    public IReadOnlyList<string> EntityNames => ["BlogCategory", "BlogTag", "BlogPost"];

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

        await SeedExamplePostsAsync(packs, appliedBy, ct);
    }

    /// <summary>
    /// Two example articles, so a site that declares the Blog pack has something to list and read
    /// rather than an empty index. Its own manifest entry, so it applies on databases where the
    /// categories and tags were seeded or adopted long ago — hung off the calls above it would only
    /// ever seed on a fresh one.
    ///
    /// They are examples, not invented claims about the owner's business: they describe the platform
    /// and say plainly that they can be replaced or deleted.
    /// </summary>
    private async Task SeedExamplePostsAsync(SeedPackService packs, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<BlogPost>(Name, d =>
        {
            if (d.Set<BlogPost>().Any()) return;

            var category = d.Set<BlogCategory>().FirstOrDefault(c => c.Slug == "technology");
            var now = DateTime.UtcNow;

            d.Set<BlogPost>().AddRange(
                new BlogPost
                {
                    Title = "Welcome to the new site",
                    Slug = "welcome-to-the-new-site",
                    Excerpt = "How this site is put together, and what you can do here.",
                    Content = """<p>This is an example article, created so every part of the publishing flow has something real to render: the index, the article page, and the in-article advertising slot.</p><h2>What is here</h2><p>The site is assembled from modules. A template decides which of them a deployment is built from, so a business portal and an editorial site can run the same application without either carrying the other's features.</p><ul><li>Articles and categories.</li><li>Forums, with categories and boards.</li><li>Advertising zones, in several positions on the page.</li></ul><h2>Editing this</h2><p>This text is ordinary content: replace it, delete it, or keep it as a starting point. Nothing in the application depends on it existing.</p>""",
                    CategoryId = category?.Id,
                    CreatedByUsername = "admin",
                    IsPublished = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    PublishedAt = now
                },
                new BlogPost
                {
                    Title = "What is new in this release",
                    Slug = "what-is-new-in-this-release",
                    Excerpt = "A short summary of the recent changes to the platform.",
                    Content = """<p>A second example article, so the index has more than one entry to list and the category filter has something to filter.</p><h2>Composition</h2><p>A deployment is built from the modules its template declares, and the data layer follows the same decision: the entities of modules a site does not include are not mapped at all.</p><h2>Advertising</h2><p>Ads render from zones. The layout, and article pages, ask for a zone by name and the module decides what, if anything, is due.</p>""",
                    CategoryId = category?.Id,
                    CreatedByUsername = "admin",
                    IsPublished = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    PublishedAt = now
                });
        }, existing: c => c.Set<BlogPost>(),
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
