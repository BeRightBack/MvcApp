using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Chat.Data.EntityConfigurations;

public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> entity)
    {
        entity.ToTable("ChatRooms");
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.HasMany(e => e.Messages)
            .WithOne(m => m.ChatRoom)
            .HasForeignKey(m => m.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
