using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Forum.Data.EntityConfigurations;

public class ForumConfiguration : IEntityTypeConfiguration<MvcApp.Core.Forum>
{
    public void Configure(EntityTypeBuilder<MvcApp.Core.Forum> entity)
    {
        entity.ToTable("Forums");
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.LastPostByUsername).HasMaxLength(50);
        entity.HasMany(e => e.Threads)
            .WithOne(t => t.Forum)
            .HasForeignKey(t => t.ForumId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
