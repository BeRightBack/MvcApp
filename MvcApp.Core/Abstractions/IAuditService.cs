namespace MvcApp.Core.Abstractions;

public interface IAuditService
{
    Task LogAsync(string action, string entity, string? entityId = null, string? details = null);
}
