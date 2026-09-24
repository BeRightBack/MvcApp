using System.ComponentModel.DataAnnotations;

namespace MvcApp.Module.Utility
{
    public class ToDoTask
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public ToDoTaskStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int Priority { get; set; }

        public int ProjectId { get; set; }

        public Project? Project { get; set; }

        public List<ToDoSubTask> ToDoSubTasks { get; set; } = [];
    }
}
