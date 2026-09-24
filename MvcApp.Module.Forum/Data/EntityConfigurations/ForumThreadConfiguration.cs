using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Forum.Data.EntityConfigurations;

public class ForumThreadConfiguration : IEntityTypeConfiguration<ForumThread>
{
    public void Configure(EntityTypeBuilder<ForumThread> entity)
    {
        entity.ToTable("ForumThreads");
        entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        entity.Property(e => e.CreatedByUsername).HasMaxLength(50).IsRequired();
        entity.Property(e => e.LastPostByUsername).HasMaxLength(50);
        entity.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasMany(e => e.Posts)
            .WithOne(p => p.Thread)
            .HasForeignKey(p => p.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.ForumId);
        entity.HasIndex(e => e.IsPinned);
        entity.HasIndex(e => e.CreatedAt);
    }
}
