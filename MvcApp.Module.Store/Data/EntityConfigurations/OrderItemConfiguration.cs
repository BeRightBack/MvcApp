using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Store.Data.EntityConfigurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.ToTable("OrderItems");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
        entity.Property(e => e.ProductImage).HasMaxLength(500);
        entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        entity.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
