using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Localization;
using MvcApp.Module.Messages.ViewModels;

namespace MvcApp.Module.Messages.Controllers.Areas.Admin;

/// <summary>
/// Read-oriented oversight of private messages. There is deliberately no compose, no read
/// receipt and no per-conversation view here: those belong to the member-facing side. A
/// moderator needs to answer "is this being used for abuse, and by whom", which needs a
/// filterable list and a way to remove a specific message that violates the rules.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
[ModuleEnabledFilter("Messages")]
public class MessagesController(
    IStringLocalizer<SharedResource> localizer,
    IAuditService auditService) : Controller
{
    private const int PageSize = 30;

    public async Task<IActionResult> Index(
        string search = "",
        string flagged = "",
        int page = 1,
        CancellationToken ct = default)
    {
        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();

        var query = db.Messages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                (m.SenderUsername != null && m.SenderUsername.Contains(term)) ||
                (m.RecipientUsername != null && m.RecipientUsername.Contains(term)) ||
                (m.Content != null && m.Content.Contains(term)));
        }

        // "deleted" is the abuse signal available without a moderation flag column: a sender who
        // deleted their own copy of a message is a pattern worth surfacing, and the table has no
        // IsFlagged column to filter on.
        if (flagged == "deleted")
            query = query.Where(m => m.SenderDeleted || m.RecipientDeleted);
        else if (flagged == "unread")
            query = query.Where(m => m.DateRead == null);

        var total = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var rows = await query
            .OrderByDescending(m => m.MessageSent)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new { m.Id, m.SenderId, m.SenderUsername, m.RecipientUsername,
                               m.Content, m.MessageSent, m.DateRead,
                               m.SenderDeleted, m.RecipientDeleted })
            .ToListAsync(ct);

        var pageIds = rows.Select(r => r.Id).ToList();

        // Correlated subquery rather than `senderIds.Contains(...)` against a materialized List:
        // the Oracle MySQL provider cannot assign a type mapping to a captured local collection
        // here and threw "Expression '@senderIds' in the SQL tree does not have a type mapping
        // assigned" — a 500 on the page. Pushing the membership test into the query keeps it a
        // single translatable statement.
        //
        // UserBan has no IsActive column: a ban is live while RevokedAt is null, and BannedUntil
        // null means indefinite. Both must hold for the sender to count as currently banned.
        var banned = await db.UserBans.AsNoTracking()
            .Where(b => b.RevokedAt == null
                        && (b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow)
                        && db.Messages.Any(m => m.SenderId == b.UserId && pageIds.Contains(m.Id)))
            .Select(b => b.UserId)
            .ToListAsync(ct);

        var bannedSet = banned.ToHashSet(StringComparer.Ordinal);

        return View(new AdminMessagesViewModel
        {
            Search = search,
            Flagged = flagged,
            Page = page,
            TotalPages = totalPages,
            Total = total,
            Messages = rows
                .Select(r => new AdminMessageRow(
                    r.Id, r.SenderUsername, r.RecipientUsername, r.Content, r.MessageSent,
                    r.DateRead, r.SenderDeleted, r.RecipientDeleted,
                    bannedSet.Contains(r.SenderId)))
                .ToList(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();

        var message = await db.Messages.FindAsync([id], ct);
        if (message is null)
        {
            TempData["Error"] = localizer["That message no longer exists."].Value;
            return RedirectToAction(nameof(Index));
        }

        var context = message.SenderUsername is null
            ? $"message {id} from {message.SenderId} to {message.RecipientId}"
            : $"message {id} from {message.SenderUsername} to {message.RecipientUsername}";

        db.Messages.Remove(message);
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync("Delete", "Message", id.ToString(),
            $"Admin deleted a private {context}.");

        TempData["Success"] = localizer["Message deleted."].Value;
        return RedirectToAction(nameof(Index));
    }
}
