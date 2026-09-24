using Microsoft.EntityFrameworkCore;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Utility.Services
{
    public class ProjectService(IRepository<Project> projectRepo) : IProjectService
    {
        public async Task<List<Project>> GetUserProjectsAsync(string userId) =>
            await projectRepo.Query()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

        public async Task<Project?> GetProjectByIdAsync(int projectId, string userId) =>
            await projectRepo.Query()
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

        public async Task<Project> CreateProjectAsync(Project project)
        {
            project.User = null;
            project.CreatedAt = DateTime.UtcNow;
            await projectRepo.AddAsync(project);
            return project;
        }

        public async Task<Project?> UpdateProjectAsync(Project project, string userId)
        {
            var existing = await projectRepo.Query()
                .FirstOrDefaultAsync(p => p.Id == project.Id && p.UserId == userId);
            if (existing == null) return null;

            existing.Name = project.Name;
            existing.Description = project.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            await projectRepo.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteProjectAsync(int projectId, string userId)
        {
            var project = await projectRepo.Query()
                .Include(p => p.ToDoTasks)
                .ThenInclude(t => t.ToDoSubTasks)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
            if (project == null) return false;

            await projectRepo.DeleteAsync(project);
            return true;
        }
    }
}
