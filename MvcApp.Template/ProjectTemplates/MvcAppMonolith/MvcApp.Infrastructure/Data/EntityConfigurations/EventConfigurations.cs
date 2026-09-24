using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Infrastructure.Data.EntityConfigurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> entity)
    {
        entity.ToTable("Events");
        entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(2000);
        entity.Property(e => e.City).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
        entity.Property(e => e.ImageUrl).HasMaxLength(500);
        entity.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.Creator)
            .WithMany()
            .HasForeignKey(e => e.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.EventDate);
        entity.HasIndex(e => e.City);
        entity.HasIndex(e => e.CreatorId);
    }
}

public class EventCategoryConfiguration : IEntityTypeConfiguration<EventCategory>
{
    public void Configure(EntityTypeBuilder<EventCategory> entity)
    {
        entity.ToTable("EventCategories");
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Icon).HasMaxLength(50);
    }
}

public class EventRSVPConfiguration : IEntityTypeConfiguration<EventRSVP>
{
    public void Configure(EntityTypeBuilder<EventRSVP> entity)
    {
        entity.ToTable("EventRSVPs");
        entity.HasOne(e => e.Event)
            .WithMany(e => e.RSVPs)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();
    }
}
