using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MvcApp.Module.Video.Data.EntityConfigurations;

public class VideoRoomConfiguration : IEntityTypeConfiguration<VideoRoom>
{
    public void Configure(EntityTypeBuilder<VideoRoom> entity)
    {
        entity.ToTable("VideoRooms");
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.HasMany(e => e.Messages)
            .WithOne(m => m.VideoRoom)
            .HasForeignKey(m => m.VideoRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasData(
            new VideoRoom { Id = 1, Name = "Main Lounge", Description = "The main video lounge — come say hello on camera", MaxSpots = 10, IsActive = true, CreatedAt = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc) },
            new VideoRoom { Id = 2, Name = "Dating Lounge", Description = "Meet new people face-to-face", MaxSpots = 10, IsActive = true, CreatedAt = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc) },
            new VideoRoom { Id = 3, Name = "VIP Lounge", Description = "Smaller, more intimate room", MaxSpots = 4, IsActive = true, CreatedAt = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
