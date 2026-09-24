using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Web.Areas.Admin.Models.UserViewModels;

namespace MvcApp.Web.Areas.Admin.Controllers
{

    //[Authorize(Roles = "Admins")]
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleAdminController : Controller
    {
        private RoleManager<UserRole> roleManager;
        private UserManager<UserDetails> userManager;
        private readonly IRepository<UserRole> _roleRepository;
        private readonly IAuditService _auditService;

        public RoleAdminController(
         RoleManager<UserRole> roleMgr,
         UserManager<UserDetails> userMrg,
         IRepository<UserRole> roleRepository,
         IAuditService auditService
         )
        {
            roleManager = roleMgr;
            userManager = userMrg;
            _roleRepository = roleRepository;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await roleManager.Roles.ToListAsync();
            var roleWithUsers = new List<RoleWithUsersViewModel>();

            var users = await userManager.Users.ToListAsync(); // Load users into memory

            foreach (var role in roles)
            {
                var usersInRole = new List<UserProfile>();
                foreach (var user in users)
                {
                    if (await userManager.IsInRoleAsync(user, role.Name!))
                    {
                        usersInRole.Add(user);
                    }
                }

                roleWithUsers.Add(new RoleWithUsersViewModel
                {
                    Role = role,
                    Users = usersInRole
                });
            }

            return View(roleWithUsers);
        }

        public async Task<IActionResult> Create(string searchQuery = "", int pageNumber = 1, int pageSize = 10)
        {
            var usersQuery = userManager.Users
                .Where(u => string.IsNullOrEmpty(searchQuery) || u.UserName!.Contains(searchQuery));

            var totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new RoleViewModel
            {
                NonMembers = users
            };

            ViewData["SearchQuery"] = searchQuery;
            ViewData["PageNumber"] = pageNumber;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalUsers / (double)pageSize);
            return View(viewModel);
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

                IdentityResult result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    AddErrorsFromResult(result);
                    return View(viewModel);
                }

                await _auditService.LogAsync("Create", "Role", role.Id, $"Created role {role.Name} with users");

                foreach (string userId in viewModel.IdsToAdd ?? new string[] { })
                {
                    UserDetails? user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, role.Name);
                        if (!result.Succeeded)
                        {
                            AddErrorsFromResult(result);
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            UserRole? role = await roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    await _auditService.LogAsync("Delete", "Role", id, $"Deleted role {role.Name}");
                    return RedirectToAction("Index");
                }
                else
                {
                    AddErrorsFromResult(result);
                }
            }
            else
            {
                ModelState.AddModelError("", "No role found");
            }
            return View("Index", roleManager.Roles);
        }

        public async Task<IActionResult> Edit(string id, string searchQuery = "", int pageNumber = 1, int pageSize = 10)
        {
            UserRole? role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usersQuery = userManager.Users
                .Where(u => string.IsNullOrEmpty(searchQuery) || u.UserName!.Contains(searchQuery));

            var totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<UserDetails> members = new List<UserDetails>();
            List<UserDetails> nonMembers = new List<UserDetails>();

            foreach (UserDetails user in users)
            {
                var list = await userManager.IsInRoleAsync(user, role.Name!)
                    ? members : nonMembers;
                list.Add(user);
            }

            var viewModel = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description!,
                Members = members,
                NonMembers = nonMembers
            };

            ViewData["SearchQuery"] = searchQuery;
            ViewData["PageNumber"] = pageNumber;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalUsers / (double)pageSize);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RoleViewModel model)
        {
            ViewData["TotalPages"] = 1;
            ViewData["PageNumber"] = 1;
            ViewData["SearchQuery"] = "";

            if (ModelState.IsValid)
            {
                UserRole? role = await roleManager.FindByIdAsync(model.Id);
                if (role == null)
                {
                    return NotFound();
                }

                role.Name = model.Name;
                role.Description = model.Description;

                IdentityResult result = await roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    AddErrorsFromResult(result);
                    return View(model);
                }

                await _auditService.LogAsync("Edit", "Role", role.Id, $"Updated role {role.Name} — added/removed members");

                foreach (string userId in model.IdsToAdd ?? new string[] { })
                {
                    UserDetails? user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, model.Name);
                        if (!result.Succeeded)
                        {
                            AddErrorsFromResult(result);
                        }
                    }
                }

                foreach (string userId in model.IdsToDelete ?? new string[] { })
                {
                    UserDetails? user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.RemoveFromRoleAsync(user, model.Name);
                        if (!result.Succeeded)
                        {
                            AddErrorsFromResult(result);
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }


        private void AddErrorsFromResult(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
    }
}
