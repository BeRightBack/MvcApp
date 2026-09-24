using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Data.EntityConfigurations;

public class AdPlacementConfiguration : IEntityTypeConfiguration<AdPlacement>
{
    public void Configure(EntityTypeBuilder<AdPlacement> entity)
    {
        entity.ToTable("AdPlacements");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.ZoneId, e.PageSlug });
        entity.Property(e => e.PageSlug).HasMaxLength(250).IsRequired();
        entity.Property(e => e.CssClassOverride).HasMaxLength(200);
        entity.Property(e => e.WrapperTemplateOverride).HasMaxLength(500);
        entity.HasOne(e => e.Zone)
            .WithMany(z => z.Placements)
            .HasForeignKey(e => e.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
