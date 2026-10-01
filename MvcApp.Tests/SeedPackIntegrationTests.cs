using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MvcApp.Core;
using MvcApp.Infrastructure;
using MvcApp.Infrastructure.Seeding;
using MvcApp.Infrastructure.Seeding.Packs;
using MvcApp.Module.Ads;
using MvcApp.Module.Blog;
using MvcApp.Module.Chat;
using MvcApp.Module.Forum;
using MvcApp.Module.IPTV;
using MvcApp.Module.Messages;
using MvcApp.Module.Pages;
using MvcApp.Module.Store;
using MvcApp.Module.Utility;
using MvcApp.Module.Video;
using Xunit;
using MvcApp.Core.Seeding;

namespace MvcApp.Tests;

public class SeedPackIntegrationTests
{
    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("SEED_TEST_CONNECTION");

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDbConnection"] = connectionString,
                ["ConnectionStrings:LocalisationDbConnection"] = connectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(configuration);
        services.AddForum();
        services.AddBlog();
        services.AddChat();
        services.AddVideo();
        services.AddMessages();
        services.AddStore();
        services.AddIptv();
        services.AddPages();
        services.AddAdsModule();
        services.AddUtility();

        return services.BuildServiceProvider();
    }

    private static async Task ClearInterestTagsAsync(UserDbContext db)
    {
        await db.SeedManifest
            .Where(m => m.EntityType == typeof(InterestTag).FullName)
            .ExecuteDeleteAsync();

        await db.UserInterestTags.ExecuteDeleteAsync();
        await db.InterestTags.ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Dating_pack_adopts_removes_and_reapplies()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var provider = BuildProvider(connectionString);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var packs = scope.ServiceProvider.GetRequiredService<SeedPackService>();

        await ClearInterestTagsAsync(db);
        var pack = scope.ServiceProvider.GetRequiredService<ISeedPack>();

        var before = await db.InterestTags.CountAsync();
        Assert.Equal(0, before);

        await pack.SeedAsync(packs, db, "integration-test", CancellationToken.None);

        var afterApply = await db.InterestTags.CountAsync();
        Assert.Equal(41, afterApply);

        var manifestCount = await db.SeedManifest
            .CountAsync(m => m.PackName == SeedPackNames.Dating && m.EntityType == typeof(InterestTag).FullName);
        Assert.Equal(41, manifestCount);

        await packs.ApplyAsync<InterestTag>(
            SeedPackNames.Dating, _ => throw new InvalidOperationException("must not re-add"),
            appliedBy: "integration-test");
        Assert.Equal(41, await db.InterestTags.CountAsync());

        var removal = await packs.RemoveAsync(SeedPackNames.Dating);

        Assert.Equal(41, removal.RowsDeleted);
        Assert.Equal(0, await db.InterestTags.CountAsync());
        Assert.Equal(0, await db.SeedManifest.CountAsync(m => m.PackName == SeedPackNames.Dating));
        Assert.False(await packs.IsAppliedAsync(SeedPackNames.Dating));
    }

    [Fact]
    public async Task Adopt_claims_existing_rows_without_creating_duplicates()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var provider = BuildProvider(connectionString);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var packs = scope.ServiceProvider.GetRequiredService<SeedPackService>();

        await ClearInterestTagsAsync(db);

        db.InterestTags.AddRange(DatingInterestTags.Build().Take(3));
        await db.SaveChangesAsync();

        var adopted = await packs.AdoptAsync<InterestTag>(
            SeedPackNames.Dating, d => d.InterestTags, "integration-test");

        Assert.Equal(3, adopted);
        Assert.Equal(3, await db.InterestTags.CountAsync());

        var removal = await packs.RemoveAsync(SeedPackNames.Dating);
        Assert.Equal(3, removal.RowsDeleted);
        Assert.Equal(0, await db.InterestTags.CountAsync());
    }

    [Fact]
    public async Task Removing_a_pack_leaves_rows_it_never_created()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var provider = BuildProvider(connectionString);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var packs = scope.ServiceProvider.GetRequiredService<SeedPackService>();

        await ClearInterestTagsAsync(db);

        var owned = new InterestTag { Name = "Owned by pack", Category = InterestCategory.Hobby, IsCurated = true };
        var foreign = new InterestTag { Name = "Created by admin", Category = InterestCategory.Hobby, IsCurated = false };
        db.InterestTags.AddRange(owned, foreign);
        await db.SaveChangesAsync();

        await packs.ApplyAsync<InterestTag>(
            SeedPackNames.Dating,
            d => d.InterestTags.AddRange(DatingInterestTags.Build()),
            appliedBy: "integration-test");

        var beforeRemoval = await db.InterestTags.CountAsync();
        Assert.Equal(43, beforeRemoval);

        var removal = await packs.RemoveAsync(SeedPackNames.Dating);

        Assert.Equal(41, removal.RowsDeleted);
        var survivors = await db.InterestTags.Select(t => t.Name).OrderBy(n => n).ToListAsync();
        Assert.Equal(["Created by admin", "Owned by pack"], survivors);
    }
}
