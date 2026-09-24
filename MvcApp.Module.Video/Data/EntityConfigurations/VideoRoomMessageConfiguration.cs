using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MvcApp.Module.Video.Data.EntityConfigurations;

public class VideoRoomMessageConfiguration : IEntityTypeConfiguration<VideoRoomMessage>
{
    public void Configure(EntityTypeBuilder<VideoRoomMessage> entity)
    {
        entity.ToTable("VideoRoomMessages");
        entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
        entity.Property(e => e.SenderUsername).HasMaxLength(50);
        entity.HasOne(e => e.Sender)
            .WithMany()
            .HasForeignKey(e => e.SenderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
