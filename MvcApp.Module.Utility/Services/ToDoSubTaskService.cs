using Microsoft.EntityFrameworkCore;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Utility.Services
{
    public class ToDoSubTaskService(IRepository<ToDoSubTask> subTaskRepo) : IToDoSubTaskService
    {
        public async Task<List<ToDoSubTask>> GetTaskSubTasksAsync(int taskId, string userId) =>
            await subTaskRepo.Query()
                .Where(s => s.TaskId == taskId && s.ToDoTask!.Project!.UserId == userId)
                .OrderBy(s => s.IsCompleted)
                .ThenByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<ToDoSubTask?> GetSubTaskByIdAsync(int subTaskId, string userId) =>
            await subTaskRepo.Query()
                .FirstOrDefaultAsync(s => s.Id == subTaskId && s.ToDoTask!.Project!.UserId == userId);

        public async Task<ToDoSubTask> CreateSubTaskAsync(ToDoSubTask subTask)
        {
            subTask.ToDoTask = null;
            subTask.CreatedAt = DateTime.UtcNow;
            await subTaskRepo.AddAsync(subTask);
            return subTask;
        }

        public async Task<ToDoSubTask?> UpdateSubTaskAsync(ToDoSubTask subTask, string userId)
        {
            var existing = await subTaskRepo.Query()
                .FirstOrDefaultAsync(s => s.Id == subTask.Id && s.ToDoTask!.Project!.UserId == userId);
            if (existing == null) return null;

            existing.Title = subTask.Title;
            existing.IsCompleted = subTask.IsCompleted;
            if (subTask.IsCompleted && !existing.CompletedAt.HasValue)
                existing.CompletedAt = DateTime.UtcNow;
            else if (!subTask.IsCompleted)
                existing.CompletedAt = null;

            await subTaskRepo.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteSubTaskAsync(int subTaskId, string userId)
        {
            var subTask = await subTaskRepo.Query()
                .FirstOrDefaultAsync(s => s.Id == subTaskId && s.ToDoTask!.Project!.UserId == userId);
            if (subTask == null) return false;

            await subTaskRepo.DeleteAsync(subTask);
            return true;
        }
    }
}
