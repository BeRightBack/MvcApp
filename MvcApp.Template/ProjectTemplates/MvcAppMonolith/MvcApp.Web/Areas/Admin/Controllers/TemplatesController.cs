using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TemplatesController : Controller
    {
        private readonly ITemplateService _templateService;
        private readonly IAuditService _auditService;

        public TemplatesController(ITemplateService templateService, IAuditService auditService)
        {
            _templateService = templateService;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var active = await _templateService.GetActiveTemplateAsync();
            var templates = await _templateService.GetAvailableTemplatesAsync();
            ViewBag.ActiveTemplate = active;

            var landingEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var template in templates)
                landingEnabled[template.Name] = await _templateService.IsLandingPageEnabledAsync(template.Name);
            ViewBag.LandingEnabled = landingEnabled;

            return View(templates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetLanding(string name, bool enabled)
        {
            await _templateService.SetLandingPageEnabledAsync(name, enabled, User.Identity?.Name);
            await _auditService.LogAsync("SetLanding", "System", name, $"Landing page for template '{name}' {(enabled ? "enabled" : "disabled")}");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(string name)
        {
            await _templateService.SetActiveTemplateAsync(name, User.Identity?.Name);
            await _auditService.LogAsync("SetTemplate", "System", name, $"UI template changed to {name}");
            return RedirectToAction(nameof(Index));
        }
    }
}
