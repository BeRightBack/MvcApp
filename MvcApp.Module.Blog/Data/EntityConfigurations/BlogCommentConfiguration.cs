using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Blog.Data.EntityConfigurations;

public class BlogCommentConfiguration : IEntityTypeConfiguration<BlogComment>
{
    public void Configure(EntityTypeBuilder<BlogComment> entity)
    {
        entity.ToTable("BlogComments");
        entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
        entity.Property(e => e.CreatedByUsername).HasMaxLength(50);
        entity.Property(e => e.GuestName).HasMaxLength(100);
        entity.Property(e => e.GuestEmail).HasMaxLength(200);
        entity.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasIndex(e => e.PostId);
        entity.HasIndex(e => e.IsApproved);
    }
}
