using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MvcApp.Module.IPTV.Models.SubscriptionViewModels;

public class SubscriptionViewModel
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string? UserEmail { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string? UserName { get; set; }

    [Required]
    public int SubscriptionPlanId { get; set; }

    [Required]
    public string? SubscriptionPlanName { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
    [Required]
    public string Status { get; set; } = "pending";
    public string? UserCode { get; set; }
    public string? Password { get; set; }

    public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "pending", Text = "Pending" },
        new SelectListItem { Value = "processing", Text = "Processing" },
        new SelectListItem { Value = "active", Text = "Active" },
        new SelectListItem { Value = "expired", Text = "Expired" }
    };
}

public class SubscriptionIndexViewModel
{
    public List<SubscriptionViewModel> Subscriptions { get; set; } = new();
    public string? SearchQuery { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public string? SortOrder { get; set; }
}
