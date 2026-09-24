using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Data.EntityConfigurations;

public class AdZoneConfiguration : IEntityTypeConfiguration<AdZone>
{
    public void Configure(EntityTypeBuilder<AdZone> entity)
    {
        entity.ToTable("AdZones");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Key).IsUnique();
        entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.DefaultCssClass).HasMaxLength(200);
        entity.Property(e => e.BannerWrapperTemplate).HasMaxLength(500);

        entity.HasData(
            new AdZone { Id = 1, Key = "top-header", BannerWidth = 970, BannerHeight = 250, Name = "Top Header", Description = "Full-width banner at the very top of the page", MaxBanners = 1, ExcludeFromLandingPage = true, DefaultCssClass = "ad-zone ad-top-header mb-3", DisplayOrder = 10, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AdZone { Id = 2, Key = "sidebar-left", BannerWidth = 300, BannerHeight = 250, Name = "Left Sidebar", Description = "Vertical banner in the left sidebar", MaxBanners = 2, ExcludeFromLandingPage = false, DefaultCssClass = "ad-zone ad-sidebar-left mb-3", DisplayOrder = 20, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AdZone { Id = 3, Key = "sidebar-right", BannerWidth = 300, BannerHeight = 250, Name = "Right Sidebar", Description = "Vertical banner in the right sidebar", MaxBanners = 2, ExcludeFromLandingPage = false, DefaultCssClass = "ad-zone ad-sidebar-right mb-3", DisplayOrder = 30, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AdZone { Id = 4, Key = "content-top", BannerWidth = 728, BannerHeight = 90, Name = "Content Top", Description = "Banner at the top of the main content area", MaxBanners = 1, ExcludeFromLandingPage = true, DefaultCssClass = "ad-zone ad-content-top mb-4", DisplayOrder = 40, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AdZone { Id = 5, Key = "content-bottom", BannerWidth = 970, BannerHeight = 250, Name = "Content Bottom", Description = "Banner at the bottom of the main content area", MaxBanners = 1, ExcludeFromLandingPage = false, DefaultCssClass = "ad-zone ad-content-bottom mt-4", DisplayOrder = 50, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AdZone { Id = 6, Key = "footer", BannerWidth = 728, BannerHeight = 90, Name = "Footer", Description = "Banner in the footer area", MaxBanners = 3, ExcludeFromLandingPage = false, DefaultCssClass = "ad-zone ad-footer mt-4", DisplayOrder = 60, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AdZone { Id = 7, Key = "in-article", BannerWidth = 728, BannerHeight = 90, Name = "In-Article", Description = "Banner inserted between paragraphs in article content", MaxBanners = 1, ExcludeFromLandingPage = true, DefaultCssClass = "ad-zone ad-in-article my-4", DisplayOrder = 70, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
