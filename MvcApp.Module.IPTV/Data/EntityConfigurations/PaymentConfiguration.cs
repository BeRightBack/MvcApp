using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.IPTV.Data.EntityConfigurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.ToTable("Payments");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.PaymentAmount).HasColumnType("decimal(10, 2)");
        entity.HasIndex(e => e.UserId);
    }
}
