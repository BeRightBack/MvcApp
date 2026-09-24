using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Infrastructure.Data.EntityConfigurations;

public class VerificationRequestConfiguration : IEntityTypeConfiguration<VerificationRequest>
{
    public void Configure(EntityTypeBuilder<VerificationRequest> entity)
    {
        entity.ToTable("VerificationRequests");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.Filename).IsRequired();
        entity.Property(e => e.DisplayNumber).HasMaxLength(100);
        entity.Property(e => e.AdminNotes).HasMaxLength(500);
        entity.Property(e => e.ReviewedBy).HasMaxLength(100);

        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.Status);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
