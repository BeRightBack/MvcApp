using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Forum.Data.EntityConfigurations;

public class ForumPostConfiguration : IEntityTypeConfiguration<ForumPost>
{
    public void Configure(EntityTypeBuilder<ForumPost> entity)
    {
        entity.ToTable("ForumPosts");
        entity.Property(e => e.CreatedByUsername).HasMaxLength(50).IsRequired();
        entity.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasIndex(e => e.ThreadId);
        entity.HasIndex(e => e.CreatedAt);
    }
}
