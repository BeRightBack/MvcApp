using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Store.Data.EntityConfigurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("Orders");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
        entity.Property(e => e.ShippingCost).HasColumnType("decimal(18,2)");
        entity.Property(e => e.Tax).HasColumnType("decimal(18,2)");
        entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        entity.Property(e => e.ShippingName).HasMaxLength(200);
        entity.Property(e => e.ShippingAddress).HasMaxLength(500);
        entity.Property(e => e.ShippingCity).HasMaxLength(100);
        entity.Property(e => e.ShippingState).HasMaxLength(100);
        entity.Property(e => e.ShippingPostalCode).HasMaxLength(20);
        entity.Property(e => e.ShippingCountry).HasMaxLength(100);
        entity.Property(e => e.PaymentMethod).HasMaxLength(50);
        entity.Property(e => e.Notes).HasMaxLength(1000);
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.PaymentStatus).HasConversion<int>();
        entity.HasMany(e => e.Items)
            .WithOne(e => e.Order)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.UserId);
    }
}
