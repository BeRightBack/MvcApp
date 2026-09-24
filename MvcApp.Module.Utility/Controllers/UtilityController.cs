using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Module.Utility.Services;

namespace MvcApp.Module.Utility.Controllers
{
    [Authorize]
    [ModuleEnabledFilter("Utility")]
    public class UtilityController(
        IProjectService projectService,
        IToDoTaskService taskService,
        IToDoSubTaskService subTaskService,
        UserManager<UserDetails> userManager) : Controller
    {
        private string? UserId => userManager.GetUserId(User);

        // GET /todos
        [HttpGet("todos")]
        public async Task<IActionResult> Index()
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            var projects = await projectService.GetUserProjectsAsync(userId);
            return View(new ProjectIndexViewModel { Projects = projects });
        }

        // POST /projects/create
        [HttpPost("projects/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([FromForm] ProjectForm form)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            if (ModelState.IsValid)
            {
                await projectService.CreateProjectAsync(new Project
                {
                    Name = form.Name,
                    Description = form.Description ?? string.Empty,
                    UserId = userId
                });
                return RedirectToAction(nameof(Index));
            }
            var projects = await projectService.GetUserProjectsAsync(userId);
            return View("Index", new ProjectIndexViewModel { Projects = projects });
        }

        // POST /projects/{id}/edit
        [HttpPost("projects/{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(int id, [FromForm] ProjectForm form)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            if (ModelState.IsValid)
            {
                await projectService.UpdateProjectAsync(new Project
                {
                    Id = id,
                    Name = form.Name,
                    Description = form.Description ?? string.Empty
                }, userId);
            }
            return RedirectToAction(nameof(ProjectDetails), new { id });
        }

        // POST /projects/{id}/delete
        [HttpPost("projects/{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            await projectService.DeleteProjectAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        // GET /projects/{id}
        [HttpGet("projects/{id:int}")]
        public async Task<IActionResult> ProjectDetails(int id, [FromQuery] string? filter)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            var project = await projectService.GetProjectByIdAsync(id, userId);
            if (project == null) return NotFound();

            var tasks = await taskService.GetProjectTasksAsync(id, userId);
            var selected = (filter ?? "all").ToLowerInvariant();
            tasks = selected switch
            {
                "active" => tasks.Where(t => t.Status != ToDoTaskStatus.Completed && t.Status != ToDoTaskStatus.Cancelled).ToList(),
                "completed" => tasks.Where(t => t.Status == ToDoTaskStatus.Completed).ToList(),
                _ => tasks
            };

            return View(new ProjectDetailsViewModel { Project = project, Tasks = tasks, Filter = selected });
        }

        // POST /projects/{projectId}/tasks/create
        [HttpPost("projects/{projectId:int}/tasks/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask(int projectId, [FromForm] TaskForm form)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            if (ModelState.IsValid)
            {
                var project = await projectService.GetProjectByIdAsync(projectId, userId);
                if (project != null)
                {
                    await taskService.CreateTaskAsync(new ToDoTask
                    {
                        Title = form.Title,
                        Description = form.Description ?? string.Empty,
                        Status = form.Status,
                        Priority = form.Priority,
                        DueDate = form.DueDate,
                        ProjectId = projectId
                    });
                }
            }
            return RedirectToAction(nameof(ProjectDetails), new { id = projectId });
        }

        // POST /tasks/{id}/edit
        [HttpPost("tasks/{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTask(int id, [FromForm] TaskForm form)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            if (ModelState.IsValid)
            {
                await taskService.UpdateTaskAsync(new ToDoTask
                {
                    Id = id,
                    Title = form.Title,
                    Description = form.Description ?? string.Empty,
                    Status = form.Status,
                    Priority = form.Priority,
                    DueDate = form.DueDate
                }, userId);
            }
            return RedirectToAction(nameof(TaskDetails), new { id });
        }

        // POST /tasks/{id}/delete
        [HttpPost("tasks/{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            var task = await taskService.GetTaskByIdAsync(id, userId);
            await taskService.DeleteTaskAsync(id, userId);
            return RedirectToAction(nameof(ProjectDetails), new { id = task?.ProjectId ?? 0 });
        }

        // POST /tasks/{id}/status
        [HttpPost("tasks/{id:int}/status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTaskStatus(int id, [FromForm] ToDoTaskStatus status)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            var task = await taskService.GetTaskByIdAsync(id, userId);
            if (task != null)
            {
                await taskService.UpdateTaskAsync(new ToDoTask
                {
                    Id = id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = status,
                    Priority = task.Priority,
                    DueDate = task.DueDate
                }, userId);
            }
            return RedirectToAction(nameof(TaskDetails), new { id });
        }

        // GET /tasks/{id}
        [HttpGet("tasks/{id:int}")]
        public async Task<IActionResult> TaskDetails(int id)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            var task = await taskService.GetTaskByIdAsync(id, userId);
            if (task == null) return NotFound();

            var subtasks = await subTaskService.GetTaskSubTasksAsync(id, userId);
            return View(new TaskDetailsViewModel { Task = task, Subtasks = subtasks });
        }

        // POST /tasks/{id}/subtasks/create
        [HttpPost("tasks/{id:int}/subtasks/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubtask(int id, [FromForm] SubtaskForm form)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            if (ModelState.IsValid)
            {
                var task = await taskService.GetTaskByIdAsync(id, userId);
                if (task != null)
                {
                    await subTaskService.CreateSubTaskAsync(new ToDoSubTask
                    {
                        Title = form.Title,
                        TaskId = id
                    });
                }
            }
            return RedirectToAction(nameof(TaskDetails), new { id });
        }

        // POST /tasks/{id}/subtasks/{subtaskId}/toggle
        [HttpPost("tasks/{id:int}/subtasks/{subtaskId:int}/toggle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubtask(int id, int subtaskId)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            var subTask = await subTaskService.GetSubTaskByIdAsync(subtaskId, userId);
            if (subTask != null)
            {
                subTask.IsCompleted = !subTask.IsCompleted;
                await subTaskService.UpdateSubTaskAsync(subTask, userId);
            }
            return RedirectToAction(nameof(TaskDetails), new { id });
        }

        // POST /tasks/{id}/subtasks/{subtaskId}/delete
        [HttpPost("tasks/{id:int}/subtasks/{subtaskId:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubtask(int id, int subtaskId)
        {
            var userId = UserId;
            if (userId == null) return Challenge();
            await subTaskService.DeleteSubTaskAsync(subtaskId, userId);
            return RedirectToAction(nameof(TaskDetails), new { id });
        }
    }
}
