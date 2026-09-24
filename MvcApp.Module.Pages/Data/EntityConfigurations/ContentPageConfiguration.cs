using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Pages.Data.EntityConfigurations;

public class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> entity)
    {
        entity.ToTable("ContentPages");
        entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(250).IsRequired();
        entity.Property(e => e.Body).IsRequired();
        entity.Property(e => e.CreatedByUsername).HasMaxLength(50).IsRequired();
        entity.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => e.IsPublished);
        entity.HasIndex(e => e.PublishedAt);
    }
}
