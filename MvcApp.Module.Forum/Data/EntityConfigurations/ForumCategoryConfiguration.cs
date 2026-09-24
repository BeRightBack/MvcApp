using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Forum.Data.EntityConfigurations;

public class ForumCategoryConfiguration : IEntityTypeConfiguration<ForumCategory>
{
    public void Configure(EntityTypeBuilder<ForumCategory> entity)
    {
        entity.ToTable("ForumCategories");
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.HasMany(e => e.Forums)
            .WithOne(f => f.Category)
            .HasForeignKey(f => f.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
