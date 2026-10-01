using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core;
using MvcApp.Infrastructure;
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

namespace MvcApp.Tests;

public class EntityModelTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDbConnection"] = "Server=localhost;Database=unused;Uid=unused;Pwd=unused;",
                ["ConnectionStrings:LocalisationDbConnection"] = "Server=localhost;Database=unused;Uid=unused;Pwd=unused;",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
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

    private static IEntityType GetEntityType(IServiceProvider provider, Type clrType)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        return db.Model.FindEntityType(clrType)
            ?? throw new InvalidOperationException($"{clrType.Name} is missing from the model.");
    }

    [Fact]
    public void Infrastructure_entity_configurations_are_applied()
    {
        var provider = BuildProvider();
        var report = GetEntityType(provider, typeof(Report));

        var reporter = report.GetForeignKeys()
            .Single(f => f.Properties.Single().Name == nameof(Report.ReporterId));
        var reported = report.GetForeignKeys()
            .Single(f => f.Properties.Single().Name == nameof(Report.ReportedUserId));

        Assert.Equal(DeleteBehavior.Restrict, reporter.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, reported.DeleteBehavior);
    }

    [Fact]
    public void Infrastructure_entity_configuration_indexes_are_applied()
    {
        var provider = BuildProvider();

        var reportIndexes = IndexProperties(GetEntityType(provider, typeof(Report)));
        Assert.Contains(nameof(Report.Status), reportIndexes);
        Assert.Contains(nameof(Report.ReportedUserId), reportIndexes);
        Assert.Contains(nameof(Report.CreatedAt), reportIndexes);

        var videoIndexes = IndexProperties(GetEntityType(provider, typeof(VideoUpload)));
        Assert.Contains(nameof(VideoUpload.Category), videoIndexes);
        Assert.Contains(nameof(VideoUpload.IsApproved), videoIndexes);
    }

    [Fact]
    public void EventRSVP_unique_index_is_applied()
    {
        var provider = BuildProvider();
        var rsvp = GetEntityType(provider, typeof(EventRSVP));

        var unique = rsvp.GetIndexes()
            .Where(i => i.IsUnique)
            .SelectMany(i => i.Properties)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(EventRSVP.EventId), unique);
        Assert.Contains(nameof(EventRSVP.UserId), unique);
    }

    [Fact]
    public void Model_matches_the_migration_snapshot()
    {
        var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        Assert.False(
            db.Database.HasPendingModelChanges(),
            "The model has drifted from the migration snapshot. Generate a migration, or remove the change that caused it.");
    }

    private static HashSet<string> IndexProperties(IEntityType entityType) =>
        entityType.GetIndexes()
            .SelectMany(i => i.Properties)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
}
