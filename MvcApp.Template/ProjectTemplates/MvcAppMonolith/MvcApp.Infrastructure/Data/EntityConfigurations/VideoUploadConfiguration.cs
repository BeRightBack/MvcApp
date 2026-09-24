using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Infrastructure.Data.EntityConfigurations;

public class VideoUploadConfiguration : IEntityTypeConfiguration<VideoUpload>
{
    public void Configure(EntityTypeBuilder<VideoUpload> entity)
    {
        entity.ToTable("VideoUploads");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(2000);
        entity.Property(e => e.Filename).IsRequired();
        entity.Property(e => e.ThumbnailFilename).HasMaxLength(200);
        entity.Property(e => e.UserId).IsRequired();

        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.Category);
        entity.HasIndex(e => e.IsApproved);
        entity.HasIndex(e => e.UploadedAt);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
