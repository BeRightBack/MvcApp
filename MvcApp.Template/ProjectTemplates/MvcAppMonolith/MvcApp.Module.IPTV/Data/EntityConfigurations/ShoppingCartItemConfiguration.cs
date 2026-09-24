using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.IPTV.Data.EntityConfigurations;

public class ShoppingCartItemConfiguration : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> entity)
    {
        entity.ToTable("ShoppingCartItems");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.SubscriptionDetailId).IsRequired();
        entity.HasOne(e => e.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(e => e.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.SubscriptionDetail)
            .WithMany()
            .HasForeignKey(e => e.SubscriptionDetailId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.UserId);
    }
}
