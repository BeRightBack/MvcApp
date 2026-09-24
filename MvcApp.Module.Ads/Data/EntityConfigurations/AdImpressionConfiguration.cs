using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Data.EntityConfigurations;

public class AdImpressionConfiguration : IEntityTypeConfiguration<AdImpression>
{
    public void Configure(EntityTypeBuilder<AdImpression> entity)
    {
        entity.ToTable("AdImpressions");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.BannerId, e.CreatedAt });
        entity.HasIndex(e => new { e.PageSlug, e.CreatedAt });
        entity.Property(e => e.PageSlug).HasMaxLength(250);
        entity.Property(e => e.ZoneKey).HasMaxLength(100);
        entity.Property(e => e.IpHash).HasMaxLength(45);
        entity.Property(e => e.UserAgent).HasMaxLength(500);
        entity.Property(e => e.Culture).HasMaxLength(10);
        entity.Property(e => e.UserId).HasMaxLength(450);
        entity.HasOne(e => e.Banner)
            .WithMany(b => b.Impressions)
            .HasForeignKey(e => e.BannerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
