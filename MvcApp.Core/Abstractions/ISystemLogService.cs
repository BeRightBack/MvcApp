namespace MvcApp.Core.Abstractions;

/// <summary>
/// Read/manage access to the Serilog MySQL sink's Logs table. Lives in its own
/// database (ConnectionStrings:SerilogLogs), so it is queried with raw SQL
/// instead of the EF model.
/// </summary>
public interface ISystemLogService
{
    Task<SystemLogPage> QueryAsync(SystemLogQuery query, CancellationToken cancellationToken = default);

    Task<SystemLogEntry?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of deleted rows.</summary>
    Task<int> DeleteOlderThanAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of deleted rows.</summary>
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}
