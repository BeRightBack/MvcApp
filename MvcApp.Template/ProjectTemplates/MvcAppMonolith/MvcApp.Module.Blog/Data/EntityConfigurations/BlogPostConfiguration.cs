using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Blog.Data.EntityConfigurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> entity)
    {
        entity.ToTable("BlogPosts");
        entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(250).IsRequired();
        entity.Property(e => e.Excerpt).HasMaxLength(500);
        entity.Property(e => e.FeaturedImagePath).HasMaxLength(500);
        entity.Property(e => e.CreatedByUsername).HasMaxLength(50).IsRequired();
        entity.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasMany(e => e.PostTags)
            .WithOne(pt => pt.Post)
            .HasForeignKey(pt => pt.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(e => e.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => e.IsPublished);
        entity.HasIndex(e => e.PublishedAt);
    }
}
