using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Infrastructure.Data.EntityConfigurations;

public class SeedManifestConfiguration : IEntityTypeConfiguration<SeedManifest>
{
    public void Configure(EntityTypeBuilder<SeedManifest> entity)
    {
        entity.ToTable("SeedManifest");
        entity.Property(e => e.PackName).HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityType).HasMaxLength(400).IsRequired();
        entity.Property(e => e.EntityKey).HasMaxLength(512).IsRequired();
        entity.Property(e => e.AppliedBy).HasMaxLength(255);
        entity.HasIndex(e => new { e.PackName, e.EntityType });
    }
}
