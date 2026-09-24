using Microsoft.EntityFrameworkCore;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Utility.Services
{
    public class ToDoTaskService(IRepository<ToDoTask> taskRepo) : IToDoTaskService
    {
        public async Task<List<ToDoTask>> GetProjectTasksAsync(int projectId, string userId) =>
            await taskRepo.Query()
                .Where(t => t.ProjectId == projectId && t.Project!.UserId == userId)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

        public async Task<ToDoTask?> GetTaskByIdAsync(int taskId, string userId) =>
            await taskRepo.Query()
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.Project!.UserId == userId);

        public async Task<ToDoTask> CreateTaskAsync(ToDoTask task)
        {
            task.Project = null;
            task.CreatedAt = DateTime.UtcNow;
            await taskRepo.AddAsync(task);
            return task;
        }

        public async Task<ToDoTask?> UpdateTaskAsync(ToDoTask task, string userId)
        {
            var existing = await taskRepo.Query()
                .FirstOrDefaultAsync(t => t.Id == task.Id && t.Project!.UserId == userId);
            if (existing == null) return null;

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Status = task.Status;
            existing.Priority = task.Priority;
            existing.DueDate = task.DueDate;

            if (task.Status == ToDoTaskStatus.Completed && !existing.CompletedAt.HasValue)
                existing.CompletedAt = DateTime.UtcNow;
            else if (task.Status != ToDoTaskStatus.Completed)
                existing.CompletedAt = null;

            await taskRepo.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteTaskAsync(int taskId, string userId)
        {
            var task = await taskRepo.Query()
                .Include(t => t.ToDoSubTasks)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.Project!.UserId == userId);
            if (task == null) return false;

            await taskRepo.DeleteAsync(task);
            return true;
        }
    }
}
