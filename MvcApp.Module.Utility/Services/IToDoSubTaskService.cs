namespace MvcApp.Module.Utility.Services
{
    public interface IToDoSubTaskService
    {
        Task<List<ToDoSubTask>> GetTaskSubTasksAsync(int taskId, string userId);
        Task<ToDoSubTask?> GetSubTaskByIdAsync(int subTaskId, string userId);
        Task<ToDoSubTask> CreateSubTaskAsync(ToDoSubTask subTask);
        Task<ToDoSubTask?> UpdateSubTaskAsync(ToDoSubTask subTask, string userId);
        Task<bool> DeleteSubTaskAsync(int subTaskId, string userId);
    }
}
