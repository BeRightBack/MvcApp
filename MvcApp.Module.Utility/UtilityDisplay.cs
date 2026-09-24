namespace MvcApp.Module.Utility
{
    public static class UtilityDisplay
    {
        public static string StatusText(ToDoTaskStatus status) => status switch
        {
            ToDoTaskStatus.NotStarted => "Not Started",
            ToDoTaskStatus.InProgress => "In Progress",
            ToDoTaskStatus.OnHold => "On Hold",
            ToDoTaskStatus.Completed => "Completed",
            ToDoTaskStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };

        public static string StatusBadgeClass(ToDoTaskStatus status) => status switch
        {
            ToDoTaskStatus.NotStarted => "bg-secondary",
            ToDoTaskStatus.InProgress => "bg-primary",
            ToDoTaskStatus.OnHold => "bg-warning",
            ToDoTaskStatus.Completed => "bg-success",
            ToDoTaskStatus.Cancelled => "bg-danger",
            _ => "bg-secondary"
        };

        public static string PriorityText(int priority) => priority switch
        {
            2 => "High",
            1 => "Medium",
            _ => "Low"
        };

        public static string PriorityCardClass(int priority) => priority switch
        {
            2 => "priority-high",
            1 => "priority-medium",
            _ => "priority-low"
        };

        public static string PriorityBadgeClass(int priority) => priority switch
        {
            2 => "text-bg-danger",
            1 => "text-bg-warning",
            _ => "text-bg-info"
        };
    }
}
