using System.ComponentModel.DataAnnotations;
using MvcApp.Core;

namespace MvcApp.Module.Utility
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public UserDetails? User { get; set; }

        public List<ToDoTask> ToDoTasks { get; set; } = [];
    }
}
