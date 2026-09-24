using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MvcApp.Module.Utility.Data.EntityConfigurations
{
    public class ToDoSubTaskConfiguration : IEntityTypeConfiguration<ToDoSubTask>
    {
        public void Configure(EntityTypeBuilder<ToDoSubTask> entity)
        {
            entity.ToTable("ToDoSubTasks");
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.TaskId);
        }
    }
}
