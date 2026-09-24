using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Blog.Data.EntityConfigurations;

public class BlogTagConfiguration : IEntityTypeConfiguration<BlogTag>
{
    public void Configure(EntityTypeBuilder<BlogTag> entity)
    {
        entity.ToTable("BlogTags");
        entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        entity.HasIndex(e => e.Slug).IsUnique();
    }
}
