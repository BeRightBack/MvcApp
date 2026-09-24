namespace MvcApp.Module.Utility.Services
{
    public interface IToDoTaskService
    {
        Task<List<ToDoTask>> GetProjectTasksAsync(int projectId, string userId);
        Task<ToDoTask?> GetTaskByIdAsync(int taskId, string userId);
        Task<ToDoTask> CreateTaskAsync(ToDoTask task);
        Task<ToDoTask?> UpdateTaskAsync(ToDoTask task, string userId);
        Task<bool> DeleteTaskAsync(int taskId, string userId);
    }
}
