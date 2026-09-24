using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Store.Data.EntityConfigurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Products");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(250).IsRequired();
        entity.Property(e => e.ShortDescription).HasMaxLength(500);
        entity.Property(e => e.ImageUrl).HasMaxLength(500);
        entity.Property(e => e.AdditionalImages).HasMaxLength(2000);
        entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        entity.Property(e => e.ComparePrice).HasColumnType("decimal(18,2)");
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasOne(e => e.Category)
            .WithMany(e => e.Products)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
