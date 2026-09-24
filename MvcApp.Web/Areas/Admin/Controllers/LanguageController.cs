using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Localization;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LanguageController(LocalizationDbContext context, IAuditService auditService) : Controller
    {
        // GET: Admin/Language
        public async Task<IActionResult> Index()
        {
            var languages = await context.Languages.ToListAsync();
            var viewModel = languages.Select(l => new LanguageViewModel(l)).ToList();
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var language = await context.Languages.FindAsync(id.Value);
            if (language == null)
            {
                return NotFound();
            }

            return View(new LanguageViewModel(language));
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LanguageViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var language = new Language { Name = viewModel.Name, Culture = viewModel.Culture };
                context.Languages.Add(language);
                await context.SaveChangesAsync();
                await auditService.LogAsync("Create", "Language", language.Id.ToString(), $"Created language {language.Name}");
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var language = await context.Languages.FindAsync(id);
            if (language == null)
            {
                return NotFound();
            }
            return View(new LanguageViewModel(language));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Culture")] Language language)
        {
            if (id != language.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    context.Languages.Update(language);
                    await context.SaveChangesAsync();
                    await auditService.LogAsync("Edit", "Language", id.ToString(), $"Updated language {language.Name}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await LanguageExists(language.Id))
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
            return View(language);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var language = await context.Languages.FindAsync(id.Value);
            if (language == null)
            {
                return NotFound();
            }

            return View(new LanguageViewModel(language));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var language = await context.Languages.FindAsync(id);
            if (language != null)
            {
                context.Languages.Remove(language);
                await context.SaveChangesAsync();
                await auditService.LogAsync("Delete", "Language", id.ToString(), $"Deleted language {language.Name}");
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> LanguageExists(int id)
        {
            return await context.Languages.AnyAsync(e => e.Id == id);
        }
    }
}
