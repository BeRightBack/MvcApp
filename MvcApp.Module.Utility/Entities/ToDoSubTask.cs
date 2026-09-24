using System.ComponentModel.DataAnnotations;

namespace MvcApp.Module.Utility
{
    public class ToDoSubTask
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public int TaskId { get; set; }

        public ToDoTask? ToDoTask { get; set; }
    }
}
