using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class UserController : Controller
    {
        private readonly IRepository<UserDetails> _userRepository;
        private readonly IAuditService _auditService;

        public UserController(IRepository<UserDetails> userRepository, IAuditService auditService)
        {
            _userRepository = userRepository;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string searchQuery = "", int pageNumber = 1, int pageSize = 10)
        {
            // Ensure IQueryable for EF Core async operations
            var usersQuery = _userRepository.GetAll()
                .AsQueryable()
                .Where(u => string.IsNullOrEmpty(searchQuery) || u.UserName!.Contains(searchQuery) || u.Email!.Contains(searchQuery));

            var totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new UserIndexViewModel
            {
                Users = users,
                SearchQuery = searchQuery,
                PageNumber = pageNumber,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = new UserDetails
                {
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    UserName = viewModel.UserName,
                    NormalizedUserName = viewModel.UserName!.ToUpperInvariant(),
                    Email = viewModel.Email,
                    NormalizedEmail = viewModel.Email!.ToUpperInvariant(),
                    EmailConfirmed = viewModel.EmailConfirmed,
                    PhoneNumber = viewModel.PhoneNumber,
                    PhoneNumberConfirmed = viewModel.PhoneNumberConfirmed,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                // Hash password after creating the instance
                var hasher = new PasswordHasher<UserDetails>();
                user.PasswordHash = hasher.HashPassword(user, viewModel.Password!);

                await _userRepository.AddAsync(user);
                await _auditService.LogAsync("Create", "User", user.Id, $"Created user {user.UserName}");
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

            var user = await _userRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            var viewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                KnownAs = user.KnownAs,
                Introduction = user.Introduction,
                LookingFor = user.LookingFor,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth == DateTime.MinValue ? null : user.DateOfBirth,
                City = user.City,
                Country = user.Country,
                IsProfileComplete = user.IsProfileComplete
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userRepository.GetFirstOrDefaultAsync(u => u.Id == id);
                    if (user == null)
                    {
                        return NotFound();
                    }
                    user.FirstName = viewModel.FirstName;
                    user.LastName = viewModel.LastName;
                    user.UserName = viewModel.UserName;
                    user.NormalizedUserName = viewModel.UserName!.ToUpperInvariant();
                    user.Email = viewModel.Email;
                    user.NormalizedEmail = viewModel.Email!.ToUpperInvariant();
                    user.EmailConfirmed = viewModel.EmailConfirmed;
                    user.PhoneNumber = viewModel.PhoneNumber;
                    user.PhoneNumberConfirmed = viewModel.PhoneNumberConfirmed;
                    user.KnownAs = viewModel.KnownAs ?? "";
                    user.Introduction = viewModel.Introduction ?? "";
                    user.LookingFor = viewModel.LookingFor ?? "";
                    user.Gender = viewModel.Gender ?? "";
                    if (viewModel.DateOfBirth.HasValue)
                        user.DateOfBirth = viewModel.DateOfBirth.Value;
                    user.City = viewModel.City ?? "";
                    user.Country = viewModel.Country ?? "";
                    user.IsProfileComplete = viewModel.IsProfileComplete;

                    if (!string.IsNullOrEmpty(viewModel.Password))
                    {
                        var hasher = new PasswordHasher<UserDetails>();
                        user.PasswordHash = hasher.HashPassword(user, viewModel.Password);
                    }

                    await _userRepository.UpdateAsync(user);
                    await _auditService.LogAsync("Edit", "User", user.Id, $"Updated user {user.UserName}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await UserExistsAsync(viewModel.Id))
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

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            await _userRepository.DeleteUserAsync(user.Id);
            await _auditService.LogAsync("Delete", "User", user.Id, $"Deleted user {user.UserName}");

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExistsAsync(string id)
        {
            var user = await _userRepository.GetFirstOrDefaultAsync(u => u.Id == id);
            return user != null;
        }
    }
}