using DeepL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Localization;
using MvcApp.Localization.Custom;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LocalizationController : Controller
    {
        private readonly LocalizationDbContext _context;
        private readonly Translator translator;
        private readonly IConfiguration? Configuration;
        private readonly IAuditService _auditService;

        public LocalizationController(LocalizationDbContext context, Translator translator, IConfiguration Configuration, IAuditService auditService)
        {
            _context = context;
            this.translator = translator;
            this.Configuration = Configuration;
            _auditService = auditService;
        }

        public string SourceLang => Configuration?["DeepLConfig:SourceLang"]!;

        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 15)
        {
            // Ensure page and pageSize are positive and greater than zero
            page = Math.Max(page, 1);
            pageSize = Math.Max(pageSize, 1);

            var stringResourceQuery = from s in _context.StringResources select s;

            // Filter based on search string
            if (!string.IsNullOrEmpty(searchString))
            {
                stringResourceQuery = stringResourceQuery.Where(s => s.Name!.Contains(searchString) || s.Value!.Contains(searchString));
            }

            // Get total count of filtered results
            var totalCount = await stringResourceQuery.CountAsync();

            // Calculate number of pages
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Ensure page number is within valid range
            if (page < 1)
            {
                page = 1;
            }
            else if (page > totalPages)
            {
                page = totalPages;
            }

            // Select only the requested page
            var stringResources = await stringResourceQuery
                .OrderBy(sr => sr.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sr => new LocalizationViewModel
                {
                    Id = sr.Id,
                    Name = sr.Name,
                    Value = sr.Value,
                    LanguageName = sr.Language!.Name,
                })
                .ToListAsync();

            // Add paging information to ViewBag for use in view
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount!;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString;

            return View(stringResources);
        }


        // GET: Admin/Localization/Create
        public IActionResult Create()
        {
            var viewModel = new StringResourceViewModel();
            return View(viewModel);
        }

        // POST: Admin/Localization/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StringResourceViewModel stringResourceViewModel)
        {
            if (ModelState.IsValid)
            {
                var stringResource = new StringResource
                {
                    LanguageId = stringResourceViewModel.LanguageId,
                    Name = stringResourceViewModel.Name,
                    Value = stringResourceViewModel.Value
                };

                _context.Add(stringResource);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Create", "StringResource", stringResource.Id.ToString(), $"Created resource '{stringResource.Name}'");

                return RedirectToAction(nameof(Index));
            }
            return View(stringResourceViewModel);
        }

        // GET: Admin/Localization/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var stringResource = await _context.StringResources.FindAsync(id);

            if (stringResource == null)
            {
                return NotFound();
            }

            var viewModel = new StringResourceViewModel
            {
                Id = stringResource.Id,
                LanguageId = stringResource.LanguageId ?? 0, // Fix for CS8629
                Name = stringResource.Name,
                Value = stringResource.Value
            };

            return View(viewModel);
        }

        // POST: Admin/Localization/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StringResourceViewModel stringResourceViewModel)
        {
            if (ModelState.IsValid)
            {
                var stringResource = new StringResource
                {
                    Id = stringResourceViewModel.Id,
                    LanguageId = stringResourceViewModel.LanguageId,
                    Name = stringResourceViewModel.Name,
                    Value = stringResourceViewModel.Value
                };
                try
                {
                    _context.Update(stringResource);
                    await _context.SaveChangesAsync();
                    await _auditService.LogAsync("Edit", "StringResource", stringResource.Id.ToString(), $"Updated resource '{stringResource.Name}'");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StringResourceExists(stringResource.Id))
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
            return View(stringResourceViewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stringResource = await _context.StringResources
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stringResource == null)
            {
                return NotFound();
            }

            var viewModel = new StringResourceViewModel
            {
                Id = stringResource.Id,
                LanguageId = stringResource.LanguageId ?? 0, // Fix for CS8629
                Name = stringResource.Name,
                Value = stringResource.Value
            };

            return View(viewModel);
        }

        // GET: Admin/Localization/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stringResource = await _context.StringResources
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stringResource == null)
            {
                return NotFound();
            }

            var viewModel = new StringResourceViewModel
            {
                Id = stringResource.Id,
                LanguageId = stringResource.LanguageId ?? 0, // Fix for CS8629
                Name = stringResource.Name,
                Value = stringResource.Value
            };

            return View(viewModel);
        }

        // POST: Admin/Localization/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stringResourceViewModel = await _context.StringResources
                .Where(sr => sr.Id == id)
                .Select(sr => new StringResourceViewModel
                {
                    Id = sr.Id,
                    LanguageId = sr.LanguageId ?? 0, // Fix for CS8629
                    Name = sr.Name,
                    Value = sr.Value

                    // Map other properties as needed
                })
                .FirstOrDefaultAsync();

            if (stringResourceViewModel == null)
            {
                return NotFound();
            }

            _context.StringResources.Remove(stringResourceViewModel.ToEntity());
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Delete", "StringResource", id.ToString(), $"Deleted resource '{stringResourceViewModel.Name}'");

            return RedirectToAction(nameof(Index));
        }

        private bool StringResourceExists(int id)
        {
            return _context.StringResources.Any(e => e.Id == id);
        }

        [HttpPost]
        public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
        {
            if (request.Value == null)
            {
                return BadRequest("Value cannot be null.");
            }

            var language = await _context.Languages.FindAsync(request.LanguageId);
            if (language == null || language.Culture == null)
            {
                return BadRequest("Invalid language ID or culture.");
            }

            var targetLanguage = GetTargetLanguage(language.Culture);
            if (targetLanguage == null)
            {
                return BadRequest($"Unsupported target language: {language.Culture}");
            }

            var translatedValue = await TranslateText(request.Value, targetLanguage);
            await _auditService.LogAsync("Translate", "StringResource", request.LanguageId.ToString(), $"Auto-translated to lang {language.Culture}: \"{request.Value?[..Math.Min(request.Value.Length, 80)]}...\"");
            return Json(new { translatedValue });
        }

        private string? GetTargetLanguage(string culture)
        {
            return culture switch
            {
                "en" => Configuration?["DeepLConfig:TargetLangEn"]!,
                "it" => Configuration?["DeepLConfig:TargetLangIt"]!,
                "fr" => Configuration?["DeepLConfig:TargetLangFr"]!,
                "es" => Configuration?["DeepLConfig:TargetLangEs"]!,
                "de" => Configuration?["DeepLConfig:TargetLangDe"]!,
                "pt" => Configuration?["DeepLConfig:TargetLangPt"]!,
                _ => null,
            };
        }

        private async Task<string> TranslateText(string text, string targetLanguage)
        {
            var result = await translator.TranslateTextAsync(text, SourceLang, targetLanguage);
            return result.Text;
        }
    }


}
