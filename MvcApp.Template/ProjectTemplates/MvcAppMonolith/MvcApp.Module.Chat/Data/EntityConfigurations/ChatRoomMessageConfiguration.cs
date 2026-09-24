using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Chat.Data.EntityConfigurations;

public class ChatRoomMessageConfiguration : IEntityTypeConfiguration<ChatRoomMessage>
{
    public void Configure(EntityTypeBuilder<ChatRoomMessage> entity)
    {
        entity.ToTable("ChatRoomMessages");
        entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
        entity.Property(e => e.SenderUsername).HasMaxLength(50);
        entity.HasOne(e => e.Sender)
            .WithMany()
            .HasForeignKey(e => e.SenderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
