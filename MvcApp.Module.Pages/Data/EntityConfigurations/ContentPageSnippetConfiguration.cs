using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MvcApp.Core;

namespace MvcApp.Module.Pages.Data.EntityConfigurations;

public class ContentPageSnippetConfiguration : IEntityTypeConfiguration<ContentPageSnippet>
{
    public void Configure(EntityTypeBuilder<ContentPageSnippet> entity)
    {
        entity.ToTable("ContentPageSnippets");
        entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Content).IsRequired();
        entity.HasIndex(e => e.Category);
        entity.HasIndex(e => e.Name);
    }
}
