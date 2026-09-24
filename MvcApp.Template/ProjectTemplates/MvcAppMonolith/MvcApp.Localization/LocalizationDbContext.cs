using Microsoft.EntityFrameworkCore;
using MvcApp.Core;

namespace MvcApp.Localization
{
    public class LocalizationDbContext : DbContext
    {
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<StringResource> StringResources { get; set; }

        public LocalizationDbContext(DbContextOptions<LocalizationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Language>().ToTable("Languages");
            modelBuilder.Entity<StringResource>().ToTable("StringResources");

            modelBuilder.Entity<Language>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<StringResource>(entity =>
            {
                entity.Property(e => e.Name)
                      .HasMaxLength(500)
                      .UseCollation("utf8mb4_general_ci"); // case-insensitive

                entity.Property(e => e.Value).HasColumnType("TEXT");

                entity.HasOne(d => d.Language)
                    .WithMany(p => p.StringResources)
                    .HasForeignKey(d => d.LanguageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.LanguageId, e.Name });
            });

        }


    }
}