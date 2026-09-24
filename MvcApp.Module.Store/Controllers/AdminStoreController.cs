using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using System.Text.RegularExpressions;

namespace MvcApp.Module.Store.Controllers.Areas.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[ModuleEnabledFilter("Store")]
public partial class StoreController(
    IRepository<Product> productRepo,
    IRepository<ProductCategory> categoryRepo,
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo) : Controller
{
    public async Task<IActionResult> Index()
    {
        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        ViewBag.TotalProducts = await db.Products.CountAsync();
        ViewBag.TotalOrders = await db.Orders.CountAsync();
        ViewBag.PendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        ViewBag.TotalRevenue = await db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.LowStockProducts = await db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= 5);

        var recentOrders = await db.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .ToListAsync();
        foreach (var order in recentOrders)
            order.Items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
        ViewBag.RecentOrders = recentOrders;

        return View();
    }

    public async Task<IActionResult> Products(int page = 1)
    {
        const int pageSize = 20;
        var productsQuery = productRepo.Query().OrderByDescending(p => p.CreatedAt);

        var totalProducts = await productsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        var paged = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        foreach (var p in paged)
            p.Category = await db.ProductCategories.FindAsync(p.CategoryId);

        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        return View(paged);
    }

    public async Task<IActionResult> CreateProduct()
    {
        ViewBag.Categories = new SelectList(
            await categoryRepo.Query().OrderBy(c => c.SortOrder).ToListAsync(),
            "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product model)
    {
        model.Slug = GenerateSlug(model.Name);
        var existing = await productRepo.Query().AnyAsync(p => p.Slug == model.Slug);
        if (existing)
            model.Slug = model.Slug + "-" + Guid.NewGuid().ToString()[..6];

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(
                await categoryRepo.Query().OrderBy(c => c.SortOrder).ToListAsync(),
                "Id", "Name", model.CategoryId);
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        await productRepo.AddAsync(model);
        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await productRepo.GetByIdAsync(id);
        if (product == null) return NotFound();

        ViewBag.Categories = new SelectList(
            await categoryRepo.Query().OrderBy(c => c.SortOrder).ToListAsync(),
            "Id", "Name", product.CategoryId);
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, Product model)
    {
        if (id != model.Id) return NotFound();

        model.Slug = GenerateSlug(model.Name);

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(
                await categoryRepo.Query().OrderBy(c => c.SortOrder).ToListAsync(),
                "Id", "Name", model.CategoryId);
            return View(model);
        }

        var existing = await productRepo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Name = model.Name;
        existing.Slug = model.Slug;
        existing.ShortDescription = model.ShortDescription;
        existing.Description = model.Description;
        existing.Price = model.Price;
        existing.ComparePrice = model.ComparePrice;
        existing.ImageUrl = model.ImageUrl;
        existing.AdditionalImages = model.AdditionalImages;
        existing.CategoryId = model.CategoryId;
        existing.StockQuantity = model.StockQuantity;
        existing.IsActive = model.IsActive;
        existing.IsFeatured = model.IsFeatured;
        existing.SortOrder = model.SortOrder;

        await productRepo.UpdateAsync(existing);
        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await productRepo.GetByIdAsync(id);
        if (product == null) return NotFound();

        await productRepo.DeleteAsync(product);
        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> Categories()
    {
        var categories = await categoryRepo.Query().OrderBy(c => c.SortOrder).ToListAsync();
        return View(categories);
    }

    public IActionResult CreateCategory() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(ProductCategory model)
    {
        model.Slug = GenerateSlug(model.Name);
        model.CreatedAt = DateTime.UtcNow;
        if (!ModelState.IsValid) return View(model);

        await categoryRepo.AddAsync(model);
        TempData["Success"] = "Category created.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await categoryRepo.GetByIdAsync(id);
        if (category == null) return NotFound();

        await categoryRepo.DeleteAsync(category);
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> Orders(int page = 1, OrderStatus? status = null)
    {
        const int pageSize = 20;
        var ordersQuery = orderRepo.Query();

        if (status.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Status == status.Value);

        var totalOrders = await ordersQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
        var paged = await ordersQuery
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        foreach (var order in paged)
            order.Items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.StatusFilter = status;
        return View(paged);
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        order.Items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status)
    {
        var order = await orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await orderRepo.UpdateAsync(order);
        TempData["Success"] = $"Order status updated to {status}.";
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.TrimStart('-').TrimEnd('-');
    }
}