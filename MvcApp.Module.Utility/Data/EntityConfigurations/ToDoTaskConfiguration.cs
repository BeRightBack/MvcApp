using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MvcApp.Module.Utility.Data.EntityConfigurations
{
    public class ToDoTaskConfiguration : IEntityTypeConfiguration<ToDoTask>
    {
        public void Configure(EntityTypeBuilder<ToDoTask> entity)
        {
            entity.ToTable("ToDoTasks");
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description);
            entity.HasIndex(e => e.ProjectId);

            entity.HasMany(e => e.ToDoSubTasks)
                .WithOne(s => s.ToDoTask)
                .HasForeignKey(s => s.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
