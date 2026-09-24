using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Models.SubscriptionViewModels;

namespace MvcApp.Module.IPTV.Controllers;

[ModuleEnabledFilter("Iptv")]
[Authorize(Roles = "Admin")]
public class IptvPlanController(IRepository<SubscriptionPlan> subscriptionPlanRepository, UserDbContext context) : Controller
{
    [HttpGet("iptv-plan")]
    public async Task<IActionResult> Index()
    {
        var subscriptionPlans = await context.SubscriptionPlans
            .Include(plan => plan.SubscriptionDetails)
            .ToListAsync();

        var subscriptionPlanViewModels = subscriptionPlans
            .Select(plan => new SubscriptionPlanViewModel
            {
                Id = plan.Id,
                Name = plan.Name!,
                DescriptionShort = plan.DescriptionShort,
                Description = plan.Description!,
                SubscriptionDetails = plan.SubscriptionDetails.Select(detail => new SubscriptionDetailViewModel
                {
                    Id = detail.Id,
                    Description = detail.Description!,
                    Price = detail.Price,
                    DurationInMonths = detail.DurationInMonths,
                    DurationInHours = detail.DurationInHours,
                    SubscriptionPlanId = detail.SubscriptionPlanId
                }).ToList()
            }).ToList();

        ViewData["Title"] = "Subscription Plans - Admin";
        return View(subscriptionPlanViewModels);
    }

    [HttpGet("iptv-plan/create")]
    public IActionResult Create()
    {
        var model = new SubscriptionPlanViewModel
        {
            Name = "Default Plan Name",
            DescriptionShort = "Default Plan DescriptionShort",
            Description = "Default Plan Description",
            SubscriptionDetails = new List<SubscriptionDetailViewModel>
            {
                new SubscriptionDetailViewModel
                {
                    Description = "Basic Plan",
                    Price = 9.99m,
                    DurationInMonths = 1
                }
            }
        };

        var usCulture = new CultureInfo("en-US");
        foreach (var detail in model.SubscriptionDetails)
        {
            detail.PriceFormatted = detail.Price.ToString("C", usCulture);
        }

        ViewData["Title"] = "Create Plan";
        return View(model);
    }

    [HttpPost("iptv-plan/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionPlanViewModel model)
    {
        if (ModelState.IsValid)
        {
            var subscriptionPlan = new SubscriptionPlan
            {
                Name = model.Name,
                DescriptionShort = model.DescriptionShort,
                Description = model.Description,
                SubscriptionDetails = model.SubscriptionDetails.Select(detail => new SubscriptionDetail
                {
                    Description = detail.Description,
                    Price = detail.Price,
                    DurationInMonths = detail.DurationInMonths,
                    DurationInHours = detail.DurationInHours,
                    SubscriptionPlanId = detail.SubscriptionPlanId
                }).ToList()
            };

            await subscriptionPlanRepository.AddAsync(subscriptionPlan);
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpGet("iptv-plan/edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var plan = await context.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            return NotFound();
        }

        var usCulture = new CultureInfo("en-US");
        var model = new SubscriptionPlanViewModel
        {
            Id = plan.Id,
            Name = plan.Name!,
            DescriptionShort = plan.DescriptionShort,
            Description = plan.Description!,
            SubscriptionDetails = plan.SubscriptionDetails.Select(detail => new SubscriptionDetailViewModel
            {
                Id = detail.Id,
                Description = detail.Description!,
                Price = detail.Price,
                PriceFormatted = detail.Price.ToString("C", usCulture),
                DurationInMonths = detail.DurationInMonths,
                DurationInHours = detail.DurationInHours ?? 0,
                SubscriptionPlanId = detail.SubscriptionPlanId
            }).ToList()
        };

        ViewData["Title"] = "Edit Plan";
        return View(model);
    }

    [HttpPost("iptv-plan/edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubscriptionPlanViewModel model, string action = "")
    {
        if (action.StartsWith("addDetail"))
        {
            model.SubscriptionDetails.Add(new SubscriptionDetailViewModel
            {
                Description = string.Empty,
                Price = 0.00m,
                DurationInMonths = 1,
                DurationInHours = 0
            });
            ModelState.Clear();
            return View(model);
        }

        if (action.StartsWith("removeDetail"))
        {
            if (int.TryParse(action.Split('_')[1], out int index))
            {
                if (index >= 0 && index < model.SubscriptionDetails.Count)
                {
                    model.SubscriptionDetails.RemoveAt(index);
                }
            }
            ModelState.Clear();
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingPlan = await context.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .FirstOrDefaultAsync(p => p.Id == model.Id);

        if (existingPlan == null)
        {
            return NotFound();
        }

        existingPlan.Name = model.Name;
        existingPlan.DescriptionShort = model.DescriptionShort;
        existingPlan.Description = model.Description;

        var modelDetailIds = model.SubscriptionDetails.Select(d => d.Id).ToList();

        var detailsToRemove = existingPlan.SubscriptionDetails
            .Where(d => !modelDetailIds.Contains(d.Id))
            .ToList();
        foreach (var detail in detailsToRemove)
        {
            context.SubscriptionDetails.Remove(detail);
        }

        foreach (var detailModel in model.SubscriptionDetails)
        {
            var existingDetail = existingPlan.SubscriptionDetails
                .FirstOrDefault(d => d.Id == detailModel.Id);

            if (existingDetail != null)
            {
                existingDetail.Description = detailModel.Description;
                existingDetail.Price = detailModel.Price;
                existingDetail.DurationInMonths = detailModel.DurationInMonths;
                existingDetail.DurationInHours = detailModel.DurationInHours;
            }
            else
            {
                var newDetail = new SubscriptionDetail
                {
                    Description = detailModel.Description,
                    Price = detailModel.Price,
                    DurationInMonths = detailModel.DurationInMonths,
                    DurationInHours = detailModel.DurationInHours,
                    SubscriptionPlanId = existingPlan.Id
                };
                existingPlan.SubscriptionDetails.Add(newDetail);
            }
        }

        await subscriptionPlanRepository.UpdateAsync(existingPlan);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("iptv-plan/delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var plan = await context.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            return NotFound();
        }

        var viewModel = new SubscriptionPlanViewModel
        {
            Id = plan.Id,
            Name = plan.Name!,
            DescriptionShort = plan.DescriptionShort,
            Description = plan.Description!,
            SubscriptionDetails = plan.SubscriptionDetails.Select(detail => new SubscriptionDetailViewModel
            {
                Id = detail.Id,
                Description = detail.Description!,
                Price = detail.Price,
                DurationInMonths = detail.DurationInMonths,
                SubscriptionPlanId = detail.SubscriptionPlanId
            }).ToList()
        };

        ViewData["Title"] = "Delete Plan";
        return View(viewModel);
    }

    [HttpPost("iptv-plan/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await subscriptionPlanRepository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
