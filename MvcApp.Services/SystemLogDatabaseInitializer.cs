using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace MvcApp.Services;

/// <summary>
/// Creates the Serilog log database when it is missing, so a fresh install logs
/// to MySQL instead of silently failing. The sink creates its own table, but it
/// cannot create the database it is asked to connect to.
/// </summary>
public sealed class SystemLogDatabaseInitializer(ILogger<SystemLogDatabaseInitializer> logger)
{
    private static readonly string[] ForbiddenNameChars = { "`", "'", "\"", ";", "\\", " ", "\t", "\n", "\r", "/", "*", "?" };

    /// <summary>
    /// True when the name is safe to embed in a CREATE DATABASE statement. MySQL
    /// identifiers may not contain backticks, quotes, whitespace or wildcards.
    /// </summary>
    public static bool IsValidDatabaseName(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name.Length <= 64 &&
        name[0] is not ('-' or '.') &&
        name.All(c => char.IsLetterOrDigit(c) || c is '_' or '$' or '-') &&
        !ForbiddenNameChars.Any(name.Contains);

    /// <summary>
    /// Returns a copy of the connection string with the database removed, so the
    /// connection can be opened against the server before the database exists.
    /// Returns null when the input cannot be parsed.
    /// </summary>
    public static string? BuildServerConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return null;

        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            builder.Database = string.Empty;
            return builder.ConnectionString;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates the database named in the connection string if it does not exist.
    /// Returns true when the database was created, false when it already existed or
    /// the attempt failed. Never throws: logging must not stop the app from starting.
    /// </summary>
    public Task<bool> EnsureExistsAsync(string? connectionString, CancellationToken cancellationToken = default) =>
        EnsureExistsAsync(connectionString, logger, cancellationToken);

    /// <summary>
    /// Same as the instance overload but usable before DI exists. Program.cs calls this
    /// BEFORE the Serilog logger is built, because the MySQL sink connects eagerly and is
    /// dropped for the lifetime of the process if its database does not exist yet.
    /// </summary>
    public static async Task<bool> EnsureExistsAsync(
        string? connectionString,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        string databaseName;
        string serverConnectionString;
        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            databaseName = builder.Database;
            builder.Database = string.Empty;
            serverConnectionString = builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            logger?.LogWarning(ex, "System log connection string could not be parsed; skipping database creation");
            return false;
        }

        if (!IsValidDatabaseName(databaseName))
        {
            logger?.LogWarning("System log database name '{Name}' is missing or not a valid MySQL identifier; skipping database creation", databaseName);
            return false;
        }

        try
        {
            await using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var existsCommand = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = @name", connection);
            existsCommand.Parameters.AddWithValue("@name", databaseName);
            var count = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (count > 0) return false;

            await using var createCommand = new MySqlCommand(
                $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", connection);
            await createCommand.ExecuteNonQueryAsync(cancellationToken);

            logger?.LogInformation("Created the system log database {Database}", databaseName);
            return true;
        }
        catch (Exception ex) when (ex is MySqlException or InvalidOperationException or TimeoutException or OperationCanceledException)
        {
            if (ex is OperationCanceledException) throw;
            logger?.LogWarning(ex, "Could not ensure the system log database {Database} exists; the MySQL log sink will not receive rows until it does", databaseName);
            return false;
        }
    }
}
