namespace MvcApp.Module.Utility.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetUserProjectsAsync(string userId);
        Task<Project?> GetProjectByIdAsync(int projectId, string userId);
        Task<Project> CreateProjectAsync(Project project);
        Task<Project?> UpdateProjectAsync(Project project, string userId);
        Task<bool> DeleteProjectAsync(int projectId, string userId);
    }
}
