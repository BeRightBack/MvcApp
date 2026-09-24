using System.ComponentModel.DataAnnotations;

namespace MvcApp.Module.Utility
{
    public class ProjectIndexViewModel
    {
        public List<Project> Projects { get; set; } = [];
    }

    public class ProjectDetailsViewModel
    {
        public Project Project { get; set; } = new();
        public List<ToDoTask> Tasks { get; set; } = [];
        public string Filter { get; set; } = "all";
    }

    public class TaskDetailsViewModel
    {
        public ToDoTask Task { get; set; } = new();
        public List<ToDoSubTask> Subtasks { get; set; } = [];
    }

    public class ProjectForm
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class TaskForm
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ToDoTaskStatus Status { get; set; }

        public int Priority { get; set; }

        public DateTime? DueDate { get; set; }
    }

    public class SubtaskForm
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
    }
}
