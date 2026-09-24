using Microsoft.AspNetCore.Http;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using System.Security.Claims;

namespace MvcApp.Infrastructure;

public class AuditService(UserDbContext db, IHttpContextAccessor httpContext) : IAuditService
{
    public async Task LogAsync(string action, string entity, string? entityId = null, string? details = null)
    {
        var userId = httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var userName = httpContext.HttpContext?.User?.Identity?.Name ?? "system";
        var ip = httpContext.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            IpAddress = ip,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
