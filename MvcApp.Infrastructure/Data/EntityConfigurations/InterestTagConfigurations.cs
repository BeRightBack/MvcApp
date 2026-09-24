using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Infrastructure.Data.EntityConfigurations;

public class InterestTagConfiguration : IEntityTypeConfiguration<InterestTag>
{
    public void Configure(EntityTypeBuilder<InterestTag> entity)
    {
        entity.ToTable("InterestTags");
        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        entity.HasIndex(e => new { e.Name, e.Category }).IsUnique();
    }
}

public class UserInterestTagConfiguration : IEntityTypeConfiguration<UserInterestTag>
{
    public void Configure(EntityTypeBuilder<UserInterestTag> entity)
    {
        entity.ToTable("UserInterestTags");
        entity.HasKey(e => new { e.UserId, e.TagId });
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Tag)
            .WithMany(t => t.UserTags)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
