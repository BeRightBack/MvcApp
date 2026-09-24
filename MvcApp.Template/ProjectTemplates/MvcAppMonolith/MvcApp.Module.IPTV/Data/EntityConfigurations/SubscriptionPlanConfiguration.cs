using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.IPTV.Data.EntityConfigurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> entity)
    {
        entity.ToTable("SubscriptionPlans");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).HasMaxLength(200);
        entity.Property(e => e.DescriptionShort).HasMaxLength(500);
        entity.Property(e => e.Description).HasColumnType("text");
        entity.HasMany(e => e.SubscriptionDetails)
            .WithOne(e => e.SubscriptionPlan)
            .HasForeignKey(e => e.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubscriptionDetailConfiguration : IEntityTypeConfiguration<SubscriptionDetail>
{
    public void Configure(EntityTypeBuilder<SubscriptionDetail> entity)
    {
        entity.ToTable("SubscriptionDetails");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
    }
}
