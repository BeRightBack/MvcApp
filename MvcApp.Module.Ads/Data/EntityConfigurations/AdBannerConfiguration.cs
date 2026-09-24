using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Data.EntityConfigurations;

public class AdBannerConfiguration : IEntityTypeConfiguration<AdBanner>
{
    public void Configure(EntityTypeBuilder<AdBanner> entity)
    {
        entity.ToTable("AdBanners");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.ZoneId, e.Weight });
        entity.HasIndex(e => new { e.ZoneId, e.IsActive, e.StartDate, e.EndDate });
        entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Content).HasMaxLength(4000);
        entity.Property(e => e.TargetUrl).HasMaxLength(500);
        entity.Property(e => e.AltText).HasMaxLength(200);
        entity.Property(e => e.CssClass).HasMaxLength(200);
        entity.Property(e => e.TargetRoles).HasMaxLength(500);
        entity.Property(e => e.TargetCultures).HasMaxLength(200);
        entity.HasOne(e => e.Zone)
            .WithMany(z => z.Banners)
            .HasForeignKey(e => e.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
