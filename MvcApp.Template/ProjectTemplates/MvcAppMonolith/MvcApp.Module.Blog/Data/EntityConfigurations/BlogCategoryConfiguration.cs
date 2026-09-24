using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Blog.Data.EntityConfigurations;

public class BlogCategoryConfiguration : IEntityTypeConfiguration<BlogCategory>
{
    public void Configure(EntityTypeBuilder<BlogCategory> entity)
    {
        entity.ToTable("BlogCategories");
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(150).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.HasIndex(e => e.Slug).IsUnique();
    }
}
