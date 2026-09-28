using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services;

public sealed class SystemLogOptions
{
    public const string SectionName = "Logging";

    /// <summary>Rows older than this are purged on schedule and on demand. 0 disables retention.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>Write the Serilog file sink. Null = on in Development, off in Production.</summary>
    public bool? FileSink { get; set; }

    public string? ConnectionStringName { get; set; } = "SerilogLogs";
}

public sealed class SystemLogService(
    IConfiguration configuration,
    IOptions<SystemLogOptions> options,
    ILogger<SystemLogService> logger) : ISystemLogService
{
    private const MySqlErrorCode TableNotFoundErrorCode = MySqlErrorCode.NoSuchTable;

    private readonly SystemLogOptions _options = options.Value;

    private string? ConnectionString =>
        configuration.GetConnectionString(_options.ConnectionStringName ?? "SerilogLogs");

    public async Task<SystemLogPage> QueryAsync(SystemLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = new SystemLogPage
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 10, 200),
            RetentionDays = _options.RetentionDays
        };

        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Unavailable(page, $"ConnectionStrings:{_options.ConnectionStringName} is not configured.");
        }

        var where = BuildFilter(query, out var parameters);

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureUtcSessionAsync(connection, cancellationToken);

            page.Total = await ScalarIntAsync(connection,
                $"SELECT COUNT(*) FROM Logs{where}", parameters, cancellationToken);

            page.LevelCounts = await LevelCountsAsync(connection, cancellationToken);
            page.DatabaseBytes = await DatabaseBytesAsync(connection, cancellationToken);
            page.Items = await PageAsync(connection, where, parameters, page, cancellationToken);
        }
        catch (MySqlException ex) when (ex.ErrorCode == TableNotFoundErrorCode)
        {
            return Unavailable(page, "The log table has not been created yet - it appears with the first log entry.");
        }
        catch (Exception ex) when (ex is MySqlException or InvalidOperationException or TimeoutException)
        {
            logger.LogWarning(ex, "System log query failed");
            return Unavailable(page, ex.Message);
        }

        return page;
    }

    public async Task<SystemLogEntry?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString)) return null;

        const string sql = "SELECT id, _ts, Timestamp, Level, Template, Message, Exception, Properties " +
                           "FROM Logs WHERE id = @id LIMIT 1";

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureUtcSessionAsync(connection, cancellationToken);

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            return Map(reader);
        }
        catch (Exception ex) when (ex is MySqlException or InvalidOperationException or TimeoutException)
        {
            logger.LogWarning(ex, "System log lookup failed for {LogId}", id);
            return null;
        }
    }

    public async Task<int> DeleteOlderThanAsync(int days, CancellationToken cancellationToken = default)
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString)) return 0;

        days = Math.Clamp(days, 1, 3650);
        var cutoff = DateTime.UtcNow.AddDays(-days);

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureUtcSessionAsync(connection, cancellationToken);

            await using var command = new MySqlCommand("DELETE FROM Logs WHERE _ts < @cutoff", connection);
            command.Parameters.AddWithValue("@cutoff", cutoff);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.ErrorCode == TableNotFoundErrorCode)
        {
            // The sink creates the table with the first write, so this is expected
            // on a fresh install rather than a failure worth warning about.
            return 0;
        }
        catch (Exception ex) when (ex is MySqlException or InvalidOperationException or TimeoutException)
        {
            logger.LogWarning(ex, "System log retention purge failed");
            return 0;
        }
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString)) return 0;

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new MySqlCommand("DELETE FROM Logs", connection);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.ErrorCode == TableNotFoundErrorCode)
        {
            return 0;
        }
        catch (Exception ex) when (ex is MySqlException or InvalidOperationException or TimeoutException)
        {
            logger.LogWarning(ex, "System log purge failed");
            return 0;
        }
    }

    private static SystemLogPage Unavailable(SystemLogPage page, string reason)
    {
        page.Available = false;
        page.UnavailableReason = reason;
        return page;
    }

    private static string BuildFilter(SystemLogQuery query, out List<MySqlParameter> parameters)
    {
        parameters = new List<MySqlParameter>();
        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Level) && !query.Level.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            clauses.Add("Level = @level");
            parameters.Add(new MySqlParameter("@level", query.Level.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            clauses.Add("(Message LIKE @search OR Template LIKE @search OR Exception LIKE @search OR Properties LIKE @search)");
            parameters.Add(new MySqlParameter("@search", $"%{query.Search.Trim()}%"));
        }

        // The UI works in local time while the log server stores UTC.
        if (query.From is { } from)
        {
            clauses.Add("_ts >= @from");
            parameters.Add(new MySqlParameter("@from", ToUtc(from)));
        }

        if (query.To is { } to)
        {
            clauses.Add("_ts < @to");
            parameters.Add(new MySqlParameter("@to", ToUtc(to)));
        }

        return clauses.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", clauses);
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
    };

    private static async Task EnsureUtcSessionAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("SET time_zone = '+00:00'", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<SystemLogEntry>> PageAsync(
        MySqlConnection connection, string where, List<MySqlParameter> parameters, SystemLogPage page,
        CancellationToken cancellationToken)
    {
        var sql = "SELECT id, _ts, Timestamp, Level, Template, Message, Exception, Properties " +
                   $"FROM Logs{where} ORDER BY _ts DESC, id DESC LIMIT @take OFFSET @skip";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());
        command.Parameters.AddWithValue("@take", page.PageSize);
        command.Parameters.AddWithValue("@skip", (page.Page - 1) * page.PageSize);

        var items = new List<SystemLogEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }
        return items;
    }

    private static async Task<Dictionary<string, int>> LevelCountsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using var command = new MySqlCommand("SELECT Level, COUNT(*) FROM Logs GROUP BY Level", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var level = reader.IsDBNull(0) ? "(none)" : reader.GetString(0);
            counts[level] = reader.GetInt32(1);
        }
        return counts;
    }

    private static async Task<long> DatabaseBytesAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COALESCE(SUM(data_length + index_length), 0) FROM information_schema.TABLES " +
                           "WHERE table_schema = DATABASE() AND table_name = 'Logs'";
        var value = await ScalarAsync(connection, sql, cancellationToken);
        return value is long l ? l : Convert.ToInt64(value ?? 0L);
    }

    private static async Task<int> ScalarIntAsync(MySqlConnection connection, string sql,
        List<MySqlParameter> parameters, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null ? 0 : Convert.ToInt32(value);
    }

    private static async Task<object?> ScalarAsync(MySqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(sql, connection);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static SystemLogEntry Map(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("id"),
        TimestampUtc = DateTime.SpecifyKind(reader.GetDateTime("_ts"), DateTimeKind.Utc),
        Timestamp = reader.IsDBNull(reader.GetOrdinal("Timestamp")) ? null : reader.GetString("Timestamp"),
        Level = reader.IsDBNull(reader.GetOrdinal("Level")) ? null : reader.GetString("Level"),
        MessageTemplate = reader.IsDBNull(reader.GetOrdinal("Template")) ? null : reader.GetString("Template"),
        Message = reader.IsDBNull(reader.GetOrdinal("Message")) ? null : reader.GetString("Message"),
        Exception = reader.IsDBNull(reader.GetOrdinal("Exception")) ? null : reader.GetString("Exception"),
        Properties = reader.IsDBNull(reader.GetOrdinal("Properties")) ? null : reader.GetString("Properties")
    };
}
