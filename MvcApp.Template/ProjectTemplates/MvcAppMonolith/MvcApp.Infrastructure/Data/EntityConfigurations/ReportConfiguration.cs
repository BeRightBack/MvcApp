using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Infrastructure.Data.EntityConfigurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> entity)
    {
        entity.ToTable("Reports");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.ReporterId).IsRequired();
        entity.Property(e => e.ReportedUserId).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(2000);
        entity.Property(e => e.AdminNotes).HasMaxLength(500);
        entity.Property(e => e.ReviewedBy).HasMaxLength(100);

        entity.HasIndex(e => e.ReportedUserId);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.CreatedAt);

        entity.HasOne(e => e.Reporter)
            .WithMany()
            .HasForeignKey(e => e.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ReportedUser)
            .WithMany()
            .HasForeignKey(e => e.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
