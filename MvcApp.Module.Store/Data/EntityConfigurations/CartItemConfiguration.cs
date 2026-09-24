using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Store.Data.EntityConfigurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> entity)
    {
        entity.ToTable("CartItems");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        entity.Property(e => e.ProductName).HasMaxLength(200);
        entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        entity.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.UserId);
    }
}
