using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Web.Areas.Admin.Models.UserViewModels;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly IRepository<UserRole> _roleRepository;
        private readonly IAuditService _auditService;

        public RoleController(IRepository<UserRole> roleRepository, IAuditService auditService)
        {
            _roleRepository = roleRepository;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleRepository.Query().ToListAsync();

            if (roles == null)
            {
                return NotFound();
            }

            var viewModel = roles.Select(r => new RoleViewModel
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description!
            });
            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var role = await _roleRepository.GetFirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
            {
                return NotFound();
            }
            var viewModel = new RoleViewModel
            {
                Name = role.Name!,
                Description = role.Description!
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var role = new UserRole
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description
                };

                await _roleRepository.AddAsync(role);
                await _auditService.LogAsync("Create", "Role", role.Id, $"Created role {role.Name}");
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _roleRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            var viewModel = new RoleViewModel
            {
                Id = user.Id,
                Name = user.Name!,
                Description = user.Description!
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, RoleViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var role = await _roleRepository.GetFirstOrDefaultAsync(u => u.Id == id);
                    if (role == null)
                    {
                        return NotFound();
                    }
                    role.Name = viewModel.Name;
                    role.Description = viewModel.Description;
                    await _roleRepository.UpdateAsync(role);
                    await _auditService.LogAsync("Edit", "Role", role.Id, $"Updated role {role.Name}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoleExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            if (role == null)
            {
                return NotFound();
            }

            var viewModel = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description!
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await _roleRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            if (role == null)
            {
                return NotFound();
            }

            await _roleRepository.DeleteUserAsync(role.Id);
            await _auditService.LogAsync("Delete", "Role", role.Id, $"Deleted role {role.Name}");

            return RedirectToAction(nameof(Index));
        }

        private bool RoleExists(string id)
        {
            return _roleRepository.GetFirstOrDefaultAsync(u => u.Id == id) != null;
        }

    }
}
