using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.IPTV.Data.EntityConfigurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> entity)
    {
        entity.ToTable("Subscriptions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("pending");
        entity.Property(e => e.UserCode).HasMaxLength(200);
        entity.Property(e => e.Password).HasMaxLength(200);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(e => e.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.Status);
    }
}
