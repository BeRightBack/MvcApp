using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.IPTV.Data.EntityConfigurations;

public class IptvOrderConfiguration : IEntityTypeConfiguration<IptvOrder>
{
    public void Configure(EntityTypeBuilder<IptvOrder> entity)
    {
        entity.ToTable("IptvOrders");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasMaxLength(64);
        entity.Property(e => e.Status).HasMaxLength(50);
        entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");
        entity.HasIndex(e => e.UserId);
    }
}
